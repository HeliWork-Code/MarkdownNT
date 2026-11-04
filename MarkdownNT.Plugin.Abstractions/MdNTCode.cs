using System;
using System.Collections.Generic;

namespace MarkdownNT.Plugin.Abstractions
{
    /// <summary>描述一个 MarkdownNT 自定义代码规则。</summary>
    public sealed class MdNTCodeRule
    {
        /// <summary>规则名称。</summary>
        public string Name { get; set; } = "";

        /// <summary>用于简单匹配的关键词。</summary>
        public string Keyword { get; set; } = "";

        /// <summary>用于复杂匹配的正则表达式。</summary>
        public string Expression { get; set; } = "";

        /// <summary>规则优先级，数值越大越优先。</summary>
        public int Priority { get; set; }

        /// <summary>匹配内容的显示样式。</summary>
        public MdNTCodeStyle Style { get; set; } = new MdNTCodeStyle();
    }

    /// <summary>描述 MdNTCode 匹配结果的显示样式。</summary>
    public sealed class MdNTCodeStyle
    {
        /// <summary>前景色，例如 #FF8800。</summary>
        public string? Foreground { get; set; }

        /// <summary>背景色，例如 #FFF0CC。</summary>
        public string? Background { get; set; }

        /// <summary>暗色模式下的前景色，例如 #FFD580。</summary>
        public string? DarkForeground { get; set; }

        /// <summary>暗色模式下的背景色，例如 #4A3A20。</summary>
        public string? DarkBackground { get; set; }

        /// <summary>字体族名称。</summary>
        public string? FontFamily { get; set; }

        /// <summary>是否使用粗体。</summary>
        public bool Bold { get; set; }

        /// <summary>是否使用斜体。</summary>
        public bool Italic { get; set; }

        /// <summary>是否使用下划线。</summary>
        public bool Underline { get; set; }

        /// <summary>是否使用删除线。</summary>
        public bool Strikethrough { get; set; }

        /// <summary>可选的效果名称，由宿主解释。</summary>
        public string? Effect { get; set; }
    }

    /// <summary>提供 MarkdownNT 自定义语法规则的插件。</summary>
    public interface IMdNTCode
    {
        /// <summary>插件唯一标识。</summary>
        string Id { get; }

        /// <summary>插件显示名称。</summary>
        string Name { get; }

        /// <summary>插件版本。</summary>
        Version Version { get; }

        /// <summary>返回插件提供的自定义语法规则。</summary>
        IEnumerable<MdNTCodeRule> GetRules();
    }
}
