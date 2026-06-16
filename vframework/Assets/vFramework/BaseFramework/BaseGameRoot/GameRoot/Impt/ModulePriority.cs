namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 模块默认优先级常量。业务可自定义 int，间隔留空便于插入。
    /// </summary>
    public static class ModulePriority
    {
        public const int Input = 0;
        public const int Early = 100;
        public const int Normal = 500;
        /// <summary>核心玩法 / 规则 / 仿真（在 Input 之后、UI 之前）。</summary>
        public const int GameLogic = 600;
        public const int Late = 900;
        public const int UI = 1000;
    }
}
