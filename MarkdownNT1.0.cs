using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownNT
{
    public enum BlockType
    {
        Heading,
        Quote,
        ListItem,
        Divider,
        Special,
        CodeBlock,
        Table,
        Paragraph
    }

    public class MarkdownBlock
    {
        public BlockType Type { get; init; }
        // Heading: 1-6 level
        public int Level { get; init; }
        // Single-line block text (heading/quote/list/special/paragraph)
        public string Text { get; init; } = "";
        // CodeBlock: language hint
        public string Language { get; init; } = "";
        // CodeBlock / Table: raw lines
        public IReadOnlyList<string> Lines { get; init; } = Array.Empty<string>();
    }

    public enum TableColumnAlignment
    {
        Left,
        Center,
        Right
    }

    public class MarkdownTable
    {
        public bool HasHeader { get; init; }
        public IReadOnlyList<string> Headers { get; init; } = Array.Empty<string>();
        public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = Array.Empty<IReadOnlyList<string>>();
        public IReadOnlyList<TableColumnAlignment> Alignments { get; init; } = Array.Empty<TableColumnAlignment>();
    }

    /// <summary>
    /// MarkdownNT core parser (no UI). Supports a small dialect:
    /// - headings (#..)
    /// - blockquote (> )
    /// - unordered lists (- or *)
    /// - divider (---)
    /// - special blocks starting with @meta/@audio/@speaker
    /// - code blocks delimited by lines starting with "&gt;&gt;&gt;" (optionally followed by language)
    /// - GitHub-style pipe tables
    /// </summary>
    public static class MarkdownParser
    {
        private static readonly Regex DividerRegex = new(@"^\s*-{3,}\s*$");
        private static readonly Regex HeadingRegex = new(@"^(#{1,6})\s+(.*)");
        private static readonly Regex ListItemRegex = new(@"^\s*[-*][ \t]+(.*)$");

        public static bool TryParseCodeBlockDelimiter(string line, out bool isEnd, out string language)
        {
            isEnd = false;
            language = "";
            string trimmed = line.Trim();
            if (!trimmed.StartsWith(">>>")) return false;
            string after = trimmed.Substring(3).Trim();
            if (after.Length == 0)
            {
                isEnd = true;
                return true;
            }
            language = after;
            return true;
        }

        public static List<MarkdownBlock> Parse(string markdownText)
        {
            var blocks = new List<MarkdownBlock>();
            var lines = markdownText?.Replace("\r\n", "\n").Split('\n') ?? Array.Empty<string>();

            bool inCodeBlock = false;
            string codeLanguage = "";
            var codeLines = new List<string>();
            var tableLines = new List<string>();

            void FlushTable()
            {
                if (tableLines.Count > 0)
                {
                    blocks.Add(new MarkdownBlock { Type = BlockType.Table, Lines = tableLines.ToArray() });
                    tableLines = new List<string>();
                }
            }

            foreach (var rawLine in lines)
            {
                string trimmed = rawLine.TrimEnd();

                if (TryParseCodeBlockDelimiter(trimmed, out bool isCodeEnd, out string parsedLanguage))
                {
                    if (inCodeBlock)
                    {
                        if (isCodeEnd)
                        {
                            blocks.Add(new MarkdownBlock
                            {
                                Type = BlockType.CodeBlock,
                                Language = codeLanguage,
                                Lines = codeLines.ToArray()
                            });
                            inCodeBlock = false;
                            codeLines = new List<string>();
                            continue;
                        }
                        // inside code block, lines starting with >>> plus content are treated as code content
                        codeLines.Add(trimmed);
                        continue;
                    }
                    if (!isCodeEnd)
                    {
                        inCodeBlock = true;
                        codeLanguage = string.IsNullOrEmpty(parsedLanguage) ? "text" : parsedLanguage;
                        codeLines = new List<string>();
                    }
                    // isolated >>> outside code block is ignored
                    continue;
                }

                if (inCodeBlock)
                {
                    codeLines.Add(trimmed);
                    continue;
                }

                if (trimmed.StartsWith("|"))
                {
                    tableLines.Add(trimmed);
                    continue;
                }
                FlushTable();

                if (DividerRegex.IsMatch(trimmed))
                {
                    blocks.Add(new MarkdownBlock { Type = BlockType.Divider });
                    continue;
                }

                var headingMatch = HeadingRegex.Match(trimmed);
                if (headingMatch.Success)
                {
                    blocks.Add(new MarkdownBlock
                    {
                        Type = BlockType.Heading,
                        Level = headingMatch.Groups[1].Value.Length,
                        Text = headingMatch.Groups[2].Value
                    });
                    continue;
                }

                if (trimmed.StartsWith(">"))
                {
                    blocks.Add(new MarkdownBlock
                    {
                        Type = BlockType.Quote,
                        Text = trimmed.Substring(1).Trim()
                    });
                    continue;
                }

                var listMatch = ListItemRegex.Match(trimmed);
                if (listMatch.Success)
                {
                    blocks.Add(new MarkdownBlock
                    {
                        Type = BlockType.ListItem,
                        Text = listMatch.Groups[1].Value
                    });
                    continue;
                }

                if (trimmed.StartsWith("@meta") || trimmed.StartsWith("@audio") || trimmed.StartsWith("@speaker"))
                {
                    blocks.Add(new MarkdownBlock { Type = BlockType.Special, Text = trimmed });
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    blocks.Add(new MarkdownBlock { Type = BlockType.Paragraph, Text = trimmed });
                }
            }

            FlushTable();
            if (inCodeBlock)
            {
                blocks.Add(new MarkdownBlock
                {
                    Type = BlockType.CodeBlock,
                    Language = codeLanguage,
                    Lines = codeLines.ToArray()
                });
            }

            return blocks;
        }

        private static readonly Regex SeparatorCellRegex = new(@"^:?-+:?$");

        /// <summary>
        /// Parse raw table lines into MarkdownTable (headers, rows, alignments).
        /// Short rows are padded with empty cells.
        /// </summary>
        public static MarkdownTable ParseTable(IReadOnlyList<string> tableLines)
        {
            var parsed = tableLines.Select(SplitTableRow).ToList();
            int colCount = parsed.Count > 0 ? parsed.Max(r => r.Count) : 0;

            bool hasHeader = parsed.Count >= 2 && parsed[1].Count > 0 && parsed[1].All(c => SeparatorCellRegex.IsMatch(c));

            var alignments = new List<TableColumnAlignment>();
            if (hasHeader)
            {
                for (int i = 0; i < colCount; i++)
                {
                    string cell = i < parsed[1].Count ? parsed[1][i] : "";
                    TableColumnAlignment alignment = TableColumnAlignment.Left;
                    if (cell.StartsWith(":" ) && cell.EndsWith(":")) alignment = TableColumnAlignment.Center;
                    else if (cell.EndsWith(":")) alignment = TableColumnAlignment.Right;
                    alignments.Add(alignment);
                }
            }

            IEnumerable<List<string>> bodyRows = hasHeader ? parsed.Skip(2) : parsed;
            var rows = bodyRows.Select(r => PadRow(r, colCount)).ToList();

            return new MarkdownTable
            {
                HasHeader = hasHeader,
                Headers = hasHeader ? PadRow(parsed[0], colCount) : Array.Empty<string>(),
                Rows = rows,
                Alignments = alignments
            };
        }

        private static List<string> SplitTableRow(string line)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("|")) trimmed = trimmed.Substring(1);
            if (trimmed.EndsWith("|")) trimmed = trimmed.Substring(0, trimmed.Length - 1);
            return trimmed.Split('|').Select(c => c.Trim()).ToList();
        }

        private static List<string> PadRow(List<string> row, int colCount)
        {
            var padded = new List<string>(row);
            while (padded.Count < colCount) padded.Add("");
            return padded;
        }
    }

    public static class HtmlExporter
    {
        public static string ExportToHtml(string markdownText)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"utf-8\">);");
            html.AppendLine("<title>MarkdownNT Export</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: -apple-system, 'Segoe UI', sans-serif; margin: 40px auto; max-width: 800px; padding: 0 20px; line-height: 1.6; color: #333; }");
            html.AppendLine("h1 { font-size: 2em; border-bottom: 1px solid #ddd; padding-bottom: 0.3em; }");
            html.AppendLine("h2 { font-size: 1.5em; border-bottom: 1px solid #eee; padding-bottom: 0.2em; }");
            html.AppendLine("pre { background-color: #2d2d30; color: #dcdcdc; padding: 16px; border-radius: 6px; overflow-x: auto; }");
            html.AppendLine(":not(pre) > code { background-color: #f0f0f0; padding: 2px 4px; border-radius: 4px; }");
            html.AppendLine("blockquote { border-left: 4px solid #ccc; margin: 0; padding-left: 16px; color: #666; }");
            html.AppendLine("table { border-collapse: collapse; margin: 16px 0; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px 12px; }");
            html.AppendLine("hr { border: none; border-top: 1px solid #ddd; margin: 20px 0; }");
            html.AppendLine(".markdownnt-special { color: purple; font-weight: bold; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            var blocks = MarkdownParser.Parse(markdownText);
            bool inList = false;

            foreach (var block in blocks)
            {
                if (block.Type != BlockType.ListItem && inList)
                {
                    html.AppendLine("</ul>");
                    inList = false;
                }

                switch (block.Type)
                {
                    case BlockType.Heading:
                        html.AppendLine($"<h{block.Level}>{RenderInlineHtml(block.Text)}</h{block.Level}>");
                        break;
                    case BlockType.Quote:
                        html.AppendLine($"<blockquote>{RenderInlineHtml(block.Text)}</blockquote>");
                        break;
                    case BlockType.ListItem:
                        if (!inList)
                        {
                            html.AppendLine("<ul>");
                            inList = true;
                        }
                        html.AppendLine($"<li>{RenderInlineHtml(block.Text)}</li>");
                        break;
                    case BlockType.Divider:
                        html.AppendLine("<hr>");
                        break;
                    case BlockType.Special:
                        html.AppendLine($"<p class=\"markdownnt-special\">{RenderInlineHtml(block.Text)}</p>");
                        break;
                    case BlockType.CodeBlock:
                        html.AppendLine($"<pre><code class=\"language-{WebUtility.HtmlEncode(block.Language)}\">{WebUtility.HtmlEncode(string.Join("\n", block.Lines))}</code></pre>");
                        break;
                    case BlockType.Table:
                        html.Append(BuildTableHtml(block.Lines));
                        break;
                    case BlockType.Paragraph:
                        html.AppendLine($"<p>{RenderInlineHtml(block.Text)}</p>");
                        break;
                }
            }

            if (inList)
            {
                html.AppendLine("</ul>");
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return html.ToString();
        }

        private static string BuildTableHtml(IReadOnlyList<string> tableLines)
        {
            var table = MarkdownParser.ParseTable(tableLines);
            var sb = new StringBuilder();
            sb.AppendLine("<table>");
            if (table.HasHeader)
            {
                sb.Append("<thead><tr>");
                for (int i = 0; i < table.Headers.Count; i++)
                {
                    string align = AlignmentStyle(i < table.Alignments.Count ? table.Alignments[i] : TableColumnAlignment.Left);
                    sb.Append($"<th style=\"text-align: {align}\">{RenderInlineHtml(table.Headers[i])}</th>");
                }
                sb.AppendLine("</tr></thead>");
            }
            if (table.Rows.Count > 0)
            {
                sb.AppendLine("<tbody>");
                foreach (var row in table.Rows)
                {
                    sb.Append("<tr>");
                    for (int i = 0; i < row.Count; i++)
                    {
                        string align = AlignmentStyle(i < table.Alignments.Count ? table.Alignments[i] : TableColumnAlignment.Left);
                        sb.Append($"<td style=\"text-align: {align}\">{RenderInlineHtml(row[i])}</td>");
                    }
                    sb.AppendLine("</tr>");
                }
                sb.AppendLine("</tbody>");
            }
            sb.AppendLine("</table>");
            return sb.ToString();
        }

        private static string AlignmentStyle(TableColumnAlignment alignment) => alignment switch
        {
            TableColumnAlignment.Center => "center",
            TableColumnAlignment.Right => "right",
            _ => "left"
        };

        private static string RenderInlineHtml(string text)
        {
            string result = WebUtility.HtmlEncode(text ?? "");
            result = Regex.Replace(result, "`([^`]+)`", m => $"<code>{m.Groups[1].Value}</code>");
            result = Regex.Replace(result, @"\*\*(.+?)\*\*", m => $"<strong>{m.Groups[1].Value}</strong>");
            result = Regex.Replace(result, @"(?<!\*)\*([^*]+)\*(?!\*)", m => $"<em>{m.Groups[1].Value}</em>");
            return result;
        }
    }
}
