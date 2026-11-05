# MarkdownNT 文档

欢迎来到 MarkdownNT 文档站点。这里整理了核心库、插件接口、插件开发指南、API 概览和发布说明，便于开发者快速理解项目结构和扩展方式。

## 项目概览

MarkdownNT 是一个轻量、可扩展的 Markdown 笔记编辑器与核心解析库项目，目标是保持核心无 UI、可选插件和可扩展结构。

当前源码仓库主要包含以下部分：

- MarkdownNT.Core：无 UI 的解析核心，负责 Markdown 解析、HTML 导出与 PDF 导出
- MarkdownNT.Plugin.Abstractions：插件接口层，供第三方插件实现
- MarkdownNT.Plugin.Test：测试插件示例，验证插件语法和样式表现
- 其他文档：插件指南、使用说明和版本记录

## 说明

官方编辑器（GUI）仅在 Release 中以可执行文件形式发布，不在本源码仓库中提供完整编辑器源码。

本仓库面向开发者和插件作者，重点发布：

- 核心库 API
- 插件开发接口
- 插件样式与规则说明
- 语法扩展示例
- 一些与发布相关的使用文档

## 快速入口

- [开始使用](./start.md)
- [插件开发指南](./plugins.md)
- [API 概览](./api.md)
- [发布说明](./release.md)

## 版本信息

当前文档站点对应的核心版本信息：

- MarkdownNT.Core: v1.1.4
- MarkdownNT.Plugin.Abstractions: v1.1.4
- 测试插件: v1.0.0

## 详细文档

- [开始使用](./start.md)
- [插件开发指南](./plugins.md)
- [API 概览](./api.md)
- [发布说明](./release.md)
- [核心库使用说明](./start.md)
- [如何编写插件](./plugins.md)
