using System.Collections.Generic;
using UnityEngine;

namespace BaseLayer.Scene
{
    /// <summary>场景目录 ScriptableObject：逻辑 Id → Unity 场景名 / AB loadPath / 预加载包。</summary>
    [CreateAssetMenu(fileName = "SceneCatalog", menuName = "vFramework/Scene Catalog")]
    public sealed class SceneCatalog : ScriptableObject
    {
        [SerializeField] List<SceneEntry> entries = new List<SceneEntry>();

        public IReadOnlyList<SceneEntry> Entries => entries;

        public bool TryGetEntry(string sceneId, out SceneEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(sceneId) || entries == null)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                SceneEntry candidate = entries[i];
                if (candidate != null && candidate.Id == sceneId)
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>替换全部条目（Demo / 测试用）。</summary>
        public void ReplaceEntries(IReadOnlyList<SceneEntry> newEntries)
        {
            entries = newEntries != null
                ? new List<SceneEntry>(newEntries)
                : new List<SceneEntry>();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (entries == null)
                return;

            var seen = new HashSet<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                SceneEntry entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.Id))
                    continue;

                if (!seen.Add(entry.Id))
                    Debug.LogWarning("[SceneCatalog] Duplicate scene Id: " + entry.Id, this);
            }
        }
#endif
    }

    /// <summary>单条场景配置。</summary>
    [System.Serializable]
    public sealed class SceneEntry
    {
        public string Id;
        public SceneSource Source = SceneSource.BuildIn;
        public string UnitySceneName;
        public string SceneLoadPath;
        public SceneLoadMode DefaultMode = SceneLoadMode.Single;
        public SceneCleanupPolicy Cleanup = SceneCleanupPolicy.FullUnloadAll;
        public string[] PreloadBundles;
        public string[] OwnedBundles;
        public bool SetActiveOnLoad = true;

        public string ResolveUnitySceneName()
        {
            if (!string.IsNullOrEmpty(UnitySceneName))
                return UnitySceneName;

            if (!string.IsNullOrEmpty(SceneLoadPath))
            {
                int slash = SceneLoadPath.LastIndexOf('/');
                return slash >= 0 ? SceneLoadPath.Substring(slash + 1) : SceneLoadPath;
            }

            return Id;
        }

        public SceneCleanupPolicy ResolveCleanup(SceneLoadMode mode, SceneCleanupPolicy? overridePolicy)
        {
            if (overridePolicy.HasValue)
                return overridePolicy.Value;

            if (mode == SceneLoadMode.Additive)
                return SceneCleanupPolicy.SceneLocalOnly;

            return Cleanup;
        }
    }
}
