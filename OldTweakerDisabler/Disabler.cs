using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;

namespace TweakerDisabler
{
    public static class Entrypoint
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("Deliter");

        public static IEnumerable<string> TargetDLLs => Enumerable.Empty<string>();

        public static void Patch(AssemblyDefinition asm)
        {
            Logger.LogWarning("No DLLs should be patched, but the patch method was called. Assembly: " + asm);
        }

        public static void Initialize()
        {
            string[] directories = Directory.GetDirectories(Paths.PluginPath);

            foreach (string dir in directories)
            {
                Logger.LogInfo($"Found directory {dir}");
                if (dir.Contains("devyndamonster-TakeAndHoldTweaker") && File.Exists(Path.Combine(dir, "TakeAndHoldTweaker.deli")))
                {
                    File.Move(Path.Combine(dir, "TakeAndHoldTweaker.deli"), Path.Combine(dir, "TakeAndHoldTweaker.deli.bak"));
                    Logger.LogInfo("Disabled old Take & Hold Tweaker install. Re-enable it via reinstalling or renaming the DLL. Will break compatibility with TNH Framework.");
                }
            }
        }
    }
}
