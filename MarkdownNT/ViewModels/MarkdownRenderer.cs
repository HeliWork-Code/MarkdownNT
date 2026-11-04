using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Collections.Generic;

namespace MarkdownNT
{
    public static class MarkdownRenderer
    {
        public static Border RenderCodeBlock(string language, List<string> codeLines,
            FontFamily? fontFamily = null, FontFamily? codeFontFamily = null)
        {
            // 根据主题选择颜色
            bool isDark = Application.Current?.RequestedThemeVariant == ThemeVariant.Dark;
            string backgroundColor = isDark ? "#2D2D30" : "#F0F0F0";
            string textColor = isDark ? "#DCDCDC" : "#1E1E1E";
            string languageColor = isDark ? "#FFFFFF" : "#000000";

            var codeBlockBorder = new Border
            {
                Background = new SolidColorBrush(Color.Parse(backgroundColor)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 4)
            };

            var codeBlockInner = new StackPanel();
            codeBlockBorder.Child = codeBlockInner;

            // 语言标签
            var langLabel = new TextBlock
            {
                Text = language,
                FontFamily = fontFamily ?? new FontFamily("Microsoft YaHei"),
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse(languageColor)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            codeBlockInner.Children.Add(langLabel);

            // 代码文本
            string code = string.Join(Environment.NewLine, codeLines);
            var codeText = new TextBlock
            {
                FontFamily = codeFontFamily ?? new FontFamily("Consolas, monospace"),
                TextWrapping = TextWrapping.NoWrap
            };

            var highlightedRuns = CodeHighlighter.HighlightCode(language, code, isDark);
            foreach (var run in highlightedRuns)
            {
                if (run.Foreground == null)
                {
                    run.Foreground = new SolidColorBrush(Color.Parse(textColor));
                }
                codeText.Inlines?.Add(run);
            }

            codeBlockInner.Children.Add(codeText);
            return codeBlockBorder;
        }
    }
}