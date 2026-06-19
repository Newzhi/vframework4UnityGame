namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 宏观流程状态 Id 常量。框架不内置 Procedure 实现；Bootstrap / 热更层 Register 时使用这些 Id 或业务自有常量。
    /// </summary>
    public static class GameFlowIds
    {
        /// <summary>启动 / 初始化（TryStart 之后的首屏编排）。</summary>
        public const string Boot = "Boot";

        /// <summary>主菜单 / 大厅。</summary>
        public const string MainMenu = "MainMenu";

        // --- 扩展示例：取消注释并实现对应 IGameFlowState，再在 Bootstrap Register ---
        // /// <summary>场景 / 资源加载中。</summary>
        // public const string Loading = "Loading";
        // /// <summary>局内（非战斗 UI、大地图等）。</summary>
        // public const string InGame = "InGame";
        // /// <summary>战斗仿真阶段；Enter 内 Activate BattleEcsModule。</summary>
        // public const string Battle = "Battle";
        // /// <summary>暂停 overlay；或与 InGame 并列，按项目选型。</summary>
        // public const string Pause = "Pause";
        // /// <summary>关卡结算。</summary>
        // public const string Settlement = "Settlement";
    }
}
