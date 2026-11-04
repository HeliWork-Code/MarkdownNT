# API 概览

## 1. MarkdownNT.Core

`MarkdownNT.Core` 提供无 UI 的 Markdown 解析与导出能力，核心对象包括：

### `MarkdownParser`

```csharp
List<MarkdownBlock> blocks = MarkdownParser.Parse(markdownText);
```

用途：

- 解析标题、引用、列表、分隔线、特殊块、代码块、段落和表格
- 统一文档块结构

### `HtmlExporter`

```csharp
string html = HtmlExporter.ExportToHtml(markdownText);
```

用途：

- 将 MarkdownNT 文本导出为一个完整 HTML 文档
- 自动处理 HTML 转义、列表、表格和代码块输出

### `PdfExporter`

```csharp
byte[] pdf = PdfExporter.ExportToPdf(markdownText);
```

用途：

- 导出 PDF 流或文件
- 适配中英文字体方案与代码块格式

## 2. `MarkdownBlock`

```csharp
public class MarkdownBlock
{
    public BlockType Type { get; set; }
    public int Level { get; set; }
    public string Text { get; set; }
    public string Language { get; set; }
    public IReadOnlyList<string> Lines { get; set; }
}
```

代表解析后的文档块，每个块都有类型与内容。

## 3. `BlockType`

支持的块类型包括：

- `Heading`
- `Quote`
- `ListItem`
- `Divider`
- `Special`
- `CodeBlock`
- `Table`
- `Paragraph`

## 4. 插件接口

### `IMdNTCode`

```csharp
public interface IMdNTCode
{
    string Id { get; }
    string Name { get; }
    Version Version { get; }
    IEnumerable<MdNTCodeRule> GetRules();
}
```

### `MdNTCodeRule`

```csharp
public sealed class MdNTCodeRule
{
    public string Name { get; set; }
    public string Keyword { get; set; }
    public string Expression { get; set; }
    public int Priority { get; set; }
    public MdNTCodeStyle Style { get; set; }
}
```

### `MdNTCodeStyle`

```csharp
public sealed class MdNTCodeStyle
{
    public string? Foreground { get; set; }
    public string? Background { get; set; }
    public string? DarkForeground { get; set; }
    public string? DarkBackground { get; set; }
    public string? FontFamily { get; set; }
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public bool Strikethrough { get; set; }
    public string? Effect { get; set; }
}
```

## 5. 参考文件

- 本仓库中的 `MarkdownNT.Plugin.Abstractions/如何编写插件.md`
- 本仓库中的 `MarkdownNT.Plugin.Abstractions/MdNTCode.cs`
- 本仓库中的 `MarkdownNT.Plugin.Test/MdNTCodeTestPlugin.cs`
