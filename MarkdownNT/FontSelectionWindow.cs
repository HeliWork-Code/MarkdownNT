using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarkdownNT
{
    public sealed class FontSelectionWindow : Window
    {
        private sealed class FontChoice
        {
            public string Name { get; }
            public FontFamily Family { get; }

            public FontChoice(string name)
            {
                Name = name;
                Family = new FontFamily(name);
            }
        }

        private readonly ComboBox _fontBox;
        private readonly ComboBox _codeFontBox;

        public string? SelectedFont { get; private set; }
        public string? SelectedCodeFont { get; private set; }

        public FontSelectionWindow(string currentFont, string currentCodeFont)
        {
            Title = "选择编辑器字体";
            Width = 360;
            Height = 240;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var fontChoices = CreateFontChoices(
                "Microsoft YaHei", "SimSun", "SimHei", "KaiTi",
                "等线", "Arial", "Segoe UI", "Consolas",
                "Cascadia Mono", "Courier New", "Sarasa Mono SC",
                "Noto Sans Mono CJK");

            _fontBox = new ComboBox
            {
                ItemsSource = fontChoices,
                SelectedItem = fontChoices.FirstOrDefault(font =>
                    string.Equals(font.Name, currentFont, StringComparison.OrdinalIgnoreCase)),
                ItemTemplate = new FuncDataTemplate<FontChoice>((font, _) =>
                    new TextBlock { Text = font.Name, FontFamily = font.Family }),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _codeFontBox = new ComboBox
            {
                ItemsSource = fontChoices,
                SelectedItem = fontChoices.FirstOrDefault(font =>
                    string.Equals(font.Name, currentCodeFont, StringComparison.OrdinalIgnoreCase)),
                ItemTemplate = new FuncDataTemplate<FontChoice>((font, _) =>
                    new TextBlock { Text = font.Name, FontFamily = font.Family }),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var okButton = new Button { Content = "确定", MinWidth = 72 };
            okButton.Click += (sender, args) =>
            {
                SelectedFont = (_fontBox.SelectedItem as FontChoice)?.Name;
                SelectedCodeFont = (_codeFontBox.SelectedItem as FontChoice)?.Name;
                Close(true);
            };

            var cancelButton = new Button { Content = "取消", MinWidth = 72 };
            cancelButton.Click += (sender, args) => Close(false);

            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = "选择全局字体：" },
                    _fontBox,
                    new TextBlock { Text = "选择代码字体：" },
                    _codeFontBox,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { okButton, cancelButton }
                    }
                }
            };
        }

        public Task<bool?> ShowAsync(Window owner)
        {
            return ShowDialog<bool?>(owner);
        }

        private static List<FontChoice> CreateFontChoices(params string[] names)
        {
            var choices = new List<FontChoice>();
            foreach (string name in names)
            {
                if (!string.IsNullOrWhiteSpace(name))
                    choices.Add(new FontChoice(name));
            }

            return choices;
        }
    }
}
