# MarkdownNT

MarkdownNT 是一个轻量、可扩展的 Markdown 笔记项目，当前开源部分聚焦于：

- `MarkdownNT.Core`：无 UI 的 Markdown 解析与导出核心
- `MarkdownNT.Plugin.Abstractions`：插件接口层
- `MarkdownNT.Plugin.Test`：示例插件与测试案例
- 文档站点：Docsify 文档

说明：官方编辑器（GUI）不在本仓库中提供完整源码，通常仅在 GitHub Release 中发布编译后的二进制文件。

## 目标

- 保持核心无 UI、无依赖的设计
- 提供标准 Markdown 解析与导出能力
- 支持插件扩展自定义语法和样式
- 让开发者可以直接复用内核并构建自己的宿主应用

## 目录结构

```text
MarkdownNT/
├─ README.md
├─ index.html
├─ docs/
│  ├─ home.md
│  ├─ _sidebar.md
│  ├─ start.md
│  ├─ plugins.md
│  ├─ api.md
│  ├─ release.md
│  └─ ...
├─ MarkdownNT/
│  ├─ MarkdownNT.csproj
│  ├─ MainWindow.axaml
│  ├─ MainWindow.axaml.cs
│  ├─ Update.txt
│  └─ MarkdownNT.core/
│     ├─ MarkdownNT1.0.cs
│     ├─ MarkdownNT.Core.csproj
│     └─ 使用MarkdownNT.Core.dll.md
├─ MarkdownNT.Plugin.Abstractions/
│  ├─ MdNTCode.cs
│  ├─ MarkdownNT.Plugin.Abstractions.csproj
│  ├─ README.md
│  └─ 如何编写插件.md
├─ MarkdownNT.Plugin.Test/
│  ├─ MdNTCodeTestPlugin.cs
│  ├─ MdNTCode-Test.mdnt
│  └─ MarkdownNT.Plugin.Test.csproj
└─ MarkdownNT.slnx
```

## 开源内容

### MarkdownNT.Core

核心库负责：

- Markdown 解析
- 标题、段落、列表、引用、代码块和表格识别
- HTML 导出
- PDF 导出（仍在迭代中）

### MarkdownNT.Plugin.Abstractions

插件接口用于：

- 定义 `IMdNTCode`
- 定义 `MdNTCodeRule`
- 定义 `MdNTCodeStyle`
- 支持规则优先级、样式和暗色模式配置

### MarkdownNT.Plugin.Test

测试插件用于验证：

- 关键词高亮
- 正则表达式规则
- 亮色/暗色样式
- 字体和装饰效果
- 插件加载和管理

## 文档站点

文档已整理到 Docsify：

- [首页](./docs/home.md)
- [开始使用](./docs/start.md)
- [插件开发指南](./docs/plugins.md)
- [API 概览](./docs/api.md)
- [发布说明](./docs/release.md)

## 构建

```powershell
dotnet build MarkdownNT.slnx -c Release
```

## 版本说明

当前开源部分版本：

- MarkdownNT.Core: v1.1.4
- MarkdownNT.Plugin.Abstractions: v1.1.4
- MarkdownNT.Plugin.Test: v1.0.0

## 许可证

当前仓库未声明正式许可证。请在发布前根据你的使用场景补充合适的开源许可证（例如 MIT 或 Apache 2.0）。

## 贡献

欢迎提交 Issue、PR 和建议。对于核心解析、插件接口、样式扩展、文档完善等内容，均欢迎贡献。
=======
# 关于MarkdownNT

## 1.为什么MdNT。

由HeliWork Studio为您呈现的，是我们的全新项目——MarkdownNT。

我们秉承着三个理念：

**自由、创新、开源。**

### MarkdownNT的由来

我们对Markdown进行改进，并为此加上了一个特殊的后缀——"NT"。

NT代表

Note——一个十分好用、轻量级的笔记软件。

Next——作为笔记格式的下一代。

New Technology——我们将会加入新技术、从所谓有的。

## 2.MdNT可以为您做什么。

## 目前支持的。

Markdown格式、Meta信息、插件系统1.0（开发者请搭配独立的插件内核使用，官方编辑器已经内置了插件系统）、插件工作台1.0（需要下载独立软件）

## 未来支持的

内置音频、视频（需要搭配独立的音视频内核进行使用，官方编辑器将在未来内置音视频内核）、更轻量化的二进制存储格式文件（需要有内置的音视频才能使用，后缀会与普通文件有所区别）、插件系统2.0（目前未定）、插件工作台2.0（目前未定，需要搭配独立软件使用）


感谢您的支持，如果您认为这个仓库由看点的话，请给我们一个Star，谢谢。

HeliWork Studio 2026 Present
