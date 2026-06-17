namespace BaseFramework.BaseCommandSys
{
    /// <summary>
    /// 命令描述，供 help 与 MCP list_tools 使用。
    /// </summary>
    public readonly struct CommandDescriptor
    {
        public string Name { get; }
        public string Description { get; }
        public string Usage { get; }

        public CommandDescriptor(string name, string description, string usage)
        {
            Name = name;
            Description = description;
            Usage = usage;
        }
    }
}
