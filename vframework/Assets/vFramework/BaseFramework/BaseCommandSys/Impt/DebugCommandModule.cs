using System;
using BaseFramework.BaseGameRoot;

namespace BaseFramework.BaseCommandSys
{
    /// <summary>
    /// 注册 <see cref="ICommandRegistry"/> / <see cref="ICommandDispatcher"/>，供调试控制台与 MCP 使用。
    /// </summary>
    public sealed class DebugCommandModule : IGameModule
    {
        private readonly Action<ICommandRegistry> _registerExtra;
        private CommandRegistry _registry;
        private CommandDispatcher _dispatcher;

        public int Priority => ModulePriority.Late;

        /// <param name="registerExtra">各层追加命令，如 time.scale、archive.save。</param>
        public DebugCommandModule(Action<ICommandRegistry> registerExtra = null)
        {
            _registerExtra = registerExtra;
        }

        public void Init(IServiceRegistry services)
        {
            _registry = new CommandRegistry();
            _registry.Register(new HelpCommand(_registry));
            _registry.Register(new EchoCommand());
            _registry.Register(new TimeScaleCommand());
            _registerExtra?.Invoke(_registry);

            var context = new CommandContext(services);
            _dispatcher = new CommandDispatcher(_registry, context);

            services.Register<ICommandRegistry>(_registry);
            services.Register<ICommandDispatcher>(_dispatcher);
        }

        public void Update(float deltaTime)
        {
        }

        public void Dispose()
        {
            _dispatcher = null;
            _registry = null;
        }
    }
}
