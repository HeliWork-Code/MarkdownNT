# MarkdownNT 插件目录

编辑器启动时会扫描程序目录下的 `Plugins` 文件夹及其子目录，加载实现
`IMdNTCode` 的插件程序集。

安装测试插件：

1. 构建 `MarkdownNT.Plugin.Test`。测试项目会自动将 DLL 复制到编辑器的插件目录。
2. 如果手动安装，将 `MarkdownNT.Plugin.Test.dll` 复制到本目录或其子目录。
3. 确保插件目录中存在匹配版本的 `MarkdownNT.Plugin.Abstractions.dll`。
4. 启动编辑器，并打开 `MdNTCode-Test.mdnt`。

加载成功后，普通段落中的 `TODO`、`[[...]]`、`FONT_TEST` 和 `EFFECT_TEST`
会按照测试插件提供的规则显示。
