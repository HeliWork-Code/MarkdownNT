# MarkdownNT.Core DLL 使用说明

## 1. DLL 的作用

`MarkdownNT.Core.dll` 是 MarkdownNT 的无 UI 核心库，负责：

- 解析 MarkdownNT 文本；
- 识别标题、引用、无序列表、分隔线、特殊块、代码块、表格和段落；
- 解析表格表头、数据行以及列对齐方式；
- 将 MarkdownNT 文本导出为 HTML。
- 将 MarkdownNT 文本导出为 PDF。

核心库不依赖 Avalonia，也不包含编辑器、窗口或文件选择界面。

## 2. 获取 DLL

在核心项目目录执行 Release 构建：

```powershell
cd "M:\MarkdownNT\MarkdownNT\MarkdownNT\MarkdownNT.core"
dotnet build -c Release
```

生成位置：

```text
MarkdownNT.core\bin\Release\netstandard2.0\MarkdownNT.Core.dll
```

`netstandard2.0` DLL 可以被更多 .NET 项目引用，包括 .NET Framework 4.7.2+、.NET Core、.NET 5 及更高版本项目。

## 3. 内核构建身份

核心源文件开头提供：

```csharp
public const bool IS_OFFICIAL = true;
```

官方构建保持为 `true`。开发者发布修改版时应将其改为 `false`，编辑器会在内核版本后显示“（已修改）”。该标记是公开的构建声明，不等同于数字签名或在线哈希认证；正式认证仍应由独立的验证组件完成。

PDF 导出依赖 `PdfSharpCore`。如果通过项目引用或 NuGet 使用核心库，依赖会自动还原；如果手动复制 DLL，需要同时部署 `PdfSharpCore` 及其依赖程序集。

## 4. 在自己的项目中引用 DLL

推荐在使用者项目中创建 `libs` 文件夹，并复制：

```text
你的项目\
├─ libs\
│  └─ MarkdownNT.Core.dll
└─ YourApp.csproj
```

在 `YourApp.csproj` 中加入：

```xml
<ItemGroup>
  <Reference Include="MarkdownNT.Core">
    <HintPath>libs\MarkdownNT.Core.dll</HintPath>
    <Private>true</Private>
  </Reference>
</ItemGroup>
```

然后执行：

```powershell
dotnet restore
dotnet build
```

## 5. 解析 MarkdownNT 文本

```csharp
using MarkdownNT;

string markdown = """
# MarkdownNT 示例

这是一段 **加粗文本**。

- 第一项
- 第二项
""";

List<MarkdownBlock> blocks = MarkdownParser.Parse(markdown);

foreach (MarkdownBlock block in blocks)
{
    Console.WriteLine($"类型: {block.Type}");
    Console.WriteLine($"文本: {block.Text}");
}
```

`MarkdownBlock` 主要属性：


| 属性       | 用途                                 |
| ---------- | ------------------------------------ |
| `Type`     | 块类型，类型为`BlockType`            |
| `Level`    | 标题等级，范围为 1 到 6              |
| `Text`     | 标题、引用、列表、特殊块或段落的文本 |
| `Language` | 代码块语言标识                       |
| `Lines`    | 代码块或表格的原始行                 |

## 6. 导出 HTML

```csharp
using MarkdownNT;

string markdown = """
# Hello

这是 **MarkdownNT**。
""";

string html = HtmlExporter.ExportToHtml(markdown);
File.WriteAllText("output.html", html);
```

`HtmlExporter` 会生成一个包含基本 CSS 样式的完整 HTML 文档。普通文本会进行 HTML 实体转义，避免文本中的 `<`、`>` 和 `&` 破坏 HTML 结构。

## 7. 导出 PDF

```csharp
using MarkdownNT;

string markdown = "# MarkdownNT\n\n这是一个 PDF 示例。";
byte[] pdf = PdfExporter.ExportToPdf(markdown);
File.WriteAllBytes("output.pdf", pdf);
```

也可以直接写入文件流：

