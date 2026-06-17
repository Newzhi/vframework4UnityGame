using System.Collections.Generic;

namespace BaseFramework.BaseCommandSys
{
    /// <summary>
    /// 解析并调度文本指令；游戏内控制台与 MCP 共用。
    /// </summary>
    public interface ICommandDispatcher
    {
        /// <summary>执行一行指令，如 /time.scale 2 或 help。</summary>
        string Execute(string line);

        IReadOnlyList<CommandDescriptor> ListCommands();
    }
}
