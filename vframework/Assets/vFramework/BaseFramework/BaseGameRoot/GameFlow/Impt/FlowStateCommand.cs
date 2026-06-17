using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BaseFramework.BaseCommandSys;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 调试：查询当前宏观流程状态。在 Bootstrap 中挂到 <c>DebugCommandModule</c> 的 registerExtra。
    /// </summary>
    public sealed class FlowStateCommand : IGameCommand
    {
        public string Name => "flow.state";
        public string Description => "Show current game flow state.";
        public string Usage => "flow.state";

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
