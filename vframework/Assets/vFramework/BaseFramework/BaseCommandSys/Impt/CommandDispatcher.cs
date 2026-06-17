using System;
using System.Collections.Generic;
using System.Text;

namespace BaseFramework.BaseCommandSys
{
    public sealed class CommandDispatcher : ICommandDispatcher
    {
        private readonly ICommandRegistry _registry;
        private readonly CommandContext _context;

        public CommandDispatcher(ICommandRegistry registry, CommandContext context)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public string Execute(string line)
        {
            if (!_context.IsDevelopment)
                return "Debug commands are disabled in this build.";

            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            line = line.Trim();
            if (line.StartsWith("/"))
                line = line.Substring(1).Trim();

            if (line.Length == 0)
                return string.Empty;

            _context.ClearReply();

            if (!TryParse(line, out string commandName, out List<string> args))
                return "Failed to parse command line.";

            if (!_registry.TryGet(commandName, out IGameCommand command))
                return $"Unknown command: {commandName}. Type 'help'.";

            try
            {
                string summary = command.Execute(args, _context);
                string extra = _context.ConsumeReply();
                if (!string.IsNullOrEmpty(extra))
                {
                    if (!string.IsNullOrEmpty(summary))
                        return summary + Environment.NewLine + extra;
                    return extra;
                }

                return summary ?? string.Empty;
            }
            catch (Exception ex)
            {
                return $"Command failed: {ex.Message}";
            }
        }

        public IReadOnlyList<CommandDescriptor> ListCommands() => _registry.ListDescriptors();

        private static bool TryParse(string line, out string commandName, out List<string> args)
        {
            args = new List<string>(4);
            commandName = null;

            int i = 0;
            while (i < line.Length && char.IsWhiteSpace(line[i]))
                i++;

            if (i >= line.Length)
                return false;

            int nameStart = i;
            while (i < line.Length && !char.IsWhiteSpace(line[i]))
                i++;

            commandName = line.Substring(nameStart, i - nameStart);

            while (i < line.Length)
            {
                while (i < line.Length && char.IsWhiteSpace(line[i]))
                    i++;

                if (i >= line.Length)
                    break;

                if (line[i] == '"')
                {
                    i++;
                    var sb = new StringBuilder();
                    while (i < line.Length && line[i] != '"')
                    {
                        if (line[i] == '\\' && i + 1 < line.Length)
                        {
                            i++;
                            sb.Append(line[i]);
                        }
                        else
                        {
                            sb.Append(line[i]);
                        }

                        i++;
                    }

                    if (i < line.Length && line[i] == '"')
                        i++;

                    args.Add(sb.ToString());
                    continue;
                }

                int argStart = i;
                while (i < line.Length && !char.IsWhiteSpace(line[i]))
                    i++;

                args.Add(line.Substring(argStart, i - argStart));
            }

            return true;
        }
    }
}
