using MarkdownNT.Plugin.Abstractions;
using System;
using System.Collections.Generic;

namespace MarkdownNT.Plugin.Test
{
    /// <summary>覆盖 MdNTCode 全部规则字段的测试插件。</summary>
    public sealed class MdNTCodeTestPlugin : IMdNTCode
    {
        public string Id => "MdNT.Plugin.Test";
        public string Name => "MdNTCode 全功能测试插件";
        public Version Version => new Version(1, 0, 0);

        public IEnumerable<MdNTCodeRule> GetRules()
        {
            yield return new MdNTCodeRule
            {
                Name = "关键词测试",
                Keyword = "TODO",
                Priority = 100,
                Style = new MdNTCodeStyle
                {
                    Foreground = "#FF8800",
                    Background = "#FFF0CC",
                    DarkForeground = "#FFD580",
                    DarkBackground = "#4A3A20",
                    FontFamily = "Microsoft YaHei",
                    Bold = true,
                    Italic = false,
                    Underline = true,
                    Strikethrough = false,
                    Effect = "warning"
                }
            };

            yield return new MdNTCodeRule
            {
                Name = "正则表达式测试",
                Expression = @"\[\[(.*?)\]\]",
                Priority = 90,
                Style = new MdNTCodeStyle
                {
                    Foreground = "#0066CC",
                    Background = "#EAF4FF",
                    DarkForeground = "#72B7FF",
                    DarkBackground = "#18324A",
                    FontFamily = "Segoe UI",
                    Bold = false,
                    Italic = true,
                    Underline = false,
                    Strikethrough = true,
                    Effect = "reference"
                }
            };

            yield return new MdNTCodeRule
            {
                Name = "字体和粗体测试",
                Keyword = "FONT_TEST",
                Priority = 80,
                Style = new MdNTCodeStyle
                {
                    Foreground = "#7A1FA2",
                    Background = "#F6E8FF",
                    DarkForeground = "#D9A7FF",
                    DarkBackground = "#3A2448",
                    FontFamily = "Consolas",
                    Bold = true,
                    Italic = true,
                    Effect = "code"
                }
            };

            yield return new MdNTCodeRule
            {
                Name = "装饰效果测试",
                Keyword = "EFFECT_TEST",
                Priority = 70,
                Style = new MdNTCodeStyle
                {
                    Foreground = "#FFFFFF",
                    Background = "#008000",
                    DarkForeground = "#B8FFB8",
                    DarkBackground = "#245C24",
                    Bold = false,
                    Italic = true,
                    Underline = true,
                    Strikethrough = true,
                    Effect = "highlight"
                }
            };
        }
    }
}
