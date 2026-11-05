# 插件开发指南

## 1. 插件是什么

MarkdownNT 插件是一个可选扩展机制，用于为文本增加自定义语法高亮、关键字处理、样式覆盖或其他视觉效果。

插件接口位于：

```text
MarkdownNT.Plugin.Abstractions
```

宿主不要求插件依赖编辑器 UI；插件只需实现通用接口并返回规则即可。

## 2. 核心接口

插件必须实现：

```csharp
public interface IMdNTCode
{
    string Id { get; }
    string Name { get; }
    Version Version { get; }
    IEnumerable<MdNTCodeRule> GetRules();
}
```

其中关键类型有：

- `MdNTCodeRule`
- `MdNTCodeStyle`

## 3. 规则示例

```csharp
yield return new MdNTCodeRule
{
    Name = "TODO 关键词",
    Keyword = "TODO",
    Priority = 100,
    Style = new MdNTCodeStyle
    {
        Foreground = "#FF8800",
        Background = "#FFF0CC",
        Bold = true,
        Italic = false
    }
};
```

## 4. 正则表达式支持

插件支持 `Expression` 字段，用来实现模式匹配：

```csharp
yield return new MdNTCodeRule
{
    Name = "双中括号内容",
    Expression = @"\[\[(.*?)\]\]",
    Priority = 50,
    Style = new MdNTCodeStyle
    {
        Foreground = "#0066CC",
        Background = "#EAF4FF",
        Italic = true,
        Strikethrough = true
    }
};
```

## 5. 样式字段

`MdNTCodeStyle` 支持以下字段：

- `Foreground` / `Background`
- `DarkForeground` / `DarkBackground`
- `FontFamily`
- `Bold`
- `Italic`
- `Underline`
- `Strikethrough`
- `Effect`

暗色模式字段会优先于亮色字段生效；未配置时自动回退。

## 6. 规则优先级

优先级越大越高：

```csharp
Priority = 100
```

当多个规则命中同一段文本时，宿主会优先保留高优先级规则。

## 7. 插件目录结构

通常插件项目可以按以下方式组织：

```text
MyPlugin/
├─ MyPlugin.csproj
├─ MyPlugin.cs
├─ README.md
└─ bin/
```

运行时，宿主通常会扫描程序目录下的 `Plugins` 文件夹加载插件。

## 8. 完整文档

完整说明请参考：

- 本仓库中的 `MarkdownNT.Plugin.Abstractions/如何编写插件.md`
- 本仓库中的 `MarkdownNT.Plugin.Abstractions/README.md`
- 本仓库中的 `MarkdownNT.Plugin.Test/MdNTCodeTestPlugin.cs`

## 9. 安全提示

插件本质上是本地程序集，宿主加载时具有执行代码能力。使用第三方插件前，请确认插件来源可信，并尽量使用最小权限原则。
