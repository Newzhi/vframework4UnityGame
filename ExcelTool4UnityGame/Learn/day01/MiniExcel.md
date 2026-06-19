# MiniExcel API 速查（Day01）

> 官方仓库：[mini-software/MiniExcel](https://github.com/mini-software/MiniExcel)  
> NuGet：`MiniExcel` · 命名空间：`using MiniExcelLibs;`  
> 本仓库版本：`1.34.2`（见 `ExcelTool/ExcelTool/ExcelTool.csproj`）

MiniExcel 是轻量级 Excel 读写库，**低内存、API 简单**。本导表工具主要用 **Query（读）** 和 **SaveAs（写样例表）**。

---

## 1. 入口类

所有常用方法都在静态类 **`MiniExcel`** 上：

```csharp
using MiniExcelLibs;

// 读
var rows = MiniExcel.Query(path, useHeaderRow: false);

// 写
MiniExcel.SaveAs(path, data);
```

也支持从 **`Stream`** 读写（适合网络流、不落地临时文件）：

```csharp
using var stream = File.OpenRead(path);
var rows = stream.Query(useHeaderRow: false);

using var outStream = File.Create(path);
outStream.SaveAs(data);
```

---

## 2. 读取 API（Query 系列）

### 2.1 `Query` — 动态行（本工具用这个）

```csharp
IEnumerable<dynamic> Query(
    string path,
    bool useHeaderRow = false,   // 默认 false
    string? sheetName = null,    // null = 第一个 sheet
    string? startCell = "A1",    // 从哪个单元格开始读
    IConfiguration? configuration = null
)
```

**返回值：** 可 `foreach` 的延迟序列；每一行是 `dynamic`，实际类型为 `IDictionary<string, object?>`。

```csharp
foreach (var row in MiniExcel.Query(path, useHeaderRow: false))
{
    var cells = (IDictionary<string, object?>)row;

    // useHeaderRow: false → key 是列字母
    var a = cells.TryGetValue("A", out var v) ? v : null;

    // useHeaderRow: true → key 是第一行表头文字
    // var id = cells["id"];
}
```

| `useHeaderRow` | 列 key 是什么 | 第一行是否当数据返回 |
|---|---|---|
| `false`（默认） | `A`, `B`, `C`, `D`… | **会**返回（含 `##var` 那行） |
| `true` | 第一行单元格文字（如 `id`, `name`） | **不会**（第一行被消费成列名） |

> **本导表工具必须用 `false`**，因为要在 A 列读 `##var` / `##type` 行标记。

---

### 2.2 `Query<T>` — 强类型行

第一行是表头、且每行能映射到一个 C# 类时使用：

```csharp
public class HeroRow
{
    public uint id { get; set; }
    public string name { get; set; }
    public float hp { get; set; }
}

// 第一行必须是 id | name | hp 这种表头
var rows = MiniExcel.Query<HeroRow>(path, useHeaderRow: true);
```

约束：`T` 需有**无参公共构造函数**，属性名与表头列名对应（大小写不敏感）。

**不适合**本项目的 `##var` 多行表头格式，所以正式工具没用这个重载。

---

### 2.3 `QueryAsDataTable` — 读成 DataTable

```csharp
DataTable table = MiniExcel.QueryAsDataTable(
    path,
    useHeaderRow: true,   // 默认 true
    sheetName: null,
    startCell: "A1"
);
```

适合传统 ADO.NET / 需要 `DataTable` 的场景。导表工具用 `List<Dictionary>` 更轻，未采用。

---

### 2.4 异步版本

```csharp
var rows = await MiniExcel.QueryAsync(path, useHeaderRow: false);
var table = await MiniExcel.QueryAsDataTableAsync(path, useHeaderRow: true);
```

参数与同步版相同，大量文件或 UI 程序可用。

---

### 2.5 常用可选参数

#### `sheetName` — 指定工作表

```csharp
// 只读名为 "Hero" 的 sheet；null 表示第一个 sheet
var rows = MiniExcel.Query(path, sheetName: "Hero", useHeaderRow: false);
```

#### `startCell` — 从指定单元格开始读

```csharp
// 从 B3 开始读，上方的行不会出现
var rows = MiniExcel.Query(path, useHeaderRow: false, startCell: "B3");
```

表头前有说明文字、或数据不从 A1 开始时有用。本工具从整表扫描 `##var`，一般从 `A1` 即可。

#### 获取所有 sheet 名

```csharp
var names = MiniExcel.GetSheetNames(path);
// 例如: ["Sheet1", "Hero", "Item"]
```

---

## 3. 写入 API（SaveAs 系列）

创建样例表 `TestExcels/Hero.xlsx` 时用到（见 `ExcelTool/Sample/SampleDataCreator.cs`）。

### 3.1 `SaveAs` — 导出为新文件

```csharp
void SaveAs(
    string path,
    object value,
    bool printHeader = true,      // 是否写表头行
    string? sheetName = null,
    ExcelType excelType = ExcelType.UNKNOWN,
    IConfiguration? configuration = null,
    bool overwriteFile = false    // 是否覆盖已存在文件
)
```

**`value` 常见类型：**

| 类型 | 说明 |
|---|---|
| `IEnumerable<Dictionary<string, object>>` | 每行一个字典，key 为列名（如 `A`,`B`） |
| `IEnumerable<T>`（POCO / 匿名对象） | 属性名作列头 |
| `DataTable` | 整张表 |

**用字典写 `##var` 行（与 SampleDataCreator 相同）：**

```csharp
var rows = new List<Dictionary<string, object>>
{
    new() { ["A"] = "##var",  ["B"] = "id",   ["C"] = "name" },
    new() { ["A"] = "##type", ["B"] = "uint", ["C"] = "string" },
    new() { ["A"] = "",       ["B"] = "10000", ["C"] = "nick" },
};

MiniExcel.SaveAs("Hero.xlsx", rows, printHeader: true);
```

`printHeader: true` 时可能多出一行列字母表头（A/B/C/D），读的时候注意行号偏移。

### 3.2 异步

```csharp
await MiniExcel.SaveAsAsync(path, rows);
```

---

## 4. 单元格值的实际类型

`Query` 读出的 `object?` **不一定是 string**：

| Excel 内容 | 常见 CLR 类型 |
|---|---|
| 文字 | `string` |
| 整数 | `double` 或 `long`（库版本/格式有关） |
| 小数 | `double` |
| 日期 | `DateTime` |
| 布尔 | `bool` |

因此打印或解析时建议做转换（见 `ReadExcelRows.cs` 的 `FormatCell`，或 `uint.TryParse(value.ToString(), ...)`）。

---

## 5. 本仓库中的用法对照

| 位置 | API | 用途 |
|---|---|---|
| `Learn/day01/ReadExcelRows.cs` | `Query(path, useHeaderRow: false)` | 学习：逐行打印 |
| `ExcelTool/Excel/ExcelReader.cs` | `Query(...).ToList()` | 正式：读原始行 |
| `ExcelTool/Sample/SampleDataCreator.cs` | `SaveAs(path, rows)` | 生成样例 xlsx |

**ExcelReader 封装：**

```csharp
return MiniExcel.Query(filePath, useHeaderRow: false)
    .Cast<IDictionary<string, object?>>()
    .Select(row => row.ToDictionary(kv => kv.Key, kv => kv.Value))
    .ToList();
```

`.ToList()` 一次性载入内存；表很大时可保持 `IEnumerable` 延迟遍历以省内存（MiniExcel 官方建议）。

---

## 6. 与本项目相关的参数选择

| 场景 | 推荐 |
|---|---|
| 读 `##var` 表 | `useHeaderRow: false` |
| 普通「第一行表头 + 数据」表 | `useHeaderRow: true` + `Query<T>` |
| 多 sheet 项目 | `GetSheetNames` + `sheetName` |
| 表前有废话行 | `startCell: "A5"` 等 |
| 写样例 / 导出简单表 | `SaveAs` + `Dictionary` 行 |

---

## 7. 本课未涉及、后续可能用到的 API

| API | 用途 |
|---|---|
| `SaveAsByTemplate` | 按模板填充 Excel |
| `Insert` / `InsertAsync` | 追加行或追加 sheet |
| `Query` + CSV 路径 | 同样 API 可读 `.csv` |
| `MiniExcelLibs.OpenXml` 配置类 | 日期格式、空值处理等高级选项 |

需要时查官方 README：https://github.com/mini-software/MiniExcel

---

## 8. 最小记忆卡片

```
读整表、认 ## 行标记  →  Query(path, useHeaderRow: false)
读普通表头表          →  Query<T>(path, useHeaderRow: true)
读成 DataTable        →  QueryAsDataTable(path)
指定 sheet            →  sheetName: "Hero"
从 B3 开始读          →  startCell: "B3"
写文件                →  SaveAs(path, IEnumerable<字典或对象>)
列 key（false）       →  "A" "B" "C"
列 key（true）        →  第一行文字
```
