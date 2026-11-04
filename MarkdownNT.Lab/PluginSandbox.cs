using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MarkdownNT.Lab;

public sealed class LabStyle
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

public sealed class LabRule
{
    public string Name { get; set; } = string.Empty;
    public string? Keyword { get; set; }
    public string? Expression { get; set; }
    public int Priority { get; set; }
    public LabStyle Style { get; set; } = new();
}

public sealed record RuleMatch(
    string PluginName,
    string RuleName,
    string Kind,
    int Start,
    int Length,
    string Text,
    int Priority,
    LabStyle Style);

public interface ILabPlugin
{
    string Id { get; }
    string Name { get; }
    Version Version { get; }
    IEnumerable<LabRule> GetRules();
}

public static class PluginLoader
{
    // Adapter that wraps an external IMdNTCode implementation into ILabPlugin
    private sealed class MdNTCodeAdapter : ILabPlugin
    {
        private readonly object _impl;
        private readonly Type _implType;

        public MdNTCodeAdapter(object impl)
        {
            _impl = impl;
            _implType = impl.GetType();
        }

        public string Id => (_implType.GetProperty("Id")?.GetValue(_impl) as string) ?? "unknown";
        public string Name => (_implType.GetProperty("Name")?.GetValue(_impl) as string) ?? "Unnamed";
        public Version Version => (_implType.GetProperty("Version")?.GetValue(_impl) as Version) ?? new Version(1, 0, 0);

        public IEnumerable<LabRule> GetRules()
        {
            var method = _implType.GetMethod("GetRules");
            if (method is null)
                yield break;

            var rulesObj = method.Invoke(_impl, null) as System.Collections.IEnumerable;
            if (rulesObj is null)
                yield break;

            foreach (var r in rulesObj)
            {
                if (r is null) continue;
                var rt = r.GetType();
                var rule = new LabRule
                {
                    Name = rt.GetProperty("Name")?.GetValue(r) as string ?? string.Empty,
                    Keyword = rt.GetProperty("Keyword")?.GetValue(r) as string,
                    Expression = rt.GetProperty("Expression")?.GetValue(r) as string,
                    Priority = (int?)(rt.GetProperty("Priority")?.GetValue(r)) ?? 0,
                    Style = new LabStyle()
                };

                var styleObj = rt.GetProperty("Style")?.GetValue(r);
                if (styleObj is not null)
                {
                    var st = styleObj.GetType();
                    rule.Style.Foreground = st.GetProperty("Foreground")?.GetValue(styleObj) as string;
                    rule.Style.Background = st.GetProperty("Background")?.GetValue(styleObj) as string;
                    rule.Style.DarkForeground = st.GetProperty("DarkForeground")?.GetValue(styleObj) as string;
                    rule.Style.DarkBackground = st.GetProperty("DarkBackground")?.GetValue(styleObj) as string;
                    rule.Style.FontFamily = st.GetProperty("FontFamily")?.GetValue(styleObj) as string;
                    rule.Style.Bold = (bool?)(st.GetProperty("Bold")?.GetValue(styleObj)) ?? false;
                    rule.Style.Italic = (bool?)(st.GetProperty("Italic")?.GetValue(styleObj)) ?? false;
                    rule.Style.Underline = (bool?)(st.GetProperty("Underline")?.GetValue(styleObj)) ?? false;
                    rule.Style.Strikethrough = (bool?)(st.GetProperty("Strikethrough")?.GetValue(styleObj)) ?? false;
                    rule.Style.Effect = st.GetProperty("Effect")?.GetValue(styleObj) as string;
                }

                yield return rule;
            }
        }
    }

