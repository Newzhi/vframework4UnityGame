using System.Text;
using BaseFramework.BaseGameRoot;

namespace BaseFramework.BaseCommandSys
{
    /// <summary>
    /// <see cref="ICommandContext"/> 默认实现。
    /// </summary>
    public sealed class CommandContext : ICommandContext
    {
        private readonly IServiceRegistry _services;
        private readonly StringBuilder _reply = new StringBuilder(256);

        public CommandContext(IServiceRegistry services)
        {
            _services = services;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool IsDevelopment => true;
#else
        public bool IsDevelopment => false;
#endif

        public T TryGetService<T>() where T : class
        {
            if (_services == null)
                return null;
            return _services.TryGet(out T service) ? service : null;
        }

        public void Reply(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            if (_reply.Length > 0)
                _reply.AppendLine();
            _reply.Append(message);
        }

        internal void ClearReply() => _reply.Length = 0;

        internal string ConsumeReply()
        {
            if (_reply.Length == 0)
                return string.Empty;

            string text = _reply.ToString();
            _reply.Length = 0;
            return text;
        }
    }
}
