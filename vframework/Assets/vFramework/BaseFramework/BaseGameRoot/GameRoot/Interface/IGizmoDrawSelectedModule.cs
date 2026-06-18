namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 可选：仅当 Hierarchy 中选中 <see cref="GameRoot"/> 时在 Scene 视图绘制（对标 OnDrawGizmosSelected）。
    /// 可与 <see cref="IGizmoDrawModule"/> 同时实现。
    /// </summary>
    public interface IGizmoDrawSelectedModule : IGameModule
    {
        /// <summary>GameRoot 被选中时绘制细节线、标签等。</summary>
        void DrawGizmosSelected();
    }
}
