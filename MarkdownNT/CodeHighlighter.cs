using Avalonia.Controls.Documents;
using Avalonia.Media;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MarkdownNT
{
    public static class CodeHighlighter
    {
        public static List<Run> HighlightCode(string language, string code, bool isDark)
        {
            var runs = new List<Run>();
            if (string.IsNullOrEmpty(code))
                return runs;

            HashSet<string> keywords = language.ToLower() switch
            {
                "python" => new HashSet<string>
                {
                    "def", "return", "if", "elif", "else", "for", "while", "import", "from", "as",
                    "class", "try", "except", "finally", "with", "pass", "break", "continue", "print"
                },
                "csharp" or "cs" => new HashSet<string>
                {
                    "using", "namespace", "class", "public", "private", "protected", "static", "void",
                    "int", "string", "bool", "var", "if", "else", "for", "foreach", "while", "return", "new"
                },
                _ => new HashSet<string>()
            };

            if (keywords.Count == 0)
            {
                runs.Add(new Run(code));
                return runs;
            }

            string keywordPattern = $@"\b({string.Join("|", keywords)})\b";
            string stringPattern = "\".*?\"|'.*?'";
            string commentPattern = language.ToLower() switch
            {
                "python" => "#.*$",
                "csharp" or "cs" => "//.*$",
                _ => "$^"
            };

            string combinedPattern = $"{keywordPattern}|{stringPattern}|{commentPattern}";
            Regex regex = new Regex(combinedPattern, RegexOptions.Multiline);

            IBrush keywordBrush = new SolidColorBrush(Color.Parse(isDark ? "#569CD6" : "#0000FF"));
            IBrush stringBrush = new SolidColorBrush(Color.Parse(isDark ? "#CE9178" : "#A31515"));
            IBrush commentBrush = new SolidColorBrush(Color.Parse(isDark ? "#6A9955" : "#008000"));

            int lastIndex = 0;
            foreach (Match match in regex.Matches(code))
            {
                if (match.Index > lastIndex)
                {
                    // 普通文本：不设置 Foreground，由渲染器根据主题设置
                    runs.Add(new Run(code.Substring(lastIndex, match.Index - lastIndex)));
                }

                string matchValue = match.Value;
                Run run = new Run(matchValue);
                if (Regex.IsMatch(matchValue, @"^(" + string.Join("|", keywords) + ")$"))
                {
                    run.Foreground = keywordBrush;
                    run.FontWeight = FontWeight.Bold;
                }
                else if (matchValue.StartsWith("\"") || matchValue.StartsWith("'"))
                {
                    run.Foreground = stringBrush;
                }
                else if (matchValue.StartsWith("#") || matchValue.StartsWith("//"))
                {
                    run.Foreground = commentBrush;
                }
                runs.Add(run);
                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < code.Length)
            {
                // 普通文本：不设置 Foreground
                runs.Add(new Run(code.Substring(lastIndex)));
            }

            return runs;
        }
    }
}
