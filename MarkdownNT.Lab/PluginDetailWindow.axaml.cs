using Avalonia.Controls;
using System.Linq;

namespace MarkdownNT.Lab;

public partial class PluginDetailWindow : Window
{
    public PluginDetailWindow(ILabPlugin plugin)
    {
        InitializeComponent();

        var rules = plugin.GetRules().ToList();
        var lines = new System.Collections.Generic.List<string>
        {
            $"Id: {plugin.Id}",
            $"Name: {plugin.Name}",
            $"Version: {plugin.Version}",
            $"规则数: {rules.Count}",
            "-- 规则详情 --"
        };

        foreach (var r in rules)
        {
            lines.Add($"规则: {r.Name}");
            lines.Add($"  关键词: {r.Keyword ?? "-"}");
            lines.Add($"  正则: {(string.IsNullOrWhiteSpace(r.Expression) ? "-" : r.Expression)}");
            lines.Add($"  优先级: {r.Priority}");
            lines.Add($"  样式: fg={r.Style.Foreground ?? "-"}, bg={r.Style.Background ?? "-"}, bold={r.Style.Bold}, italic={r.Style.Italic}, underline={r.Style.Underline}");
        }

        DetailText.Text = string.Join('\n', lines);
    }
}
