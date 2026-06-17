using System;
using System.Collections.Generic;

namespace BaseFramework.BaseCommandSys
{
    public sealed class CommandRegistry : ICommandRegistry
    {
        private readonly Dictionary<string, IGameCommand> _commands =
            new Dictionary<string, IGameCommand>(StringComparer.OrdinalIgnoreCase);

        public void Register(IGameCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (string.IsNullOrWhiteSpace(command.Name))
                throw new ArgumentException("Command name cannot be empty.", nameof(command));

            _commands[command.Name] = command;
        }

        public bool TryGet(string name, out IGameCommand command)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                command = null;
                return false;
            }

            return _commands.TryGetValue(name, out command);
        }

        public IReadOnlyList<CommandDescriptor> ListDescriptors()
        {
            var list = new List<CommandDescriptor>(_commands.Count);
            foreach (IGameCommand command in _commands.Values)
            {
                list.Add(new CommandDescriptor(
                    command.Name,
                    command.Description,
                    command.Usage));
            }

            list.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return list;
        }
    }
}
