# 发布说明

## 版本信息

当前源码仓库中发布的主要版本：

- MarkdownNT.Core: v1.1.4
- MarkdownNT.Plugin.Abstractions: v1.1.4
- MarkdownNT.Plugin.Test: v1.0.0

## 当前状态

本项目当前仍处于持续开发状态，重点改进内容包括：

- 标准 Markdown 代码块兼容
- 插件加载与管理
- 深色模式优化
- 字体与代码字体分离
- 插件样式能力增强

## 发行方式说明

官方编辑器（GUI）通常不在源码仓库中完整公开；该可执行程序会在 GitHub Release 中单独发布。

本仓库中主要提供：

- 核心解析实现
- 插件接口
- 插件示例
- 文档说明

## 注意事项

- PDF 导出仍需进一步修复与测试
- 插件机制存在本地程序集执行权限，使用前请确认来源可信
- 第三方扩展应遵循最小权限与安全审查原则

## 示例

以下是使用核心库的典型场景：

```csharp
using MarkdownNT;

string markdown = "# Title\n\n- Item A\n- Item B";
var blocks = MarkdownParser.Parse(markdown);
string html = HtmlExporter.ExportToHtml(markdown);
```

## 关联文档

- [开始使用](./start.md)
- [插件开发指南](./plugins.md)
- [API 概览](./api.md)
- 本仓库中的 `MarkdownNT.Plugin.Abstractions/如何编写插件.md`
