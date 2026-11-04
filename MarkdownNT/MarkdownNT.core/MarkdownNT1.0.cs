using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;

namespace MarkdownNT
{
    /// <summary>MarkdownNT 内核的构建身份信息。</summary>
    public static class MarkdownNTIdentity
    {
        /// <summary>
        /// 是否声明为 MarkdownNT 官方构建。开发者创建修改版时应将其改为 false。
        /// </summary>
        public const bool IS_OFFICIAL = true;
    }

    /// <summary>MarkdownNT 文档块的类型。</summary>
    public enum BlockType
    {
        /// <summary>标题。</summary>
        Heading,
        /// <summary>引用。</summary>
        Quote,
        /// <summary>无序列表项。</summary>
        ListItem,
        /// <summary>分隔线。</summary>
        Divider,
        /// <summary>MarkdownNT 特殊块。</summary>
        Special,
        /// <summary>代码块。</summary>
        CodeBlock,
        /// <summary>管道表格。</summary>
        Table,
        /// <summary>普通段落。</summary>
        Paragraph
    }

    /// <summary>MarkdownNT 解析后的文档块。</summary>
    public class MarkdownBlock
    {
        /// <summary>文档块类型。</summary>
        public BlockType Type { get; set; }
        /// <summary>标题等级，范围为 1 到 6。</summary>
        public int Level { get; set; }
        /// <summary>块的文本内容。</summary>
        public string Text { get; set; } = "";
        /// <summary>代码块的语言标识。</summary>
        public string Language { get; set; } = "";
        /// <summary>代码块或表格的原始行。</summary>
        public IReadOnlyList<string> Lines { get; set; } = Array.Empty<string>();
    }

    /// <summary>表格列的对齐方式。</summary>
    public enum TableColumnAlignment
    {
        /// <summary>左对齐。</summary>
        Left,
        /// <summary>居中对齐。</summary>
        Center,
        /// <summary>右对齐。</summary>
        Right
    }

    /// <summary>解析后的 MarkdownNT 表格。</summary>
    public class MarkdownTable
    {
        /// <summary>是否存在表头。</summary>
        public bool HasHeader { get; set; }
        /// <summary>表头单元格。</summary>
        public IReadOnlyList<string> Headers { get; set; } = Array.Empty<string>();
        /// <summary>表格数据行。</summary>
        public IReadOnlyList<IReadOnlyList<string>> Rows { get; set; } = Array.Empty<IReadOnlyList<string>>();
        /// <summary>每一列的对齐方式。</summary>
        public IReadOnlyList<TableColumnAlignment> Alignments { get; set; } = Array.Empty<TableColumnAlignment>();
    }

    /// <summary>MarkdownNT 文本解析器。</summary>
    public static class MarkdownParser
    {
        private static readonly Regex DividerRegex = new Regex(@"^\s*-{3,}\s*$");
        private static readonly Regex HeadingRegex = new Regex(@"^(#{1,6})\s+(.*)");
        private static readonly Regex ListItemRegex = new Regex(@"^\s*[-*][ \t]+(.*)$");
        private static readonly Regex SeparatorCellRegex = new Regex(@"^:?-+:?$");

        /// <summary>尝试识别代码块分隔符。</summary>
        /// <param name="line">待解析的行。</param>
        /// <param name="fence">该行的 fence 字符串（重复的 fence 字符）。</param>
        /// <param name="isEnd">该分隔符是否标识代码块结束。</param>
        /// <param name="language">分隔符中的语言标识（已规范化）。</param>
        /// <returns>如果该行属于代码块分隔符，则返回 true。</returns>
        /// <summary>
        /// 识别任意类型的代码块 fence，并返回 fence 字符串、是否为结束 fence（无语言标记）及语言 token（若有）。
        /// </summary>
        public static bool TryParseCodeBlockDelimiter(string line, out string fence, out bool isEnd, out string language)
        {
            fence = "";
            isEnd = false;
            language = "";
            string trimmed = (line ?? "").Trim();
            if (string.IsNullOrEmpty(trimmed)) return false;

            // count leading repeated fence char (`, ~, >)
            char c = trimmed[0];
            if (c != '`' && c != '~' && c != '>') return false;

            int fenceLength = 0;
            while (fenceLength < trimmed.Length && trimmed[fenceLength] == c)
                fenceLength++;

            // a run of fewer than three '>' is a Markdown blockquote, not a code fence;
            // MarkdownNT keeps '>>>' (and longer) as its custom fence character
            if (c == '>' && fenceLength < 3) return false;

            fence = new string(c, fenceLength);
            string trailing = trimmed.Substring(fenceLength).Trim();

            if (string.IsNullOrEmpty(trailing))
            {
                // bare fence line: considered an end when closing against the same opening fence;
                // caller (Parse) will decide based on currentFence. Here mark as isEnd=true to indicate no language token.
                isEnd = true;
                language = "";
                return true;
            }

            // language token may include parameters (e.g. "javascript linenums"), take first token
            var first = trailing.Split(new[] { ' ', '\t' }, 2)[0];
            language = NormalizeLanguage(first);
            isEnd = false;
            return true;
        }

