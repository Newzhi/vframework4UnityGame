using System;
using System.Collections.Generic;
using System.Threading;
using BaseFramework.BaseEventSys;
using BaseLayer.Scene.Impt.Hooks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace BaseLayer.Scene.Impt
{
    /// <summary>场景调度实现：串行队列 + 清理链 + Build-in / AB 双路径。</summary>
    public sealed class SceneService : ISceneService, IDisposable
    {
        readonly SceneCatalog _catalog;
        readonly SceneCleanupPipeline _pipeline;
        readonly HashSet<string> _loadedSceneIds = new HashSet<string>(StringComparer.Ordinal);
        readonly Queue<PendingRequest> _queue = new Queue<PendingRequest>();

        CancellationTokenSource _operationCts;
        bool _isBusy;
        string _activeSceneId;

        public SceneService(SceneCatalog catalog, IEnumerable<ISceneTransitionHook> extraHooks = null)
        {
            _catalog = catalog;
            var hooks = new List<ISceneTransitionHook>
            {
                new AssetCleanupHook(),
                new PoolRuntimeHook(),
                new EventBusHook()
            };

            if (extraHooks != null)
                hooks.AddRange(extraHooks);

            _pipeline = new SceneCleanupPipeline(hooks);
        }

        public bool IsBusy => _isBusy;
        public string ActiveSceneId => _activeSceneId;
        public SceneRequestConflictPolicy ConflictPolicy { get; set; } = SceneRequestConflictPolicy.ReplacePending;

        public IReadOnlyCollection<string> LoadedSceneIds => _loadedSceneIds;

        public bool TryGetEntry(string sceneId, out SceneEntry entry) => _catalog.TryGetEntry(sceneId, out entry);

        public UniTask<SceneLoadResult> LoadSingleAsync(string sceneId, object userData = null,
            CancellationToken cancellationToken = default)
        {
            return LoadAsync(new SceneLoadRequest
            {
                SceneId = sceneId,
                Mode = SceneLoadMode.Single,
                UserData = userData
            }, cancellationToken);
        }

        public UniTask<SceneLoadResult> LoadAdditiveAsync(string sceneId, bool setActive = true, object userData = null,
            CancellationToken cancellationToken = default)
        {
            return LoadAsync(new SceneLoadRequest
            {
                SceneId = sceneId,
                Mode = SceneLoadMode.Additive,
                SetActive = setActive,
                UserData = userData
            }, cancellationToken);
        }

        public UniTask<SceneLoadResult> LoadAsync(SceneLoadRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || string.IsNullOrEmpty(request.SceneId))
                return UniTask.FromResult(SceneLoadResult.Fail(null, SceneTransitionPhase.Failed, "SceneId is empty."));

            if (!_catalog.TryGetEntry(request.SceneId, out SceneEntry entry))
            {
                return UniTask.FromResult(SceneLoadResult.Fail(
                    request.SceneId, SceneTransitionPhase.Failed, "SceneId not in catalog: " + request.SceneId));
            }

            var pending = new PendingRequest(request, entry, cancellationToken);
            if (_isBusy && ConflictPolicy == SceneRequestConflictPolicy.Queue)
            {
                _queue.Enqueue(pending);
                return pending.Completion.Task;
            }

            if (_isBusy && ConflictPolicy == SceneRequestConflictPolicy.ReplacePending)
            {
                CancelCurrentOperation();
                _queue.Clear();
            }

            return RunOperationAsync(pending);
        }

        public UniTask<SceneLoadResult> UnloadAsync(string sceneId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sceneId))
                return UniTask.FromResult(SceneLoadResult.Fail(null, SceneTransitionPhase.Failed, "SceneId is empty."));

            if (!_catalog.TryGetEntry(sceneId, out SceneEntry entry))
            {
                return UniTask.FromResult(SceneLoadResult.Fail(
                    sceneId, SceneTransitionPhase.Failed, "SceneId not in catalog: " + sceneId));
            }

            var pending = new PendingRequest(new SceneLoadRequest { SceneId = sceneId }, entry, cancellationToken, true);
            if (_isBusy && ConflictPolicy == SceneRequestConflictPolicy.Queue)
            {
                _queue.Enqueue(pending);
                return pending.Completion.Task;
            }

            if (_isBusy && ConflictPolicy == SceneRequestConflictPolicy.ReplacePending)
            {
                CancelCurrentOperation();
                _queue.Clear();
            }

            return RunOperationAsync(pending);
        }

        public void Dispose()
        {
            CancelCurrentOperation();
            _queue.Clear();
        }

        async UniTask<SceneLoadResult> RunOperationAsync(PendingRequest pending)
        {
            _isBusy = true;
            _operationCts = CancellationTokenSource.CreateLinkedTokenSource(pending.ExternalToken);
            CancellationToken token = _operationCts.Token;

            SceneLoadResult result = default;
            try
            {
                result = pending.IsUnload
                    ? await UnloadInternalAsync(pending.Entry, token)
                    : await LoadInternalAsync(pending.Request, pending.Entry, token);
            }
            catch (OperationCanceledException)
            {
                result = SceneLoadResult.Fail(pending.Request?.SceneId, SceneTransitionPhase.Cancelled, "Cancelled.");
                Publish(SceneTransitionPhase.Cancelled, _activeSceneId, pending.Request?.SceneId, userData: pending.Request?.UserData);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                result = SceneLoadResult.Fail(pending.Request?.SceneId, SceneTransitionPhase.Failed, ex.Message);
                Publish(SceneTransitionPhase.Failed, _activeSceneId, pending.Request?.SceneId, errorMessage: ex.Message,
                    userData: pending.Request?.UserData);
            }
            finally
            {
                _operationCts?.Dispose();
                _operationCts = null;
                _isBusy = false;
            }

            pending.Completion.TrySetResult(result);
            ProcessQueue();
            return result;
        }

        void ProcessQueue()
        {
            if (_isBusy || _queue.Count == 0)
                return;

            PendingRequest next = _queue.Dequeue();
            RunOperationAsync(next).Forget();
        }

        async UniTask<SceneLoadResult> LoadInternalAsync(SceneLoadRequest request, SceneEntry entry, CancellationToken token)
        {
            string sceneId = request.SceneId;
            string fromSceneId = _activeSceneId;
            LoadSceneMode unityMode = ToUnityLoadMode(request.Mode);

            SceneCleanupPolicy cleanup = entry.ResolveCleanup(request.Mode, request.CleanupOverride);
            bool setActive = request.SetActive || entry.SetActiveOnLoad;

            var context = new SceneTransitionContext
            {
                FromSceneId = fromSceneId,
                ToSceneId = sceneId,
                Mode = request.Mode,
                CleanupPolicy = cleanup,
                UserData = request.UserData
            };

            Publish(SceneTransitionPhase.BeforeLeave, fromSceneId, sceneId, userData: request.UserData);
            _pipeline.OnBeforeLeave(context);

            Publish(SceneTransitionPhase.Loading, fromSceneId, sceneId, 0f, request.UserData);
            var progress = new Progress<float>(p => Publish(SceneTransitionPhase.Loading, fromSceneId, sceneId, p, request.UserData));

            bool loaded;
            if (entry.Source == SceneSource.AssetBundle)
            {
                loaded = await SceneBundleLoader.LoadSceneFromBundleAsync(
                    sceneId,
                    entry.SceneLoadPath,
                    unityMode,
                    LocalPhysicsMode.None,
                    progress,
                    token);
            }
            else
            {
                loaded = await LoadBuildInSceneAsync(entry.ResolveUnitySceneName(), unityMode, progress, token);
            }

            if (!loaded)
            {
                Publish(SceneTransitionPhase.Failed, fromSceneId, sceneId, errorMessage: "Scene load failed.",
                    userData: request.UserData);
                return SceneLoadResult.Fail(sceneId, SceneTransitionPhase.Failed, "Scene load failed.");
            }

            if (setActive)
            {
                UnityScene scene = SceneManager.GetSceneByName(entry.ResolveUnitySceneName());
                if (scene.IsValid())
                    SceneManager.SetActiveScene(scene);
            }

            PreloadBundles(entry);

            if (request.Mode == SceneLoadMode.Single)
                _loadedSceneIds.Clear();

            _loadedSceneIds.Add(sceneId);
            _activeSceneId = sceneId;

            context.ToSceneId = sceneId;
            _pipeline.OnAfterEnter(context);
            Publish(SceneTransitionPhase.AfterEnter, fromSceneId, sceneId, 1f, request.UserData);
            Publish(SceneTransitionPhase.Completed, fromSceneId, sceneId, 1f, request.UserData);

            return SceneLoadResult.Ok(sceneId);
        }

        async UniTask<SceneLoadResult> UnloadInternalAsync(SceneEntry entry, CancellationToken token)
        {
            string sceneId = entry.Id;
            string unityName = entry.ResolveUnitySceneName();
            UnityScene scene = SceneManager.GetSceneByName(unityName);

            Publish(SceneTransitionPhase.BeforeLeave, _activeSceneId, sceneId);

            if (!scene.IsValid())
            {
                SceneBundleLoader.ReleaseSceneBundles(sceneId);
                _loadedSceneIds.Remove(sceneId);
                if (_activeSceneId == sceneId)
                    _activeSceneId = null;
                Publish(SceneTransitionPhase.Completed, sceneId, null);
                return SceneLoadResult.Ok(sceneId);
            }

            Publish(SceneTransitionPhase.Loading, _activeSceneId, sceneId, 0f);
            AsyncOperation op = SceneManager.UnloadSceneAsync(scene);
            while (op != null && !op.isDone)
            {
                token.ThrowIfCancellationRequested();
                Publish(SceneTransitionPhase.Loading, _activeSceneId, sceneId, op.progress);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            SceneBundleLoader.ReleaseSceneBundles(sceneId);
            if (entry.OwnedBundles != null && entry.OwnedBundles.Length > 0)
                BundleManager.UnloadPackageBundles(entry.OwnedBundles);

            _loadedSceneIds.Remove(sceneId);

            if (_activeSceneId == sceneId)
            {
                _activeSceneId = null;
                if (_loadedSceneIds.Count > 0)
                {
                    foreach (string remainingId in _loadedSceneIds)
                    {
                        if (_catalog.TryGetEntry(remainingId, out SceneEntry remaining))
                        {
                            UnityScene remainingScene = SceneManager.GetSceneByName(remaining.ResolveUnitySceneName());
                            if (remainingScene.IsValid())
                            {
                                SceneManager.SetActiveScene(remainingScene);
                                _activeSceneId = remainingId;
                                break;
                            }
                        }
                    }
                }
            }

            Publish(SceneTransitionPhase.Completed, sceneId, _activeSceneId);
            return SceneLoadResult.Ok(sceneId);
        }

        static async UniTask<bool> LoadBuildInSceneAsync(
            string unitySceneName,
            LoadSceneMode mode,
            IProgress<float> progress,
            CancellationToken token)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(unitySceneName, mode);
            if (op == null)
            {
                Debug.LogError("[SceneService] LoadSceneAsync failed: " + unitySceneName);
                return false;
            }

            while (!op.isDone)
            {
                token.ThrowIfCancellationRequested();
                progress?.Report(op.progress);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            progress?.Report(1f);
            return SceneManager.GetSceneByName(unitySceneName).IsValid();
        }

        static LoadSceneMode ToUnityLoadMode(SceneLoadMode mode) =>
            mode == SceneLoadMode.Additive ? LoadSceneMode.Additive : LoadSceneMode.Single;

        static void PreloadBundles(SceneEntry entry)
        {
            if (entry.PreloadBundles == null || entry.PreloadBundles.Length == 0)
                return;

            BundleResLoader.Instance.PreLoadBundles(entry.PreloadBundles);
        }

        void CancelCurrentOperation()
        {
            _operationCts?.Cancel();
            _operationCts?.Dispose();
            _operationCts = null;
        }

        void Publish(
            SceneTransitionPhase phase,
            string fromSceneId,
            string toSceneId,
            float progress = 0f,
            object userData = null,
            string errorMessage = null)
        {
            GameEventBus.SentEvent(new SceneTransitionEvent(
                phase, fromSceneId, toSceneId, progress, userData, errorMessage));
        }

        sealed class PendingRequest
        {
            public SceneLoadRequest Request { get; }
            public SceneEntry Entry { get; }
            public CancellationToken ExternalToken { get; }
            public bool IsUnload { get; }
            public UniTaskCompletionSource<SceneLoadResult> Completion { get; } =
                new UniTaskCompletionSource<SceneLoadResult>();

            public PendingRequest(SceneLoadRequest request, SceneEntry entry, CancellationToken externalToken,
                bool isUnload = false)
            {
                Request = request;
                Entry = entry;
                ExternalToken = externalToken;
                IsUnload = isUnload;
            }
        }
    }
}
