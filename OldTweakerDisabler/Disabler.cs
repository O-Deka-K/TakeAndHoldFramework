using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
                if (dir.Contains("devyndamonster-TakeAndHoldTweaker"))
                {
                    ModifyTNHTweakerFiles(dir);
                    break;
                }
            }
        }

        public static void ModifyTNHTweakerFiles(string dir)
        {
            string deli = Path.Combine(dir, "TakeAndHoldTweaker.deli");
            string deliBak = Path.Combine(dir, "TakeAndHoldTweaker.deli.bak");
            string deliOld = Path.Combine(dir, "TakeAndHoldTweaker.deli.old");

            if (File.Exists(deliBak))
            {
                if (File.Exists(deli))
                    File.Delete(deliBak);
                else
                    File.Move(deliBak, deli);
            }

            if (File.Exists(deli))
            {
                File.Copy(deli, deliOld, true);
                File.Delete(deli);
                Logger.LogInfo("Disabled old TakeAndHoldTweaker install. To re-enable it, you must disable TNHFramework, and then disable and enable TakeAndHoldFramework");
            }
        }
    }
}