        /// <summary>将原始语言标识规范化为宿主支持的标识（别名映射）。</summary>
        /// <param name="raw">原始语言标识。</param>
        /// <returns>规范化后的语言标识。</returns>
        private static string NormalizeLanguage(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var token = raw.Trim().ToLowerInvariant();
            if (token.StartsWith(".")) token = token.Substring(1);

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "js", "javascript" }, { "jsx", "javascript" }, { "ts", "typescript" }, { "tsx", "typescript" },
                { "py", "python" }, { "rb", "ruby" }, { "rs", "rust" },
                { "c#", "csharp" }, { "cs", "csharp" }, { "csharp", "csharp" },
                { "cpp", "cpp" }, { "c++", "cpp" }, { "h", "cpp" }, { "hpp", "cpp" },
                { "java", "java" }, { "go", "go" }, { "php", "php" },
                { "html", "html" }, { "xml", "xml" }, { "json", "json" },
                { "yaml", "yaml" }, { "yml", "yaml" }, { "bash", "bash" }, { "sh", "bash" }, { "zsh", "bash" },
                { "ps1", "powershell" }, { "ps", "powershell" }, { "kotlin", "kotlin" }, { "swift", "swift" }
            };

            if (map.TryGetValue(token, out var normalized)) return normalized;

