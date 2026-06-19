namespace BaseLayer.ConfigTable
{
    /// <summary>
    /// 配置表模块就绪标记。表数据查询请使用热更层 <c>Game.Config.GameConfigTables.Instance</c>。
    /// </summary>
    public interface IConfigTableService
    {
        /// <summary>bytes 已预加载且 <c>GameConfigTables.Initialize</c> 已完成。</summary>
        bool IsReady { get; }
    }
}