```csharp
using (FileStream stream = File.Create("output.pdf"))
{
    PdfExporter.ExportToPdf(markdown, stream);
}
```

当前版本支持标题、段落、引用、列表、分隔线、代码块和基础表格的 PDF 输出，并会自动查找系统中文字体并嵌入 PDF。Windows 通常会优先使用 Microsoft YaHei 或 SimSun。为避免字体集合在不同 PDF 阅读器中被错误选择，标题、正文和强调文本统一使用同一个已解析的中文字体文件；代码块会优先使用 Sarasa Mono SC 或 Noto Sans Mono CJK 等等宽中文字体。也可分别设置 `MARKDOWNNT_PDF_FONT` 和 `MARKDOWNNT_PDF_CODE_FONT` 指定字体文件。当前核心版本为 v1.1.4。

## 8. MarkdownNT 支持的语法

### 标题

```text
# 一级标题
## 二级标题
### 三级标题
```

支持一到六级标题。

### 引用

```text
> 这是一段引用
```

### 无序列表

```text
- 第一项
* 第二项
```

### 分隔线

```text
---
```

三个或更多连续的 `-` 会被识别为分隔线。

### MarkdownNT 代码块

代码块使用 `>>>语言` 或单独一行的 `>>>` 开始，使用代码块内单独一行的 `>>>` 结束：

```text
>>>csharp
Console.WriteLine("Hello MarkdownNT");
>>>
```

不填写语言时，默认语言为 `text`：

```text
>>>
plain text
>>>
```

### 特殊块

以下行会被识别为特殊块：（现在功能还未启用，详细请关注MarkdownNT2.0.0或3.0.0，欢迎为其添加功能）

```text
@meta title=示例
@audio file=demo.mp3
@speaker name=Alice
```

当前核心库会保留特殊块的原始文本，具体业务含义由使用者程序决定。

### 表格

```text
| 名称 | 数量 | 价格 |
| :--- | ---: | :---: |
| 苹果 | 2 | 10 |
```

表格支持：

- 第一行表头；
- 第二行分隔线；
- 左对齐：`:---`；
- 右对齐：`---:`；
- 居中对齐：`:---:`；
- 不完整行自动补充空单元格。

也可以使用没有表头分隔行的表格：

```text
| A | B |
| 1 | 2 |
```

## 9. 直接读取表格数据

如果程序只需要处理表格，可以先从解析结果中找到 `Table` 块，再调用 `ParseTable`：

```csharp
using MarkdownNT;

List<MarkdownBlock> blocks = MarkdownParser.Parse(markdown);

foreach (MarkdownBlock block in blocks)
{
    if (block.Type != BlockType.Table)
        continue;

    MarkdownTable table = MarkdownParser.ParseTable(block.Lines);

    if (table.HasHeader)
        Console.WriteLine(string.Join(" | ", table.Headers));

    foreach (IReadOnlyList<string> row in table.Rows)
        Console.WriteLine(string.Join(" | ", row));
}
```

## 10. 使用注意事项

- `MarkdownParser.Parse` 不负责读取文件，文件读取由调用方完成。
- 核心库不负责保存文件、自动保存或显示预览。
- 多行普通段落目前按行解析为多个 `Paragraph` 块。
- `>>>` 是 MarkdownNT 自定义代码块标记，不是标准 Markdown 的三个反引号。
- `HtmlExporter` 会转义代码块和行内文本内容，但不会把 MarkdownNT 特殊块解释成业务功能。
- 使用 DLL 时必须保证目标项目能够加载 `netstandard2.0` 程序集，并部署 PDF 导出所需的 `PdfSharpCore` 依赖。

## 11. 推荐的项目引用方式

如果使用者需要持续更新核心库，建议使用项目引用而不是手动复制 DLL：

```xml
<ItemGroup>
  <ProjectReference Include="..\MarkdownNT.Core\MarkdownNT.Core.csproj" />
</ItemGroup>
```

项目引用适合开发阶段；Release 发布或跨仓库分发时，再复制 `MarkdownNT.Core.dll` 或制作 NuGet 包。
