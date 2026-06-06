# ResTest — Unity 原生 AB 练习

完整三问演示见 **[AB学习演示说明.md](./AB学习演示说明.md)**。

## 快速开始

1. **vFramework → AB Demo → Apply Demo AB Labels**
2. **vFramework → Build Test AB**
3. 场景挂 **AbDemoRunner**，按说明文档绑定 Button
4. Play，依次点 Q1 / Q2 / Q3 按钮，看 Console

## 核心脚本

| 文件 | 作用 |
|------|------|
| `AbDemoRunner.cs` | 三问演示（注释含 API 说明） |
| `AbManifestLoader.cs` | Manifest + 依赖顺序加载 |
| `AbTestConfig.cs` | 包名与资源名 |
| `ResTest.cs` | 简化版：只 Load `UI/TestUI.prefab` |
| `Editor/AbDemoLabelApplier.cs` | 一键 Mark |

## 加载链（TestUI.prefab）

```
LoadFromFile(.../demo/ui/testui)
  → LoadAsset<GameObject>("TestUI")
  → Instantiate
```

目标资源：**`Assets/AssetBundle/UI/TestUI.prefab`**
