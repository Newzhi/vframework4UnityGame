namespace BaseLayer.Scene
{
    /// <summary>场景加载/卸载结果。</summary>
    public readonly struct SceneLoadResult
    {
        public bool Success { get; }
        public string SceneId { get; }
        public SceneTransitionPhase Phase { get; }
        public string ErrorMessage { get; }

        public SceneLoadResult(bool success, string sceneId, SceneTransitionPhase phase, string errorMessage = null)
        {
            Success = success;
            SceneId = sceneId;
            Phase = phase;
            ErrorMessage = errorMessage;
        }

        public static SceneLoadResult Ok(string sceneId) =>
            new SceneLoadResult(true, sceneId, SceneTransitionPhase.Completed);

        public static SceneLoadResult Fail(string sceneId, SceneTransitionPhase phase, string message) =>
            new SceneLoadResult(false, sceneId, phase, message);

        public static SceneLoadResult Busy(string sceneId) =>
            new SceneLoadResult(false, sceneId, SceneTransitionPhase.Failed, "Scene service is busy.");
    }
}
