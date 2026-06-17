# BaseCommandSys

> 路径：`BaseFramework/BaseCommandSys/`  
> 命名空间：`BaseFramework.BaseCommandSys`

调试 / MCP 用文本指令调度：`ICommandRegistry` + `ICommandDispatcher`。业务 Module 不依赖本模块。

## Bootstrap

```csharp
modules.AddModule(new DebugCommandModule(registry =>
{
    registry.Register(new MyGameplayCommand());
}));
```

## 执行

```csharp
var cmd = services.Get<ICommandDispatcher>();
string result = cmd.Execute("help");
string result2 = cmd.Execute("/echo hello");
```

## MCP

外部将 MCP tool 调用映射为 `ICommandDispatcher.Execute(line)`；`ListCommands()` 对应 tool 列表。

## 扩展命令

实现 `IGameCommand`，在 `Name` 中使用点分命名（如 `time.scale`）：

```csharp
public string Execute(IReadOnlyList<string> args, ICommandContext context)
{
    var clock = context.TryGetService<IGameTimeClock>();
    // ...
}
```

Release 非 Development Build 下 `Execute` 返回 disabled 提示。

## 目录

```text
BaseCommandSys/
├── Interfaces/
└── Impt/
```
