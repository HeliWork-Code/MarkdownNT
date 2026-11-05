# MarkdownNT.Plugin.Abstractions v1.1.0

这是 MarkdownNT 的可选插件接口层，不依赖 `MarkdownNT.Core`、Avalonia 或 Official Editor。
当前接口版本为 `1.1.0`，包含暗色模式样式字段。

插件可以实现 `IMdNTCode`，通过 `MdNTCodeRule` 声明关键词、正则表达式、优先级和显示样式：

```csharp
using MarkdownNT.Plugin.Abstractions;
using System;
using System.Collections.Generic;

public sealed class TodoPlugin : IMdNTCode
{
    public string Id => "example.todo";
    public string Name => "Todo 语法";
    public Version Version => new Version(1, 0, 0);

    public IEnumerable<MdNTCodeRule> GetRules()
    {
        yield return new MdNTCodeRule
        {
            Name = "TODO",
            Keyword = "TODO",
            Priority = 10,
            Style = new MdNTCodeStyle
            {
                Foreground = "#FF8800",
                DarkForeground = "#FFD580",
                DarkBackground = "#4A3A20",
                Bold = true
            }
        };
    }
}
```

`DarkForeground` 和 `DarkBackground` 用于暗色主题；未设置时，宿主回退到
`Foreground` 和 `Background`。

该项目只定义数据和接口。插件如何加载、如何渲染以及是否启用，由具体宿主自行决定。
