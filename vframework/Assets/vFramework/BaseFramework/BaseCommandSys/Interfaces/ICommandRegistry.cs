using System.Collections.Generic;

namespace BaseFramework.BaseCommandSys
{
    /// <summary>
    /// 命令注册表；Bootstrap 或各层 Module 在 Init 阶段注册。
    /// </summary>
    public interface ICommandRegistry
    {
        void Register(IGameCommand command);

        bool TryGet(string name, out IGameCommand command);

        IReadOnlyList<CommandDescriptor> ListDescriptors();
    }
}
