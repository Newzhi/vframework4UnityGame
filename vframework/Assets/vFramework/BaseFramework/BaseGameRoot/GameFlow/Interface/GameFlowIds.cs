namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 内置 MVP 流程状态 Id 常量。热更层新增状态时请在此或业务侧定义常量，避免魔法字符串。
    /// </summary>
    public static class GameFlowIds
    {
        /// <summary>启动 / 初始化（Patch、热更、首屏加载等）。</summary>
        public const string Boot = "Boot";

        /// <summary>主菜单 / 大厅。</summary>
        public const string MainMenu = "MainMenu";

        // --- 扩展示例（热更层实现对应 IGameFlowState 后 Register）---
        // public const string Loading = "Loading";
        // public const string InGame = "InGame";
        // public const string Battle = "Battle";
        // public const string Pause = "Pause";
        // public const string Settlement = "Settlement";
    }
}
