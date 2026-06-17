namespace BaseFramework.BaseCommandSys
{
    /// <summary>
    /// 命令执行上下文：受控访问 IOC，禁止业务热路径使用。
    /// </summary>
    public interface ICommandContext
    {
        /// <summary>是否允许执行调试命令（Editor / Development Build）。</summary>
        bool IsDevelopment { get; }

        /// <summary>从容器获取服务；未注册时返回 null。</summary>
        T TryGetService<T>() where T : class;

        /// <summary>追加回显文本（控制台 / MCP 返回）。</summary>
        void Reply(string message);
    }
}
