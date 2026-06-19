namespace BaseLayer.Scene
{
    /// <summary>场景加载请求 DTO。</summary>
    public sealed class SceneLoadRequest
    {
        public string SceneId { get; set; }
        public SceneLoadMode Mode { get; set; } = SceneLoadMode.Single;
        public bool SetActive { get; set; } = true;
        public object UserData { get; set; }
        public SceneCleanupPolicy? CleanupOverride { get; set; }
        public bool ShowLoadingUi { get; set; }
    }
}