    public static IReadOnlyList<ILabPlugin> Load(string pluginsDirectory)
    {
        if (!Directory.Exists(pluginsDirectory))
            return Array.Empty<ILabPlugin>();

        var plugins = new List<ILabPlugin>();
        foreach (var dllPath in Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                var asm = Assembly.LoadFrom(dllPath);
                foreach (var type in asm.GetTypes())
                {
                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    // 1) Direct ILabPlugin implementations (built for the lab)
                    if (typeof(ILabPlugin).IsAssignableFrom(type))
                    {
                        var plugin = (ILabPlugin?)Activator.CreateInstance(type);
                        if (plugin is not null)
                            plugins.Add(plugin);

                        continue;
                    }

                    // 2) External plugins implementing MarkdownNT.Plugin.Abstractions.IMdNTCode
                    var ifaces = type.GetInterfaces();
                    var mdIface = ifaces.FirstOrDefault(i => i.FullName == "MarkdownNT.Plugin.Abstractions.IMdNTCode");
                    if (mdIface is not null)
                    {
                        var impl = Activator.CreateInstance(type);
                        if (impl is not null)
                        {
                            plugins.Add(new MdNTCodeAdapter(impl));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Keep failures quiet but available in debug output.
                try { Console.Error.WriteLine($"Plugin load error '{dllPath}': {ex.Message}"); } catch { }
            }
        }

        return plugins;
    }
}

public static class RuleTester
{
    public static IReadOnlyList<RuleMatch> FindMatches(string? source, IEnumerable<ILabPlugin> plugins)
    {
        var results = new List<RuleMatch>();
        source ??= string.Empty;
        foreach (var plugin in plugins)
        {
            foreach (var rule in plugin.GetRules())
            {
                if (!string.IsNullOrWhiteSpace(rule.Keyword))
                {
                    int start = 0;
                    while ((start = source.IndexOf(rule.Keyword, start, StringComparison.Ordinal)) >= 0)
                    {
                        results.Add(new RuleMatch(
                            plugin.Name,
                            rule.Name,
                            "关键词",
                            start,
                            rule.Keyword.Length,
                            source.Substring(start, rule.Keyword.Length),
                            rule.Priority,
                            rule.Style));
                        start += Math.Max(1, rule.Keyword.Length);
                    }
                }

                if (!string.IsNullOrWhiteSpace(rule.Expression))
                {
                    foreach (Match match in Regex.Matches(source, rule.Expression, RegexOptions.None, TimeSpan.FromMilliseconds(100)))
                    {
                        if (match.Success && match.Length > 0)
                        {
                            results.Add(new RuleMatch(
                                plugin.Name,
                                rule.Name,
                                "正则",
                                match.Index,
                                match.Length,
                                match.Value,
                                rule.Priority,
                                rule.Style));
                        }
                    }
                }
            }
        }

        return results
            .OrderBy(match => match.Start)
            .ThenByDescending(match => match.Priority)
            .ThenByDescending(match => match.Length)
            .ToArray();
    }

    public static IReadOnlyList<string> Evaluate(string? source, IEnumerable<ILabPlugin> plugins)
    {
        return FindMatches(source, plugins)
            .Select(match => $"{match.PluginName} / {match.RuleName} / {match.Kind} / {match.Start}")
            .ToArray();
    }
}

public sealed class DemoPlugin : ILabPlugin
{
    public string Id => "lab.demo";
    public string Name => "MarkdownNT Lab Demo";
    public Version Version => new(1, 0, 0);

    public IEnumerable<LabRule> GetRules()
    {
        yield return new LabRule
        {
            Name = "TODO 关键词",
            Keyword = "TODO",
            Priority = 100,
            Style = new LabStyle
            {
                Foreground = "#FF8C00",
                Background = "#FFF3DB",
                Bold = true
            }
        };

        yield return new LabRule
        {
            Name = "双中括号",
            Expression = @"\[\[(.+?)\]\]",
            Priority = 60,
            Style = new LabStyle
            {
                Foreground = "#0078D4",
                Background = "#EAF3FF",
                Italic = true,
                Underline = true
            }
        };
    }
}
