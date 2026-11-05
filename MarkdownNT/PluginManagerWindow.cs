using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarkdownNT
{
    internal sealed class PluginManagerWindow : Window
    {
        private readonly List<PluginToggle> _toggles = new List<PluginToggle>();

        public IReadOnlyCollection<string> DisabledPluginIds { get; private set; } =
            Array.Empty<string>();

        public PluginManagerWindow(
            IReadOnlyList<LoadedPlugin> plugins,
            IReadOnlyCollection<string> disabledPluginIds)
        {
            Title = "插件管理";
            Width = 560;
            Height = 420;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var pluginPanel = new StackPanel { Spacing = 8 };
            foreach (LoadedPlugin plugin in plugins)
            {
                var toggle = new PluginToggle(plugin, !disabledPluginIds.Contains(plugin.Plugin.Id));
                _toggles.Add(toggle);
                pluginPanel.Children.Add(toggle.CheckBox);
            }

            if (plugins.Count == 0)
            {
                pluginPanel.Children.Add(new TextBlock
                {
                    Text = "当前没有发现插件。",
                    Margin = new Thickness(4)
                });
            }

            var scrollViewer = new ScrollViewer { Content = pluginPanel };
            var applyButton = new Button
            {
                Content = "应用并关闭",
                MinWidth = 100,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            applyButton.Click += (sender, args) =>
            {
                DisabledPluginIds = _toggles
                    .Where(toggle => !toggle.CheckBox.IsChecked.GetValueOrDefault())
                    .Select(toggle => toggle.Plugin.Plugin.Id)
                    .ToArray();
                Close(true);
            };

            Content = new DockPanel
            {
                Margin = new Thickness(16),
                Children =
                {
                    new TextBlock
                    {
                        Text = "启用或禁用已发现的 MarkdownNT 插件：",
                        Margin = new Thickness(0, 0, 0, 12)
                    },
                    scrollViewer,
                    new Border
                    {
                        Child = applyButton,
                        Margin = new Thickness(0, 12, 0, 0)
                    }
                }
            };
            DockPanel.SetDock((Control)((DockPanel)Content).Children[0], Dock.Top);
            DockPanel.SetDock((Control)((DockPanel)Content).Children[2], Dock.Bottom);
        }

        public Task<bool?> ShowAsync(Window owner)
        {
            return ShowDialog<bool?>(owner);
        }

        private sealed class PluginToggle
        {
            public LoadedPlugin Plugin { get; }
            public CheckBox CheckBox { get; }

            public PluginToggle(LoadedPlugin plugin, bool enabled)
            {
                Plugin = plugin;
                CheckBox = new CheckBox
                {
                    IsChecked = enabled,
                    Content = $"{plugin.Plugin.Name}  |  {plugin.Plugin.Id}  |  v{plugin.Plugin.Version}  |  {plugin.Rules.Count} 条规则"
                };
            }
        }
    }
}
