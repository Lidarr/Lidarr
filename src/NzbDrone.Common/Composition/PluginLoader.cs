using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NLog;
using NzbDrone.Common.Instrumentation;

namespace NzbDrone.Common.Composition
{
    public static class PluginLoader
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(PluginLoader));

        public static (List<Assembly>, List<WeakReference>) LoadPlugins(IEnumerable<string> pluginPaths)
        {
            var assemblies = new List<Assembly>();
            var pluginRefs = new List<WeakReference>();

            foreach (var pluginPath in pluginPaths)
            {
                (var plugin, var pluginRef) = LoadPlugin(pluginPath);
                pluginRefs.Add(pluginRef);
                assemblies.Add(plugin);
            }

            return (assemblies, pluginRefs);
        }

        public static bool UnloadPlugins(List<WeakReference> pluginRefs)
        {
            RequestPluginUnload(pluginRefs);
            return AwaitPluginUnload(pluginRefs);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (Assembly, WeakReference) LoadPlugin(string path)
        {
            var context = new PluginLoadContext(path);
            var weakRef = new WeakReference(context, trackResurrection: true);

            // load from stream to avoid locking on windows
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var assembly = context.LoadFromStream(fs);

            return (assembly, weakRef);
        }

        private static void RequestPluginUnload(List<WeakReference> pluginRefs)
        {
            foreach (var pluginRef in pluginRefs)
            {
                if (pluginRef?.Target != null)
                {
                    ((PluginLoadContext)pluginRef.Target).Unload();
                }
            }
        }

        private static bool AwaitPluginUnload(List<WeakReference> pluginRefs)
        {
            var i = 0;
            foreach (var pluginRef in pluginRefs.Where(x => x != null))
            {
                while (pluginRef.IsAlive)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    if (i++ >= 10)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
