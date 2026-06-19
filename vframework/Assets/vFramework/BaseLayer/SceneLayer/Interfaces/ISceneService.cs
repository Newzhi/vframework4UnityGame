using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace BaseLayer.Scene
{
    /// <summary>
    /// 场景调度服务：安全加载/卸载 Unity 场景（Build-in / AB）。
    /// 业务与 GameFlow 应通过本接口切换场景，禁止直接调用 SceneManager。
    /// </summary>
    public interface ISceneService
    {
        bool IsBusy { get; }
        string ActiveSceneId { get; }
        IReadOnlyCollection<string> LoadedSceneIds { get; }
        SceneRequestConflictPolicy ConflictPolicy { get; set; }

        UniTask<SceneLoadResult> LoadAsync(SceneLoadRequest request, CancellationToken cancellationToken = default);

        UniTask<SceneLoadResult> LoadSingleAsync(string sceneId, object userData = null,
            CancellationToken cancellationToken = default);

        UniTask<SceneLoadResult> LoadAdditiveAsync(string sceneId, bool setActive = true, object userData = null,
            CancellationToken cancellationToken = default);

        UniTask<SceneLoadResult> UnloadAsync(string sceneId, CancellationToken cancellationToken = default);

        bool TryGetEntry(string sceneId, out SceneEntry entry);
    }
}
