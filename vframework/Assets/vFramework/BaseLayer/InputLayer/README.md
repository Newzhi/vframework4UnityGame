# InputLayer

> 路径：`BaseLayer/InputLayer/`  
> 命名空间：`BaseLayer.Input`

全局输入快照：`InputSnapshot` + `IInputService`，由 `InputModule`（`ModulePriority.Input`）每帧采集。

## Bootstrap

```csharp
modules.AddModule(new InputModule());
// Init 内自动 Register<IInputService>
```

## 消费

```csharp
_input = services.Get<IInputService>();
InputSnapshot s = _input.Current;
if (s.Attack.PressedThisFrame) { }
```

## 平台

- PC：`KeyboardMouseInputProvider`（WASD + 鼠标）
- 移动：`TouchInputProvider`（左半屏虚拟摇杆 + 触摸）

## 目录

```text
InputLayer/
├── Interfaces/   InputSnapshot, IInputService, IInputDeviceProvider …
└── Impt/         InputModule, InputService, Providers
```
