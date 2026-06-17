using System.Collections.Generic;
using BaseFramework.BaseCommandSys;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 调试命令：Development 下强制 <see cref="IGameFlowService.ChangeState"/>。
    /// 未知 stateId 时 Service 打 Error 日志。
    /// </summary>
    public sealed class FlowGotoCommand : IGameCommand
    {
        /// <inheritdoc />
        public string Name => "flow.goto";

        /// <inheritdoc />
        public string Description => "Change game flow state (development only).";

        /// <inheritdoc />
        public string Usage => "flow.goto <stateId>";

        /// <inheritdoc />
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
