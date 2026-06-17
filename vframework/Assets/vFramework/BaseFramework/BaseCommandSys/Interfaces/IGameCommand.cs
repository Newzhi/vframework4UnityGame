using System.Collections.Generic;

namespace BaseFramework.BaseCommandSys
{
    /// <summary>
    /// 单条调试 / MCP 指令，类似 MC 子命令。
    /// </summary>
    public interface IGameCommand
    {
        /// <summary>命令名，如 help、time.scale、archive.save。</summary>
        string Name { get; }

        string Description { get; }

        /// <summary>用法示例，如 time.scale &lt;float&gt;。</summary>
        string Usage { get; }

        /// <summary>执行；返回单行摘要（也可通过 context.Reply 输出多行）。</summary>
        string Execute(IReadOnlyList<string> args, ICommandContext context);
    }
}
