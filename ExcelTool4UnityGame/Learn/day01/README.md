# Day01 — 读取 Excel 的每一行

> 学习目标：理解工具是怎么把 `.xlsx` 变成「一行一行的字典」的。  
> 本课**只做读取和打印**，不做解析、校验、生成代码。

---

## 1. 本目录有什么

```
Learn/day01/
├── README.md         ← 本文档
├── MiniExcel.md      ← MiniExcel API 速查（读/写/参数）
└── ReadExcelRows.cs  ← 带注释的演示代码（不参与编译，仅供学习）
```

没有独立项目，直接阅读代码即可。想运行可临时把逻辑拷到 `ExcelTool/ExcelTool/Program.cs`，或用正式工具的 `ExcelReader.ReadRows()` 对照。

---

## 2. 准备工作

| 项目 | 说明 |
|---|---|
| 测试表 | `TestExcels/Hero.xlsx`（没有可运行 `ExcelTool/create-sample.bat`） |
| 正式工具 | `ExcelTool/ExcelTool/Excel/ExcelReader.cs` 已实现同样读取 |

---

## 3. 核心代码就三行

```csharp
var rows = MiniExcel.Query(path, useHeaderRow: false);

foreach (var row in rows)
{
    var cells = (IDictionary<string, object?>)row;
    // cells["A"] → A 列
    // cells["B"] → B 列
}
```

完整带注释版本见 [`ReadExcelRows.cs`](./ReadExcelRows.cs)。  
**MiniExcel 各方法、参数说明**见 [`MiniExcel.md`](./MiniExcel.md)。

### 为什么 `useHeaderRow: false`？

| A | B | C |
|---|---|---|
| ##var | id | name |
| ##type | uint | string |
| | 10000 | nick |

- `true`：第一行 `##var` 被当成列名，后面 key 变成 `id`、`name`，**丢失 A 列的 `##var`**
- `false`：每列用 **A、B、C、D** 做 key，与 Excel 界面一致

更多参数（`sheetName`、`startCell`、`Query<T>` 等）见 [MiniExcel.md](./MiniExcel.md)。

---

## 4. 读出来后长什么样？

`Hero.xlsx` 若由 `create-sample.bat` 生成，第 1 行可能是列字母表头（A、B、C、D），`##var` 从第 2 行开始：

```
第 2 行 (rowIndex=1)
  列 A : ##var
  列 B : id
  列 C : name
  列 D : hp
```

第 6 行（第一条数据）：

```
第 6 行 (rowIndex=5)
  列 A : (空)
  列 B : 10000
  列 C : nick
  列 D : 100
```

> 正式工具的 `SchemaParser` 通过 A 列**内容**查找 `##var`，不依赖固定行号。

**如何判断行类型？** 看 A 列（Day02 再展开）：

| A 列 | 作用 |
|---|---|
| `##var` | 字段名 |
| `##type` | 类型 |
| `##Alias` / `##desc` | 注释（可选） |
| 空 | 数据行 |

---

## 5. 与正式工具的关系

```
Excel 文件
  → MiniExcel.Query()     ← Day01 / ReadExcelRows.cs
  → List<行字典>
  → SchemaParser.Parse()  ← Day02
  → 生成 cs / bytes
```

正式代码：`ExcelTool/ExcelTool/Excel/ExcelReader.cs` → `ReadRows()`。

---

## 6. 练习建议

1. 打开 `ReadExcelRows.cs` 和 `Hero.xlsx`，逐行对照理解。
2. 阅读 `ExcelReader.cs`，看正式工具如何封装同样逻辑。
3. 思考：若 `useHeaderRow` 为 `true`，`##var` 行还会被正确识别吗？

---

## 下一课（Day02）

- 根据 A 列识别 `##var` / `##type`
- 拼成列定义 + 数据行
