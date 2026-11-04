using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Controls.Documents;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using MarkdownNT.Plugin.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MarkdownNT
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _autoSaveTimer;
        private DispatcherTimer? _previewDebounceTimer;
        private string? _currentFilePath;
        private bool _firstUserInput = true;
        // 程序载入文本（打开/新建文件）时置位，避免误记为用户输入
        private bool _suppressInputLog;
        private string _fontFamilyName = "Microsoft YaHei";
        private string _codeFontFamilyName = "Consolas";
        private IReadOnlyList<LoadedPlugin> _loadedPlugins = Array.Empty<LoadedPlugin>();
        private readonly HashSet<string> _disabledPluginIds = new HashSet<string>(StringComparer.Ordinal);
        private string PluginSettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MarkdownNT", "disabled-plugins.txt");

        public MainWindow()
        {
            InitializeComponent();
            LoadPluginSettings();
            _loadedPlugins = PluginLoader.Load(Path.Combine(AppContext.BaseDirectory, "Plugins"), _disabledPluginIds);
            FontFamily = new FontFamily(_fontFamilyName);
            ApplyEditorTheme();
            if (InputBox.Document == null)
                InputBox.Document = new TextDocument();

            var coreVersion = typeof(MarkdownParser).Assembly.GetName().Version;
            string versionText = coreVersion?.ToString(3) ?? "未知";
            CoreVersionText.Text = MarkdownNTIdentity.IS_OFFICIAL
                ? $"内核 v{versionText}"
                : $"内核 v{versionText}（已修改）";

            Dispatcher.UIThread.UnhandledException += (s, e) =>
                Logger.Error("UI 线程未处理异常", e.Exception);

            // 预览防抖：停止输入 300ms 后再刷新，避免每次按键全量重建控件
            _previewDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _previewDebounceTimer.Tick += (s, e) =>
            {
                _previewDebounceTimer.Stop();
                UpdatePreview();
                UpdateWordCount();
            };

            InputBox.Document.TextChanged += (s, e) =>
            {
                if (_firstUserInput && !_suppressInputLog)
                {
                    _firstUserInput = false;
                    Logger.Log("User First Input");
                }
                _previewDebounceTimer.Stop();
                _previewDebounceTimer.Start();
            };

            InputBox.GotFocus += (s, e) => Logger.Log("编辑器获得键盘焦点");
            InputBox.LostFocus += (s, e) => Logger.Log("编辑器失去键盘焦点");

            Closed += (s, e) => Logger.Log("窗口关闭，会话结束");

            _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _autoSaveTimer.Tick += (s, e) => AutoSave();
            _autoSaveTimer.Start();
        }

        private void ToggleTheme_Click(object? sender, RoutedEventArgs e)
        {
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = Application.Current.RequestedThemeVariant == ThemeVariant.Dark
                    ? ThemeVariant.Light
                    : ThemeVariant.Dark;
                ApplyEditorTheme();
                UpdatePreview();
            }
        }

        private void ApplyEditorTheme()
        {
            bool isDark = Application.Current?.RequestedThemeVariant == ThemeVariant.Dark;
            InputBox.Foreground = isDark ? Brushes.White : Brushes.Black;
            InputBox.Background = isDark
                ? new SolidColorBrush(Color.Parse("#1E1E1E"))
                : Brushes.White;
        }

        private async void ManagePlugins_Click(object? sender, RoutedEventArgs e)
        {
            var allPlugins = PluginLoader.Load(Path.Combine(AppContext.BaseDirectory, "Plugins"));
            var dialog = new PluginManagerWindow(allPlugins, _disabledPluginIds);
            bool? result = await dialog.ShowAsync(this);
            if (result != true) return;

            _disabledPluginIds.Clear();
            foreach (string pluginId in dialog.DisabledPluginIds)
                _disabledPluginIds.Add(pluginId);
            SavePluginSettings();
            _loadedPlugins = PluginLoader.Load(Path.Combine(AppContext.BaseDirectory, "Plugins"), _disabledPluginIds);
            UpdatePreview();
            StatusText.Text = $"插件设置已更新，已启用 {_loadedPlugins.Count} 个";
        }

        private void LoadPluginSettings()
        {
            if (!File.Exists(PluginSettingsPath)) return;
            foreach (string line in File.ReadAllLines(PluginSettingsPath))
            {
                string pluginId = line.Trim();
                if (!string.IsNullOrWhiteSpace(pluginId))
                    _disabledPluginIds.Add(pluginId);
            }
        }

        private void SavePluginSettings()
        {
            string? directory = Path.GetDirectoryName(PluginSettingsPath);
            if (directory == null) return;
            Directory.CreateDirectory(directory);
            File.WriteAllLines(PluginSettingsPath, _disabledPluginIds.OrderBy(id => id));
        }

        private async void SelectFont_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new FontSelectionWindow(_fontFamilyName, _codeFontFamilyName);
            bool? result = await dialog.ShowAsync(this);
            string? selectedFont = dialog.SelectedFont;
            string? selectedCodeFont = dialog.SelectedCodeFont;
            if (result != true || string.IsNullOrWhiteSpace(selectedFont)
                || string.IsNullOrWhiteSpace(selectedCodeFont)) return;

            _fontFamilyName = selectedFont;
            _codeFontFamilyName = selectedCodeFont;
            FontFamily = new FontFamily(_fontFamilyName);
            InputBox.FontFamily = FontFamily;
            StatusText.Text = $"字体已切换: {_fontFamilyName} / 代码: {_codeFontFamilyName}";
            UpdatePreview();
        }

        private void NewFile_Click(object? sender, RoutedEventArgs e)
        {
            _suppressInputLog = true;
            try { InputBox.Text = ""; }
            finally { _suppressInputLog = false; }
            _currentFilePath = null;
            StatusText.Text = "新建文件";
            Logger.Log("新建文件");
            UpdatePreview();
            UpdateWordCount();
        }

        private async void OpenFile_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "打开 MarkdownNT 文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("MarkdownNT") { Patterns = new[] { "*.mdnt" } },
                    new FilePickerFileType("Markdown") { Patterns = new[] { "*.md" } },
                    new FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count >= 1)
            {
                var file = files[0];
                _currentFilePath = file.Path.LocalPath;
                using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream);
                _suppressInputLog = true;
                try { InputBox.Text = await reader.ReadToEndAsync(); }
                finally { _suppressInputLog = false; }
                StatusText.Text = $"已打开: {Path.GetFileName(_currentFilePath)}";
                Logger.Log($"打开文件: {_currentFilePath} ({InputBox.Text.Length} 字符)");
                UpdatePreview();
                UpdateWordCount();
            }
        }

        private async void SaveFile_Click(object? sender, RoutedEventArgs e)
        {
            await SaveFileAsync();
        }

        private async System.Threading.Tasks.Task SaveFileAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            if (_currentFilePath == null)
            {
                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "保存 MarkdownNT 文件",
                    FileTypeChoices = new[] { new FilePickerFileType("MarkdownNT") { Patterns = new[] { "*.mdnt" } } },
                    SuggestedFileName = "笔记.mdnt",
                    DefaultExtension = "mdnt"
                });
                if (file == null) return;
                _currentFilePath = file.Path.LocalPath;
            }

            await using (var stream = File.Create(_currentFilePath))
            using (var writer = new StreamWriter(stream))
                await writer.WriteAsync(InputBox.Text ?? "");

            StatusText.Text = $"已保存: {Path.GetFileName(_currentFilePath)}";
            AutoSaveText.Text = "自动保存: 已保存";
        }

        private async void AutoSave()
        {
            string autoSaveDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MarkdownNT");
            Directory.CreateDirectory(autoSaveDir);
            string autoSavePath = Path.Combine(autoSaveDir, "autosave.mdnt");
            // 空文本也写入，保证自动保存与编辑器内容一致
            await using (var stream = File.Create(autoSavePath))
            using (var writer = new StreamWriter(stream))
                await writer.WriteAsync(InputBox.Text ?? "");
            AutoSaveText.Text = $"自动 {DateTime.Now:HH:mm:ss}";
        }

        private void UpdateWordCount()
        {
            string text = InputBox.Text ?? "";
            int charCount = text.Length;
            int wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            WordCountText.Text = $"{charCount}字 / {wordCount}词";
        }

        private async void ExportHtml_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "导出为 HTML",
                FileTypeChoices = new[] { new FilePickerFileType("HTML") { Patterns = new[] { "*.html", "*.htm" } } },
                SuggestedFileName = "笔记.html",
                DefaultExtension = "html"
            });
            if (file == null) return;

            string html = HtmlExporter.ExportToHtml(InputBox.Text ?? "");
            await using (var stream = File.Create(file.Path.LocalPath))
            using (var writer = new StreamWriter(stream))
                await writer.WriteAsync(html);

            StatusText.Text = $"已导出: {Path.GetFileName(file.Path.LocalPath)}";
        }

        private async void ExportPdf_Click(object? sender, RoutedEventArgs e)
        {
            var notice = new Window
            {
                Title = "PDF 导出暂时关闭",
                Width = 380,
                Height = 170,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Thickness(18),
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "目前PDF导出有问题，暂时关闭。",
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 14
                        },
                        new Button
                        {
                            Content = "确定",
                            Width = 80,
                            HorizontalAlignment = HorizontalAlignment.Right
                        }
                    }
                }
            };

            if (notice.Content is StackPanel panel && panel.Children[1] is Button button)
                button.Click += (s, args) => notice.Close();

            await notice.ShowDialog(this);
        }

        private void UpdatePreview()
        {
            PreviewPanel.Children.Clear();
            bool isDark = Application.Current?.RequestedThemeVariant == ThemeVariant.Dark;
            IBrush defaultForeground = isDark ? Brushes.White : Brushes.Black;

            foreach (var block in MarkdownParser.Parse(InputBox.Text ?? ""))
            {
                switch (block.Type)
                {
                    case BlockType.Heading:
                    {
                        double size = 24 - block.Level * 2;
                        var headerText = new TextBlock
                        {
                            Text = block.Text,
                            FontSize = size,
                            FontWeight = FontWeight.Bold,
                            Foreground = defaultForeground,
                            Margin = new Thickness(0, 4)
                        };
                        PreviewPanel.Children.Add(headerText);
                        break;
                    }

                    case BlockType.Quote:
                    {
                        var quoteBorder = new Border
                        {
                            BorderBrush = isDark ? Brushes.DarkGray : Brushes.Silver,
                            BorderThickness = new Thickness(3, 0, 0, 0),
                            Padding = new Thickness(8, 4),
                            Margin = new Thickness(0, 4),
                            Background = isDark ? new SolidColorBrush(Color.Parse("#2A2A2E")) : new SolidColorBrush(Color.Parse("#F5F5F5"))
                        };
                        var quoteTextBlock = new TextBlock
                        {
                            Text = block.Text,
                            Foreground = isDark ? Brushes.LightGray : Brushes.DimGray,
                            FontStyle = FontStyle.Italic,
                            TextWrapping = TextWrapping.Wrap
                        };
                        quoteBorder.Child = quoteTextBlock;
                        PreviewPanel.Children.Add(quoteBorder);
                        break;
                    }

                    case BlockType.ListItem:
                    {
                        var listItem = new TextBlock
                        {
                            Text = "• " + block.Text,
                            Foreground = defaultForeground,
                            Margin = new Thickness(16, 0, 0, 2)
                        };
                        PreviewPanel.Children.Add(listItem);
                        break;
                    }

                    case BlockType.Divider:
                    {
                        var divider = new Border
                        {
                            Height = 1,
                            Background = isDark ? Brushes.Gray : Brushes.LightGray,
                            Margin = new Thickness(0, 8)
                        };
                        PreviewPanel.Children.Add(divider);
                        break;
                    }

                    case BlockType.Special:
                    {
                        var specialText = new TextBlock
                        {
                            Text = block.Text,
                            Foreground = Brushes.Purple,
                            FontWeight = FontWeight.Bold,
                            Margin = new Thickness(0, 2)
                        };
                        PreviewPanel.Children.Add(specialText);
                        break;
                    }

                    case BlockType.CodeBlock:
                    {
                        PreviewPanel.Children.Add(MarkdownRenderer.RenderCodeBlock(
                            block.Language,
                            new List<string>(block.Lines),
                            FontFamily,
                            new FontFamily(_codeFontFamilyName)));
                        break;
                    }

                    case BlockType.Table:
                    {
                        RenderTable(block.Lines, defaultForeground);
                        break;
                    }

                    case BlockType.Paragraph:
                    {
                        var paragraph = new TextBlock
                        {
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = defaultForeground,
                            Margin = new Thickness(0, 2),
                            FontFamily = FontFamily,
                            FontSize = 14,
                            FontWeight = FontWeight.Normal,
                            FontStyle = FontStyle.Normal
                        };
                        RenderInline(paragraph, block.Text, isDark);
                        PreviewPanel.Children.Add(paragraph);
                        break;
                    }
                }
            }
        }

        private void RenderInline(TextBlock paragraph, string text, bool isDark)
        {
            if (RenderPluginInline(paragraph, text, isDark)) return;

            int i = 0;
            while (i < text.Length)
            {
                // 加粗 **...**
                if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '*')
                {
                    int end = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                    if (end != -1)
                    {
                        string inner = text.Substring(i + 2, end - i - 2);
                        var run = new Run(inner)
                        {
                            FontWeight = FontWeight.Bold,
                            FontFamily = paragraph.FontFamily,
                            FontSize = paragraph.FontSize
                        };
                        paragraph.Inlines?.Add(run);
                        i = end + 2;
                        continue;
                    }

                }
                // 行内代码 `...`
                if (text[i] == '`')
                {
                    int end = text.IndexOf('`', i + 1);
                    if (end != -1)
                    {
                        string inner = text.Substring(i + 1, end - i - 1);
                        var run = new Run(inner)
                        {
                            FontFamily = FontFamily,
                            Background = isDark ? new SolidColorBrush(Color.Parse("#3A3A3E")) : new SolidColorBrush(Color.Parse("#E8E8E8")),
                            Foreground = isDark ? Brushes.LightGreen : Brushes.DarkRed
                        };
                        paragraph.Inlines?.Add(run);
                        i = end + 1;
                        continue;
                    }
                }
                // 斜体 *...*
                if (text[i] == '*' && (i == 0 || text[i - 1] != '*') && (i + 1 >= text.Length || text[i + 1] != '*'))
                {
                    int end = text.IndexOf('*', i + 1);
                    if (end != -1)
                    {
                        string inner = text.Substring(i + 1, end - i - 1);
                        var run = new Run(inner)
                        {
                            FontStyle = FontStyle.Italic,
                            FontFamily = paragraph.FontFamily,
                            FontSize = paragraph.FontSize
                        };
                        paragraph.Inlines?.Add(run);
                        i = end + 1;
                        continue;
                    }
                }

                // 普通文本
                int nextSpecial = text.Length;
                int nextBold = text.IndexOf("**", i, StringComparison.Ordinal);
                int nextCode = text.IndexOf('`', i);
                int nextItalic = text.IndexOf('*', i);
                if (nextBold != -1) nextSpecial = Math.Min(nextSpecial, nextBold);
                if (nextCode != -1) nextSpecial = Math.Min(nextSpecial, nextCode);
                if (nextItalic != -1) nextSpecial = Math.Min(nextSpecial, nextItalic);
                if (nextSpecial <= i) nextSpecial = i + 1;

                string normalText = text.Substring(i, nextSpecial - i);
                paragraph.Inlines?.Add(new Run(normalText));
                i = nextSpecial;
            }
        }

        private bool RenderPluginInline(TextBlock paragraph, string text, bool isDark)
        {
            var matches = new List<PluginMatch>();
            foreach (LoadedPlugin plugin in _loadedPlugins)
            {
                foreach (MdNTCodeRule rule in plugin.Rules)
                {
                    if (!string.IsNullOrWhiteSpace(rule.Keyword))
                    {
                        int start = 0;
                        while ((start = text.IndexOf(rule.Keyword, start, StringComparison.Ordinal)) >= 0)
                        {
                            matches.Add(new PluginMatch(start, rule.Keyword.Length, rule.Style, rule.Priority));
                            start += Math.Max(1, rule.Keyword.Length);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(rule.Expression))
                    {
                        try
                        {
                            foreach (Match match in Regex.Matches(
                                text, rule.Expression, RegexOptions.None, TimeSpan.FromMilliseconds(100)))
                            {
                                if (match.Success && match.Length > 0)
                                    matches.Add(new PluginMatch(match.Index, match.Length, rule.Style, rule.Priority));
                            }
                        }
                        catch (ArgumentException ex)
                        {
                            Logger.Error($"插件规则正则表达式无效: {rule.Name}", ex);
                        }
                        catch (RegexMatchTimeoutException ex)
                        {
                            Logger.Error($"插件规则正则表达式超时: {rule.Name}", ex);
                        }
                    }
                }
            }

            if (matches.Count == 0) return false;

            var selected = new List<PluginMatch>();
            foreach (PluginMatch match in matches
                .OrderByDescending(item => item.Priority)
                .ThenBy(item => item.Start))
            {
                if (selected.All(item =>
                    item.Start + item.Length <= match.Start ||
                    match.Start + match.Length <= item.Start))
                {
                    selected.Add(match);
                }
            }
            selected.Sort((left, right) => left.Start.CompareTo(right.Start));

            int position = 0;
            foreach (PluginMatch match in selected)
            {
                if (match.Start > position)
                    paragraph.Inlines?.Add(new Run(text.Substring(position, match.Start - position)));

                var run = new Run(text.Substring(match.Start, match.Length));
                ApplyPluginStyle(run, match.Style, isDark);
                paragraph.Inlines?.Add(run);
                position = match.Start + match.Length;
            }

            if (position < text.Length)
                paragraph.Inlines?.Add(new Run(text.Substring(position)));
            return true;
        }

        private static void ApplyPluginStyle(Run run, MdNTCodeStyle style, bool isDark)
        {
            string? foreground = isDark && !string.IsNullOrWhiteSpace(style.DarkForeground)
                ? style.DarkForeground : style.Foreground;
            string? background = isDark && !string.IsNullOrWhiteSpace(style.DarkBackground)
                ? style.DarkBackground : style.Background;
            if (!string.IsNullOrWhiteSpace(foreground))
                run.Foreground = ParseBrush(foreground, isDark ? Brushes.White : Brushes.Black);
            if (!string.IsNullOrWhiteSpace(background))
                run.Background = ParseBrush(background, Brushes.Transparent);
            if (!string.IsNullOrWhiteSpace(style.FontFamily))
                run.FontFamily = new FontFamily(style.FontFamily);
            if (style.Bold) run.FontWeight = FontWeight.Bold;
            if (style.Italic) run.FontStyle = FontStyle.Italic;
            if (style.Underline && style.Strikethrough)
                run.TextDecorations = CombineTextDecorations(
                    TextDecorations.Underline, TextDecorations.Strikethrough);
            else if (style.Underline)
                run.TextDecorations = TextDecorations.Underline;
            else if (style.Strikethrough)
                run.TextDecorations = TextDecorations.Strikethrough;
        }

        private static TextDecorationCollection CombineTextDecorations(
            params TextDecorationCollection[] collections)
        {
            var result = new TextDecorationCollection();
            foreach (TextDecorationCollection collection in collections)
            {
                foreach (TextDecoration decoration in collection)
                    result.Add(decoration);
            }

            return result;
        }

        private static IBrush ParseBrush(string value, IBrush fallback)
        {
            try { return new SolidColorBrush(Color.Parse(value)); }
            catch (FormatException) { return fallback; }
        }

        private sealed class PluginMatch
        {
            public int Start { get; }
            public int Length { get; }
            public MdNTCodeStyle Style { get; }
            public int Priority { get; }

            public PluginMatch(int start, int length, MdNTCodeStyle style, int priority)
            {
                Start = start;
                Length = length;
                Style = style;
                Priority = priority;
            }
        }

        private void RenderTable(IReadOnlyList<string> tableLines, IBrush defaultForeground)
        {
            if (tableLines.Count == 0) return;
            bool isDark = Application.Current?.RequestedThemeVariant == ThemeVariant.Dark;
            var table = MarkdownParser.ParseTable(tableLines);
            int colCount = Math.Max(table.Headers.Count, table.Rows.Count > 0 ? table.Rows[0].Count : 0);
            if (colCount == 0) return;

            IBrush cellLineBrush = isDark ? Brushes.Gray : Brushes.LightGray;

            var tableBorder = new Border
            {
                BorderBrush = isDark ? Brushes.DarkGray : Brushes.Silver,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 4)
            };

            var grid = new Grid();
            for (int c = 0; c < colCount; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            int rowCount = table.Rows.Count + (table.HasHeader ? 1 : 0);
            for (int r = 0; r < rowCount; r++)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            void AddCell(string text, int row, int col, bool isHeader)
            {
                TableColumnAlignment alignment = col < table.Alignments.Count
                    ? table.Alignments[col]
                    : TableColumnAlignment.Left;

                var textBlock = new TextBlock
                {
                    Text = text,
                    Foreground = defaultForeground,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = alignment switch
                    {
                        TableColumnAlignment.Center => Avalonia.Media.TextAlignment.Center,
                        TableColumnAlignment.Right => Avalonia.Media.TextAlignment.Right,
                        _ => Avalonia.Media.TextAlignment.Left
                    },
                    FontWeight = isHeader ? FontWeight.Bold : FontWeight.Normal,
                    Margin = new Thickness(8, 5)
                };
                // 每个单元格用右边线和下线拼出表格线，外框由 tableBorder 提供
                var border = new Border
                {
                    BorderBrush = cellLineBrush,
                    BorderThickness = new Thickness(0, 0, col < colCount - 1 ? 1 : 0, 1),
                    Child = textBlock,
                    Background = isHeader
                        ? (isDark ? new SolidColorBrush(Color.Parse("#33333A")) : new SolidColorBrush(Color.Parse("#F0F0F0")))
                        : Brushes.Transparent
                };
                Grid.SetRow(border, row);
                Grid.SetColumn(border, col);
                grid.Children.Add(border);
            }

            int rowIndex = 0;
            if (table.HasHeader)
            {
                for (int c = 0; c < table.Headers.Count; c++)
                    AddCell(table.Headers[c], rowIndex, c, isHeader: true);
                rowIndex++;
            }
            foreach (var row in table.Rows)
            {
                for (int c = 0; c < colCount; c++)
                    AddCell(c < row.Count ? row[c] : "", rowIndex, c, isHeader: false);
                rowIndex++;
            }

            tableBorder.Child = grid;
            PreviewPanel.Children.Add(tableBorder);
        }
    }
}
