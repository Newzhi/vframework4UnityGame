using System.Collections.Generic;
using System.Text;

namespace BaseFramework.BaseCommandSys
{
    /// <summary>
    /// 内置 help 命令。
    /// </summary>
    public sealed class HelpCommand : IGameCommand
    {
        private readonly ICommandRegistry _registry;

        public HelpCommand(ICommandRegistry registry)
        {
            _registry = registry;
        }

        public string Name => "help";
        public string Description => "List commands or show usage for one command.";
        public string Usage => "help [commandName]";

        public string Execute(IReadOnlyList<string> args, ICommandContext context)
        {
            if (args.Count == 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Available commands:");
                foreach (CommandDescriptor d in _registry.ListDescriptors())
                    sb.Append("  ").Append(d.Name).Append(" - ").AppendLine(d.Description);
                return sb.ToString().TrimEnd();
            }

            if (!_registry.TryGet(args[0], out IGameCommand command))
                return $"Unknown command: {args[0]}";

            return $"{command.Name}: {command.Description}\nUsage: {command.Usage}";
        }
    }
}
