using System;
using System.Collections.Generic;
using BaseFramework.BaseGameRoot;

namespace BaseFramework.BaseCommandSys
{
    /// <summary>
    /// 内置 echo 命令，用于验证调度链路与 MCP 桥接。
    /// </summary>
    public sealed class EchoCommand : IGameCommand
    {
        public string Name => "echo";
        public string Description => "Echo arguments back.";
        public string Usage => "echo [text...]";

        public string Execute(IReadOnlyList<string> args, ICommandContext context)
        {
            if (args.Count == 0)
                return string.Empty;

            return string.Join(" ", args);
        }
    }
}
