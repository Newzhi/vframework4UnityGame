using System.Collections.Generic;
using BaseFramework.BaseCommandSys;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 调试：强制切换流程状态（仅 Development）。用法：flow.goto MainMenu
    /// </summary>
    public sealed class FlowGotoCommand : IGameCommand
    {
        public string Name => "flow.goto";
        public string Description => "Change game flow state (development only).";
        public string Usage => "flow.goto <stateId>";

        public string Execute(IReadOnlyList<string> args, ICommandContext context)
        {
            if (!context.IsDevelopment)
                return "flow.goto is disabled outside development builds.";

            if (args.Count == 0)
                return "Missing stateId. Usage: " + Usage;

            var flow = context.TryGetService<IGameFlowService>();
            if (flow == null)
                return "IGameFlowService not registered.";

            string target = args[0];
            flow.ChangeState(target);
            return $"ChangeState -> {target} (current={flow.CurrentStateId})";
        }
    }
}
