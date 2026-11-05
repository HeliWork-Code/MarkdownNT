namespace MarkdownNT.Lab;

public static class LabConsoleDemo
{
    private const string Sample = """
# MarkdownNT.Lab 试验

TODO: 这里是一个测试任务。

[[重要内容]]

- 普通列表
- 第二项
""";

    public static void ShowPlugins()
    {
        var pluginDir = Path.Combine(AppContext.BaseDirectory, "Plugins");
        var plugins = PluginLoader.Load(pluginDir);

        if (plugins.Count == 0)
        {
            Console.WriteLine("未发现插件。当前仅内置 DemoPlugin 可用于测试。");
            plugins = new List<ILabPlugin> { new DemoPlugin() };
        }

        foreach (var plugin in plugins)
            Console.WriteLine($"{plugin.Id} v{plugin.Version} - {plugin.Name}");
    }

    public static void RunRuleTest()
    {
        var pluginDir = Path.Combine(AppContext.BaseDirectory, "Plugins");
        var plugins = PluginLoader.Load(pluginDir);
        if (plugins.Count == 0)
            plugins = new List<ILabPlugin> { new DemoPlugin() };

        var hits = RuleTester.Evaluate(Sample, plugins);
        if (hits.Count == 0)
        {
            Console.WriteLine("没有命中任何规则。");
            return;
        }

        Console.WriteLine("规则命中：");
        foreach (var hit in hits)
            Console.WriteLine($"- {hit}");
    }

    public static void GenerateTemplate()
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
        Console.WriteLine($"模板已生成：{path}");
    }
}
