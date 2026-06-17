namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 模块默认优先级常量。业务可自定义 int，间隔留空便于插入。
    /// </summary>
    public static class ModulePriority
    {
        /// <summary>输入采集；最先 Update，供后续模块读快照。</summary>
        public const int Input = 0;

        /// <summary>GameTimeModule：Clock / Pipeline / Timer 基础设施。</summary>
        public const int Early = 100;

        /// <summary>GameFlowModule：宏观流程 FSM（Boot / 主菜单 / 战斗阶段名）。</summary>
        public const int GameFlow = 150;

        /// <summary>通用全局 Module 默认档（如 Archive）。</summary>
        public const int Normal = 500;

        /// <summary>核心玩法 / 规则 / 仿真（在 GameFlow 之后、UI 之前）。</summary>
        public const int GameLogic = 600;

        /// <summary>调试命令等收尾逻辑。</summary>
        public const int Late = 900;

        /// <summary>界面刷新；靠后执行以便读本帧玩法结果。</summary>
        public const int UI = 1000;
    }
}
