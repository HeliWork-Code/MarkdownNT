using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MarkdownNT.Lab;

public partial class MainWindow : Window
{
    private const string DefaultPluginFolderName = "Plugins";
    private readonly string _settingsPath;
    private readonly string _enabledSettingsPath;
    private readonly HashSet<string> _enabledPluginIds = new();
    private DateTime _lastPluginClickTime = DateTime.MinValue;
    private string? _lastPluginClickedId = null;

    public MainWindow()
    {
        InitializeComponent();

        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MarkdownNT.Lab",
            "plugin-folder.txt");

        _enabledSettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MarkdownNT.Lab",
            "enabled-plugins.txt");

        LoadPluginsButton.Click += LoadPluginsButton_Click;
        RunRulesButton.Click += RunRulesButton_Click;
        GenerateTemplateButton.Click += GenerateTemplateButton_Click;
        ChangePluginFolderButton.Click += ChangePluginFolderButton_Click;

        PluginPathBox.Text = LoadSavedPluginPath();
        InputBox.Text = "# MarkdownNT.Lab\n\nTODO: 这里是一个测试任务。\n\n[[重要内容]]\n\n- 列表项一\n- 列表项二";

        LoadEnabledPluginIds();
    }

    private string LoadSavedPluginPath()
    {
        string defaultPath = Path.Combine(AppContext.BaseDirectory, DefaultPluginFolderName);

        if (!File.Exists(_settingsPath))
            return defaultPath;

        string savedPath = File.ReadAllText(_settingsPath).Trim();
        if (string.IsNullOrWhiteSpace(savedPath))
            return defaultPath;

        return savedPath;
    }

    private void SavePluginPath(string path)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(_settingsPath, path);
        }
        catch
        {
            // Ignore write failures in this small lab app.
        }
    }

    private void RefreshPluginList()
    {
        var directory = PluginPathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Path.Combine(AppContext.BaseDirectory, DefaultPluginFolderName);
            PluginPathBox.Text = directory;
        }

        SavePluginPath(directory);

        var plugins = PluginLoader.Load(directory).ToList();
        if (plugins.Count == 0)
        {
            PluginList.ItemsSource = new[] { "未发现插件。当前仅内置 DemoPlugin 可用于测试。" };
            ResultList.ItemsSource = new[] { "(没有规则可运行)" };
            FormattedPreview.Text = "";
        return;
        }

        // If no saved enabled IDs, default to all enabled (don't persist until user changes)
        bool defaultAllEnabled = _enabledPluginIds.Count == 0;

        var itemControls = new List<Control>();
        foreach (var p in plugins)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            var cb = new CheckBox
            {
                Content = $"{p.Id} v{p.Version} - {p.Name}",
                Tag = p,
                IsChecked = defaultAllEnabled || _enabledPluginIds.Contains(p.Id),
                Margin = new Avalonia.Thickness(0, 2),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var moreBtn = new Button
            {
                Content = "⋯",
                Width = 28,
                Height = 24,
                Margin = new Avalonia.Thickness(8, 0, 0, 0),
                IsVisible = true
            };

            moreBtn.Click += (_, __) =>
            {
                try { OpenPluginDetailWindow(p); } catch { }
            };

            // clicking checkbox toggles enable and selects
            cb.Click += (s, e) => PluginItem_Click(s, e, p);

            panel.Children.Add(cb);
            panel.Children.Add(moreBtn);

            itemControls.Add(panel);
        }

        PluginList.ItemsSource = itemControls;

        // Also populate ResultList with a quick summary
        ResultList.ItemsSource = plugins.Select(p => $"{p.Id} ({p.Name}) - {p.GetRules().Count()} 规则").ToArray();
    }

    private void PluginItem_Click(object? sender, RoutedEventArgs e, ILabPlugin plugin)
    {
        // Toggle enable state (sender should be the CheckBox)
        try
        {
            PluginCheckbox_Changed(sender, e);
        }
        catch { }

        // Detect double click by timing
        var now = DateTime.UtcNow;
        if (_lastPluginClickedId == plugin.Id && (now - _lastPluginClickTime).TotalMilliseconds < 450)
        {
            try { ShowPluginDetailsDetailed(plugin); } catch { }
            // reset to avoid triple-clicks
            _lastPluginClickedId = null;
            _lastPluginClickTime = DateTime.MinValue;
            return;
        }

        _lastPluginClickedId = plugin.Id;
        _lastPluginClickTime = now;

        // single click -> show brief details
        try { ShowPluginDetails(plugin); } catch { }
    }

    private void OpenPluginDetailWindow(ILabPlugin plugin)
    {
        var win = new PluginDetailWindow(plugin);
        win.Show(this);
    }

    private void ShowPluginDetailsDetailed(ILabPlugin plugin)
    {
        var rules = plugin.GetRules().ToList();
        var lines = new List<string>
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

        // show detailed info in a separate window instead
        OpenPluginDetailWindow(plugin);
    }
    private void LoadPluginsButton_Click(object? sender, RoutedEventArgs e)
    {
        RefreshPluginList();
    }

    private void PluginCheckbox_Changed(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cb) return;
        if (cb.Tag is not ILabPlugin plugin) return;

        if (cb.IsChecked == true)
            _enabledPluginIds.Add(plugin.Id);
        else
            _enabledPluginIds.Remove(plugin.Id);

        SaveEnabledPluginIds();
    }

    private void ShowPluginDetails(ILabPlugin plugin)
    {
        var rules = plugin.GetRules().ToList();
        var lines = new List<string>
        {
            $"Id: {plugin.Id}",
            $"Name: {plugin.Name}",
            $"Version: {plugin.Version}",
            $"规则数: {rules.Count}"
        };

        foreach (var r in rules)
        {
            lines.Add($"- {r.Name} (关键词: {r.Keyword ?? "-"}, 正则: {(string.IsNullOrWhiteSpace(r.Expression) ? "-" : r.Expression)})");
        }

        ResultList.ItemsSource = lines.ToArray();
    }

    private void RunRulesButton_Click(object? sender, RoutedEventArgs e)
    {
        var sample = InputBox.Text;
        var path = PluginPathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, DefaultPluginFolderName);
            PluginPathBox.Text = path;
        }

        SavePluginPath(path);

        var allPlugins = PluginLoader.Load(path).ToList();
        List<ILabPlugin> pluginsToRun;
        if (_enabledPluginIds.Count == 0)
        {
            // no explicit choices -> run all available (or demo if none)
            pluginsToRun = allPlugins.Count == 0 ? new List<ILabPlugin> { new DemoPlugin() } : allPlugins;
        }
        else
        {
            pluginsToRun = allPlugins.Where(p => _enabledPluginIds.Contains(p.Id)).ToList();
            if (pluginsToRun.Count == 0 && allPlugins.Count == 0)
                pluginsToRun = new List<ILabPlugin> { new DemoPlugin() };
            else if (pluginsToRun.Count == 0)
                pluginsToRun = allPlugins; // fallback to all when none of the enabled ids match
        }

        var hits = RuleTester.Evaluate(sample, pluginsToRun);
        ResultList.ItemsSource = hits.Count == 0
            ? new[] { "没有命中任何规则。" }
            : hits.ToArray();

        var matches = RuleTester.FindMatches(sample, pluginsToRun);
        RenderFormattedPreview(sample, matches);
    }

    private void RenderFormattedPreview(string? source, IReadOnlyList<RuleMatch> matches)
    {
        source ??= string.Empty;
        FormattedPreview.Text = null;
        FormattedPreview.Inlines?.Clear();

        if (matches.Count == 0)
        {
            FormattedPreview.Text = string.IsNullOrEmpty(source) ? "暂无可预览内容。" : source;
            ResultList.ItemsSource = new[] { "未检测到插件规则命中。" };
            return;
        }

        var position = 0;
        foreach (var match in SelectNonOverlappingMatches(matches, source.Length))
        {
            if (match.Start > position)
                FormattedPreview.Inlines?.Add(new Run(source[position..match.Start]));

            var run = new Run(match.Text);
            ApplyStyle(run, match.Style);
            FormattedPreview.Inlines?.Add(run);
            position = match.Start + match.Length;
        }

        if (position < source.Length)
            FormattedPreview.Inlines?.Add(new Run(source[position..]));

        ResultList.ItemsSource = matches.Select(match =>
            $"{match.PluginName} | {match.RuleName} | {match.Kind} | \"{match.Text}\" | 位置 {match.Start}，长度 {match.Length}")
            .ToArray();
    }

    private static IEnumerable<RuleMatch> SelectNonOverlappingMatches(
        IReadOnlyList<RuleMatch> matches,
        int sourceLength)
    {
        var position = 0;
        foreach (var match in matches)
        {
            if (match.Start < position || match.Start < 0 || match.Length <= 0 ||
                match.Start + match.Length > sourceLength)
                continue;

            yield return match;
            position = match.Start + match.Length;
        }
    }

    private static void ApplyStyle(Run run, LabStyle style)
    {
        if (!string.IsNullOrWhiteSpace(style.Foreground) &&
            Color.TryParse(style.Foreground, out var foreground))
            run.Foreground = new SolidColorBrush(foreground);
        if (!string.IsNullOrWhiteSpace(style.Background) &&
            Color.TryParse(style.Background, out var background))
            run.Background = new SolidColorBrush(background);
        if (style.Bold)
            run.FontWeight = FontWeight.Bold;
        if (style.Italic)
            run.FontStyle = FontStyle.Italic;
    }

    private void GenerateTemplateButton_Click(object? sender, RoutedEventArgs e)
    {
        var template = """
using MarkdownNT.Lab;

public sealed class MyPlugin : ILabPlugin
{
    public string Id => \"demo.myplugin\";
    public string Name => \"My Plugin\";
    public Version Version => new(1, 0, 0);

    public IEnumerable<LabRule> GetRules()
    {
        yield return new LabRule
        {
            Name = \"自定义关键词\",
            Keyword = \"FIXME\",
            Priority = 10,
            Style = new LabStyle
            {
                Foreground = \"#FF6600\",
                Background = \"#FFF0E0\",
                Bold = true
            }
        };
    }
}
""";

        var path = Path.Combine(AppContext.BaseDirectory, "MyPluginTemplate.cs");
        File.WriteAllText(path, template);
        ResultList.ItemsSource = new[] { $"模板已生成：{path}" };
    }

    private async void ChangePluginFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var options = new FolderPickerOpenOptions
        {
            Title = "选择插件目录",
            AllowMultiple = false
        };

        var folders = await StorageProvider.OpenFolderPickerAsync(options);
        var selectedFolder = folders.FirstOrDefault();
        if (selectedFolder is null)
            return;

        var folderPath = selectedFolder.Path.LocalPath;
        PluginPathBox.Text = folderPath;
        SavePluginPath(folderPath);
        RefreshPluginList();
    }

    private void LoadEnabledPluginIds()
    {
        try
        {
            if (!File.Exists(_enabledSettingsPath))
                return;

            var ids = File.ReadAllLines(_enabledSettingsPath)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l));

            foreach (var id in ids)
                _enabledPluginIds.Add(id);
        }
        catch
        {
            // ignore
        }
    }

    private void SaveEnabledPluginIds()
    {
        try
        {
            var directory = Path.GetDirectoryName(_enabledSettingsPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllLines(_enabledSettingsPath, _enabledPluginIds);
        }
        catch
        {
            // ignore
        }
    }
}
