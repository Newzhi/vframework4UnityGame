namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 可选：在 Editor Scene 视图绘制 Gizmo（对标 MonoBehaviour.OnDrawGizmos）。
    /// 由 <see cref="GameRoot"/> 转发；仅 <c>#if UNITY_EDITOR</c> 下调度。
    /// 执行顺序与 <see cref="IGameModule.Priority"/> 一致。
    /// </summary>
    /// <remarks>
    /// 回调内请只读 Init/Update 缓存的数据，使用 <c>Gizmos.*</c> 绘制；
    /// 勿在 DrawGizmos 中 Get 服务、加载资源或做重逻辑。
    /// </remarks>
    public interface IGizmoDrawModule : IGameModule
    {
        /// <summary>Scene 视图刷新时调用（GameRoot 被选中与否均会绘制）。</summary>
        void DrawGizmos();
    }
}