            var m = Regex.Match(token, "^[a-z0-9_+-]+$");
            if (m.Success) return m.Value;
            return token;
        }

        /// <summary>将 MarkdownNT 文本解析为文档块列表。</summary>
        /// <param name="markdownText">待解析的文本。</param>
        /// <returns>文档块列表。</returns>
        public static List<MarkdownBlock> Parse(string markdownText)
        {
            var blocks = new List<MarkdownBlock>();
            string[] lines = (markdownText ?? "").Replace("\r\n", "\n").Split('\n');
            var tableLines = new List<string>();
            var codeLines = new List<string>();
            bool inCode = false;
            string codeLanguage = "";
            string? currentFence = null;

            void FlushTable()
            {
                if (tableLines.Count == 0)
                    return;
                blocks.Add(new MarkdownBlock { Type = BlockType.Table, Lines = tableLines.ToArray() });
                tableLines.Clear();
            }

            foreach (string rawLine in lines)
            {
                string normalized = rawLine.Trim();
                if (TryParseCodeBlockDelimiter(normalized, out string fence, out bool isEnd, out string language))
                {
                    if (inCode)
                    {
                        // only close code block when a bare fence (isEnd) matches the opening fence
                        if (isEnd && fence == currentFence)
                        {
                            blocks.Add(new MarkdownBlock
                            {
                                Type = BlockType.CodeBlock,
                                Language = codeLanguage,
                                Lines = codeLines.ToArray()
                            });
                            inCode = false;
                            currentFence = null;
                            codeLines.Clear();
                        }
                        else
                        {
                            codeLines.Add(rawLine.TrimEnd('\r'));
                        }
                    }
                    else
                    {
                        FlushTable();
                        inCode = true;
                        currentFence = fence;
                        codeLanguage = string.IsNullOrEmpty(language) ? "text" : language;
                        codeLines.Clear();
                    }
                    continue;
                }

                if (inCode)
                {
                    codeLines.Add(rawLine.TrimEnd('\r'));
                    continue;
                }

                if (normalized.StartsWith("|", StringComparison.Ordinal))
                {
                    tableLines.Add(normalized);
                    continue;
                }
                FlushTable();

                if (DividerRegex.IsMatch(normalized))
                {
                    blocks.Add(new MarkdownBlock { Type = BlockType.Divider });
                    continue;
                }

                Match heading = HeadingRegex.Match(normalized);
                if (heading.Success)
                {
                    blocks.Add(new MarkdownBlock
                    {
                        Type = BlockType.Heading,
                        Level = heading.Groups[1].Value.Length,
                        Text = heading.Groups[2].Value
                    });
                    continue;
                }

                if (normalized.StartsWith(">", StringComparison.Ordinal))
                {
                    blocks.Add(new MarkdownBlock
                    {
                        Type = BlockType.Quote,
                        Text = normalized.Substring(1).Trim()
                    });
                    continue;
                }

                Match list = ListItemRegex.Match(normalized);
                if (list.Success)
                {
                    blocks.Add(new MarkdownBlock { Type = BlockType.ListItem, Text = list.Groups[1].Value });
                    continue;
                }

                if (normalized.StartsWith("@meta", StringComparison.Ordinal) ||
                    normalized.StartsWith("@audio", StringComparison.Ordinal) ||
                    normalized.StartsWith("@speaker", StringComparison.Ordinal))
                {
                    blocks.Add(new MarkdownBlock { Type = BlockType.Special, Text = normalized });
                    continue;
                }

                if (normalized.Length > 0)
                    blocks.Add(new MarkdownBlock { Type = BlockType.Paragraph, Text = normalized });
            }

            FlushTable();
            if (inCode)
            {
                blocks.Add(new MarkdownBlock
                {
                    Type = BlockType.CodeBlock,
                    Language = codeLanguage.Length == 0 ? "text" : codeLanguage,
                    Lines = codeLines.ToArray()
                });
            }
            return blocks;
        }

        /// <summary>解析表格的表头、数据行和列对齐方式。</summary>
        /// <param name="tableLines">表格原始行。</param>
        /// <returns>解析后的表格。</returns>
        public static MarkdownTable ParseTable(IReadOnlyList<string> tableLines)
        {
            var parsed = (tableLines ?? Array.Empty<string>()).Select(SplitTableRow).ToList();
            int columns = parsed.Count == 0 ? 0 : parsed.Max(row => row.Count);
            bool hasHeader = parsed.Count >= 2 && parsed[1].Count > 0 &&
                             parsed[1].All(cell => SeparatorCellRegex.IsMatch(cell));
            var alignments = new List<TableColumnAlignment>();

            if (hasHeader)
            {
                for (int i = 0; i < columns; i++)
                {
                    string cell = i < parsed[1].Count ? parsed[1][i] : "";
                    alignments.Add(cell.StartsWith(":", StringComparison.Ordinal) && cell.EndsWith(":", StringComparison.Ordinal)
                        ? TableColumnAlignment.Center
                        : cell.EndsWith(":", StringComparison.Ordinal)
                            ? TableColumnAlignment.Right
                            : TableColumnAlignment.Left);
                }
            }

            IEnumerable<List<string>> body = hasHeader ? parsed.Skip(2) : parsed;
            return new MarkdownTable
            {
                HasHeader = hasHeader,
                Headers = hasHeader ? PadRow(parsed[0], columns) : Array.Empty<string>(),
                Rows = body.Select(row => (IReadOnlyList<string>)PadRow(row, columns)).ToList(),
                Alignments = alignments
            };
        }

        private static List<string> SplitTableRow(string line)
        {
            string text = (line ?? "").Trim();
            if (text.StartsWith("|", StringComparison.Ordinal)) text = text.Substring(1);
            if (text.EndsWith("|", StringComparison.Ordinal)) text = text.Substring(0, text.Length - 1);
            return text.Split('|').Select(cell => cell.Trim()).ToList();
        }

        private static List<string> PadRow(List<string> row, int columns)
        {
            var result = new List<string>(row);
            while (result.Count < columns) result.Add("");
            return result;
        }
    }

    /// <summary>将 MarkdownNT 文本导出为完整 HTML 文档。</summary>
    public static class HtmlExporter
    {
        /// <summary>导出 HTML 文档。</summary>
        /// <param name="markdownText">待导出的 MarkdownNT 文本。</param>
        /// <returns>完整 HTML 文档字符串。</returns>
        public static string ExportToHtml(string markdownText)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\">");
            html.AppendLine("<title>MarkdownNT Export</title><style>");
            html.AppendLine("body{font-family:-apple-system,'Segoe UI',sans-serif;margin:40px auto;max-width:800px;padding:0 20px;line-height:1.6;color:#333}");
            html.AppendLine("pre{background:#2d2d30;color:#dcdcdc;padding:16px;border-radius:6px;overflow-x:auto}");
            html.AppendLine("code{font-family:Consolas,'Courier New',monospace}");
            html.AppendLine("blockquote{border-left:4px solid #ccc;margin:0;padding-left:16px;color:#666}");
            html.AppendLine("table{border-collapse:collapse;margin:16px 0}th,td{border:1px solid #ddd;padding:8px 12px}");
            html.AppendLine("hr{border:none;border-top:1px solid #ddd;margin:20px 0}.markdownnt-special{color:purple;font-weight:bold}");
            html.AppendLine("</style></head><body>");

            bool inList = false;
            foreach (MarkdownBlock block in MarkdownParser.Parse(markdownText ?? ""))
            {
                if (block.Type != BlockType.ListItem && inList)
                {
                    html.AppendLine("</ul>");
                    inList = false;
                }
                switch (block.Type)
                {
                    case BlockType.Heading:
                        html.AppendLine($"<h{block.Level}>{Inline(block.Text)}</h{block.Level}>");
                        break;
                    case BlockType.Quote:
                        html.AppendLine($"<blockquote>{Inline(block.Text)}</blockquote>");
                        break;
                    case BlockType.ListItem:
                        if (!inList) { html.AppendLine("<ul>"); inList = true; }
                        html.AppendLine($"<li>{Inline(block.Text)}</li>");
                        break;
                    case BlockType.Divider:
                        html.AppendLine("<hr>");
                        break;
                    case BlockType.Special:
                        html.AppendLine($"<p class=\"markdownnt-special\">{Inline(block.Text)}</p>");
                        break;
                    case BlockType.CodeBlock:
                        html.AppendLine($"<pre><code class=\"language-{WebUtility.HtmlEncode(block.Language)}\">{WebUtility.HtmlEncode(string.Join("\n", block.Lines))}</code></pre>");
                        break;
                    case BlockType.Table:
                        html.AppendLine(TableHtml(block.Lines));
                        break;
                    case BlockType.Paragraph:
                        html.AppendLine($"<p>{Inline(block.Text)}</p>");
                        break;
                }
            }
            if (inList) html.AppendLine("</ul>");
            html.AppendLine("</body></html>");
            return html.ToString();
        }

        private static string TableHtml(IReadOnlyList<string> lines)
        {
            MarkdownTable table = MarkdownParser.ParseTable(lines);
            var html = new StringBuilder("<table>");
            if (table.HasHeader)
            {
                html.Append("<thead><tr>");
                for (int i = 0; i < table.Headers.Count; i++)
                    html.Append($"<th style=\"text-align:{Alignment(i, table.Alignments)}\">{Inline(table.Headers[i])}</th>");
                html.Append("</tr></thead>");
            }
            if (table.Rows.Count > 0)
            {
                html.Append("<tbody>");
                foreach (IReadOnlyList<string> row in table.Rows)
                {
                    html.Append("<tr>");
                    for (int i = 0; i < row.Count; i++)
                        html.Append($"<td style=\"text-align:{Alignment(i, table.Alignments)}\">{Inline(row[i])}</td>");
                    html.Append("</tr>");
                }
                html.Append("</tbody>");
            }
            return html.Append("</table>").ToString();
        }

        private static string Alignment(int index, IReadOnlyList<TableColumnAlignment> alignments)
        {
            if (index >= alignments.Count) return "left";
            return alignments[index] == TableColumnAlignment.Center ? "center" :
                   alignments[index] == TableColumnAlignment.Right ? "right" : "left";
        }

        private static string Inline(string text)
        {
            string result = WebUtility.HtmlEncode(text ?? "");
            result = Regex.Replace(result, "`([^`]+)`", "<code>$1</code>");
            result = Regex.Replace(result, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            return Regex.Replace(result, @"(?<!\*)\*([^*]+)\*(?!\*)", "<em>$1</em>");
        }
    }

    /// <summary>将 MarkdownNT 文本导出为 PDF 文档。</summary>
    public static class PdfExporter
    {
        private const string RegularFace = "MarkdownNT-CJK-Regular";
        private const string BoldFace = "MarkdownNT-CJK-Bold";
        private const string CodeFace = "MarkdownNT-CJK-Code";
        private static readonly object FontResolverLock = new object();
        private static bool _fontResolverConfigured;

        /// <summary>导出 PDF 字节数组。</summary>
        /// <param name="markdownText">待导出的 MarkdownNT 文本。</param>
        /// <returns>PDF 文件内容。</returns>
        public static byte[] ExportToPdf(string markdownText)
        {
            using (var stream = new MemoryStream())
            {
                ExportToPdf(markdownText, stream);
                return stream.ToArray();
            }
        }

        /// <summary>将 MarkdownNT 文本写入 PDF 流。</summary>
        /// <param name="markdownText">待导出的 MarkdownNT 文本。</param>
        /// <param name="output">可写输出流。</param>
        public static void ExportToPdf(string markdownText, Stream output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (!output.CanWrite) throw new ArgumentException("输出流必须可写。", nameof(output));

            EnsureFontResolver();
            var document = new PdfDocument();
            PdfPage page = document.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            XGraphics graphics = XGraphics.FromPdfPage(page);
            const double left = 42, top = 42, bottom = 42;
            double width = page.Width - left * 2;
            double y = top;
            var body = new XFont(RegularFace, 10, XFontStyle.Regular);
            var bold = new XFont(RegularFace, 10, XFontStyle.Bold);
            var code = new XFont(CodeFace, 9, XFontStyle.Regular);

            void NewPage()
            {
                graphics.Dispose();
                page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                graphics = XGraphics.FromPdfPage(page);
                y = top;
            }

            void Ensure(double height)
            {
                if (y + height > page.Height - bottom) NewPage();
            }

            void DrawText(string text, XFont font, XBrush brush, double lineHeight, double indent = 0)
            {
                string[] lines = (text ?? "").Replace("\r\n", "\n").Split('\n');
                foreach (string source in lines)
                {
                    string current = "";
                    foreach (string word in (source.Length == 0 ? " " : source).Split(new[] { ' ' }, StringSplitOptions.None))
                    {
                        string candidate = current.Length == 0 ? word : current + " " + word;
                        if (current.Length > 0 && graphics.MeasureString(candidate, font).Width > width - indent)
                        {
                            Ensure(lineHeight);
                            graphics.DrawString(current, font, brush, new XRect(left + indent, y, width - indent, lineHeight), XStringFormats.TopLeft);
                            y += lineHeight;
                            current = word;
                        }
                        else current = candidate;
                    }
                    Ensure(lineHeight);
                    graphics.DrawString(current, font, brush, new XRect(left + indent, y, width - indent, lineHeight), XStringFormats.TopLeft);
                    y += lineHeight;
                }
            }

            foreach (MarkdownBlock block in MarkdownParser.Parse(markdownText ?? ""))
            {
                switch (block.Type)
                {
                    case BlockType.Heading:
                        Ensure(28);
                        DrawText(block.Text, new XFont(RegularFace, Math.Max(10, 22 - block.Level * 2), XFontStyle.Bold), XBrushes.Black, 24);
                        y += 5;
                        break;
                    case BlockType.Quote:
                        Ensure(18);
                        graphics.DrawLine(XPens.Gray, left, y, left, y + 16);
                        DrawText(block.Text, body, XBrushes.Gray, 16, 10);
                        break;
                    case BlockType.ListItem:
                        DrawText("• " + block.Text, body, XBrushes.Black, 16, 8);
                        break;
                    case BlockType.Divider:
                        Ensure(12);
                        graphics.DrawLine(XPens.LightGray, left, y, left + width, y);
                        y += 12;
                        break;
                    case BlockType.Special:
                        DrawText(block.Text, bold, XBrushes.Purple, 16);
                        break;
                    case BlockType.CodeBlock:
                        DrawText("[" + (block.Language.Length == 0 ? "text" : block.Language) + "]", bold, XBrushes.DarkSlateGray, 15);
                        foreach (string line in block.Lines) DrawText(line, code, XBrushes.Black, 13, 8);
                        y += 5;
                        break;
                    case BlockType.Table:
                        DrawTable(block.Lines, graphics, ref y, Ensure, width, left, body, bold);
                        break;
                    case BlockType.Paragraph:
                        DrawText(block.Text, body, XBrushes.Black, 16);
                        y += 4;
                        break;
                }
            }
            graphics.Dispose();
            document.Save(output, false);
        }

        private static void EnsureFontResolver()
        {
            if (_fontResolverConfigured) return;

            lock (FontResolverLock)
            {
                if (_fontResolverConfigured) return;
                if (GlobalFontSettings.FontResolver == null)
                    GlobalFontSettings.FontResolver = new ChineseFontResolver();
                _fontResolverConfigured = true;
            }
        }

        private sealed class ChineseFontResolver : IFontResolver
        {
            private readonly string _regularFontPath;
            private readonly string _boldFontPath;
            private readonly string _codeFontPath;

            public ChineseFontResolver()
            {
                _regularFontPath = FindFont(
                    Environment.GetEnvironmentVariable("MARKDOWNNT_PDF_FONT"),
                    "msyh.ttf", "msyh.ttc", "simsun.ttf", "simsun.ttc",
                    "simhei.ttf", "Deng.ttf", "NotoSansCJK-Regular.ttc",
                    "NotoSansSC-Regular.otf");
                // Keep all weights on the same known CJK face. This avoids
                // selecting a different font collection face for bold text.
                _boldFontPath = _regularFontPath;
                _codeFontPath = TryFindFont(
                    Environment.GetEnvironmentVariable("MARKDOWNNT_PDF_CODE_FONT"),
                    "SarasaMonoSC-Regular.ttf", "NotoSansMonoCJK-Regular.otf",
                    "NotoSansCJK-Regular.ttc") ?? _regularFontPath;
            }

            public string DefaultFontName => RegularFace;

            public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
            {
                if (familyName == CodeFace) return new FontResolverInfo(CodeFace);
                return new FontResolverInfo(RegularFace);
            }

            public byte[] GetFont(string faceName)
            {
                string path = faceName == CodeFace
                    ? _codeFontPath
                    : faceName == BoldFace ? _boldFontPath : _regularFontPath;
                return File.ReadAllBytes(path);
            }

            private static string FindFont(params string[]? values)
            {
                string? result = TryFindFont(values);
                if (result != null) return result;

                throw new InvalidOperationException(
                    "未找到可嵌入 PDF 的中文字体。请设置 MARKDOWNNT_PDF_FONT 环境变量为 .ttf/.ttc/.otf 字体文件路径。");
            }

            private static string? TryFindFont(params string[]? values)
            {
                var candidates = new List<string>();
                foreach (string? value in values)
                {
                    string fontValue = value ?? "";
                    if (!string.IsNullOrWhiteSpace(fontValue))
                    {
                        if (Path.IsPathRooted(fontValue))
                            candidates.Add(fontValue);
                        else
                            candidates.AddRange(FontDirectories().Select(directory => Path.Combine(directory, fontValue)));
                    }
                }

                foreach (string candidate in candidates)
                    if (File.Exists(candidate)) return candidate;

                return null;
            }

            private static IEnumerable<string> FontDirectories()
            {
                string windowsFonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
                if (!string.IsNullOrWhiteSpace(windowsFonts)) yield return windowsFonts;

                yield return "/usr/share/fonts/opentype/noto";
                yield return "/usr/share/fonts/truetype/noto";
                yield return "/usr/share/fonts/truetype/wqy";
                yield return "/Library/Fonts";
                yield return "/System/Library/Fonts";
            }
        }

        private static void DrawTable(IReadOnlyList<string> lines, XGraphics graphics, ref double y,
            Action<double> ensure, double width, double left, XFont body, XFont bold)
        {
            MarkdownTable table = MarkdownParser.ParseTable(lines);
            int columns = table.HasHeader ? table.Headers.Count :
                table.Rows.Count == 0 ? 0 : table.Rows.Max(row => row.Count);
            if (columns == 0) return;
            double columnWidth = width / columns;
            bool header = table.HasHeader;
            foreach (IReadOnlyList<string> row in Rows(table))
            {
                ensure(22);
                for (int i = 0; i < columns; i++)
                {
                    string cell = i < row.Count ? row[i] : "";
                    double x = left + i * columnWidth;
                    graphics.DrawRectangle(XPens.LightGray, x, y, columnWidth, 20);
                    graphics.DrawString(cell, header ? bold : body, XBrushes.Black,
                        new XRect(x + 3, y + 3, columnWidth - 6, 14), XStringFormats.TopLeft);
                }
                y += 20;
                header = false;
            }
            y += 6;
        }

        private static IEnumerable<IReadOnlyList<string>> Rows(MarkdownTable table)
        {
            if (table.HasHeader) yield return table.Headers;
            foreach (IReadOnlyList<string> row in table.Rows) yield return row;
        }
    }
}
