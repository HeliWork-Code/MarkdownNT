using MarkdownNT.Plugin.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MarkdownNT
{
    internal sealed class LoadedPlugin
    {
        public IMdNTCode Plugin { get; }
        public IReadOnlyList<MdNTCodeRule> Rules { get; }

        public LoadedPlugin(IMdNTCode plugin, IReadOnlyList<MdNTCodeRule> rules)
        {
            Plugin = plugin;
            Rules = rules;
        }
    }

    internal static class PluginLoader
    {
        public static IReadOnlyList<LoadedPlugin> Load(
            string directory, IReadOnlyCollection<string>? disabledPluginIds = null)
        {
            var loaded = new List<LoadedPlugin>();
            if (!Directory.Exists(directory))
            {
                Logger.Log($"插件目录不存在: {directory}");
                return loaded;
            }

            foreach (string path in Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(path);
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.IsAbstract || !typeof(IMdNTCode).IsAssignableFrom(type)) continue;
                        if (Activator.CreateInstance(type) is not IMdNTCode plugin) continue;

                        if (disabledPluginIds?.Contains(plugin.Id) == true)
                        {
                            Logger.Log($"插件已禁用: {plugin.Id}+{plugin.Version}");
                            continue;
                        }

                        var rules = plugin.GetRules()
                            .Where(rule => rule != null)
                            .OrderByDescending(rule => rule.Priority)
                            .ToList();
                        loaded.Add(new LoadedPlugin(plugin, rules));
                        Logger.Log($"加载插件: {plugin.Name} v{plugin.Version}");
                        Logger.Log($"Plugin Done:{plugin.Id}+{plugin.Version}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"插件加载失败: {path}", ex);
                }
            }

            Logger.Log($"插件加载完成: {loaded.Count} 个");
            return loaded;
        }
    }
}
