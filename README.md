<!-- 建议在仓库设置中开启 Discussions，方便社区交流 -->

# MarkdownNT (.mdnt)

**下一代 Markdown，为笔记、音频和 AI 而生。**

> 状态：🚧 早期开发中，欢迎 Watch / Star 关注进展！

MarkdownNT（简称 **MDNT**）是一种基于 Markdown 的增强型纯文本格式，旨在保持简单、可读、可迁移的同时，原生支持现代笔记场景所需的**音频关联**、**AI 元数据**和**结构化内容**。

**NT 代表：**
- **N**ote —— 为记录而生
- **N**ew Technology —— 融入现代 AI 技术
- **N**ext —— 下一代 Markdown

---

## ✨ 特性预览

- ✅ 完全兼容标准 Markdown 子集
- 🎙️ 音频块：关联录音文件、时间戳和说话人
- 🤖 AI 元数据：标记 AI 生成的摘要、待办等
- ⌨️ 快捷代码块：`>>>语言` 快速创建高亮代码块
- 📊 表格快捷创建：输入 `|` 自动引导
- 📝 极简语法：不打断心流，会打字就会排版

---

## 📄 示例

```markdown
@meta {
  "title": "产品评审会",
  "tags": ["会议", "产品"]
}

# 产品评审会

@audio {
  "file": "attachments/audio-20260824.wav",
  "start": 0,
  "end": 12.3,
  "speaker": "张三"
}

@speaker 张三
我们来看一下这个项目的进度。

>>>python
print("Hello, MDNT!")
>>>

| 任务 | 负责人 |
| --- | --- |
| 文档 | 张三 |
