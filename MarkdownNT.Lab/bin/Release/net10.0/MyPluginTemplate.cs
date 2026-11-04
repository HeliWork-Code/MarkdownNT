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