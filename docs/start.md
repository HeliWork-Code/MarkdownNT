# 开始使用

## 1. 项目定位

MarkdownNT 是一个 JavaScript/Markdown 风格的笔记与渲染生态，核心设计目标是：

- 保持 MarkdownNT.Core 独立、无 UI、无插件依赖
- 提供官方插件接口，允许扩展自定义语法
- 允许使用者自行构建自己的宿主与编辑器

## 2. 开源部分是什么

本仓库中开源的内容主要包括：

- MarkdownNT.Core
- MarkdownNT.Plugin.Abstractions
- 插件测试示例
- 文档与开发说明

官方 GUI 编辑器并不在本仓库中提供完整源码，通常仅发布编译后的二进制产物。

## 3. 直接引用内核

如果你只是使用 MarkdownNT 的核心解析功能，可以直接引用 `MarkdownNT.Core.dll`。常见用途包括：

- 解析 Markdown 文本
- 导出 HTML
- 导出 PDF（目前仍需注意字体和排版兼容性）
- 在自己的应用中重用解析器

示例：

```csharp
using MarkdownNT;

string text = "# Hello\n\n这是 **MarkdownNT**。";
var blocks = MarkdownParser.Parse(text);
string html = HtmlExporter.ExportToHtml(text);
```

## 4. 插件开发

如果你希望扩展 MarkdownNT 的标记语法，可以实现 `IMdNTCode`，并返回一组 `MdNTCodeRule`：

- `Keyword`: 固定文本匹配
- `Expression`: 正则表达式匹配
- `Priority`: 优先级
- `Style`: 颜色、背景、粗体、斜体、删除线等样式

插件作者可以参考：

- [插件开发指南](./plugins.md)
- 仓库中的 `MarkdownNT.Plugin.Abstractions/如何编写插件.md`

## 5. 运行官方编辑器

官方编辑器只通过 Release 提供二进制文件。你可以在 GitHub Release 页面下载对应版本的发布包，并运行其中的可执行程序。

## 6. 版本约定

当前源码仓库中的核心版本为：

- MarkdownNT.Core v1.1.4
- MarkdownNT.Plugin.Abstractions v1.1.4

测试插件保持示例版本：

- MarkdownNT.Plugin.Test v1.0.0
