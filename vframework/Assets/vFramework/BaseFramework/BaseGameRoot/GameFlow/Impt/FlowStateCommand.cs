using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BaseFramework.BaseCommandSys;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 调试命令：输出当前宏观流程态（Current / Previous / Elapsed）。
    /// 通过 <see cref="GameFlowModule.RegisterDebugCommands"/> 挂入 DebugCommandModule。
    /// </summary>
    public sealed class FlowStateCommand : IGameCommand
    {
        /// <inheritdoc />
        public string Name => "flow.state";

        /// <inheritdoc />
        public string Description => "Show current game flow state.";

        /// <inheritdoc />
        public string Usage => "flow.state";

        /// <inheritdoc />
        public string Execute(IReadOnlyList<string> args, ICommandContext context)
        {
            var flow = context.TryGetService<IGameFlowService>();
            if (flow == null)
                return "IGameFlowService not registered. Add GameFlowModule in Bootstrap.";

            var sb = new StringBuilder();
            sb.Append("Current=").Append(flow.CurrentStateId ?? "<none>");
            sb.Append(", Previous=").Append(flow.PreviousStateId ?? "<none>");
            sb.Append(", Elapsed=")
                .Append(flow.CurrentStateElapsedSeconds.ToString("F2", CultureInfo.InvariantCulture))
                .Append('s');
            return sb.ToString();
        }
    }
}
