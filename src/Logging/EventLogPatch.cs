using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace NMica.HttpModules.Logging
{
    public class EventLogPatch
    {
        [HarmonyPatch]
        private static class EventLogInternal_WriteEntry
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools
                    .TypeByName("System.Diagnostics.EventLogInternal")
                    .GetMethod("WriteEntry", new[] { typeof(string), typeof(EventLogEntryType), typeof(int), typeof(short), typeof(byte[]) });
            }

            private static bool Prefix(object __instance, string message, EventLogEntryType type)
            {
                var source = Traverse.Create(__instance).Field("sourceName").GetValue<string>();
                var entry = $"{source}: {message}";
                if (type == EventLogEntryType.Error)
                    Console.Error.WriteLine(entry);
                else
                    Console.WriteLine(entry);
                return false;
            }
        }

        [HarmonyPatch]
        private static class EventLogInternal_WriteEvent
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools
                    .TypeByName("System.Diagnostics.EventLogInternal")
                    .GetMethod("WriteEvent", new[] { typeof(EventInstance), typeof(byte[]), typeof(object[]) });
            }

            private static bool Prefix(object __instance, EventInstance instance, object[] values)
            {
                if (values == null)
                    return false;
                var message = values.Select(x => x.ToString() + "\n");
                var source = Traverse.Create(__instance).Field("sourceName").GetValue<string>();
                var entry = $"{source}: {message}";
                if (instance.EntryType == EventLogEntryType.Error)
                    Console.Error.WriteLine(entry);
                else
                    Console.WriteLine(entry);
                return false;
            }
        }

        [HarmonyPatch]
        private static class EventLog_SourceExists
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools
                    .TypeByName("System.Diagnostics.EventLog")
                    .GetMethod("SourceExists",  new[] { typeof(string), typeof(string) });
            }

            private static bool Prefix(object __instance, ref bool __result) //, bool wantToCreate)
            {
                __result = true;
                return false;
            }
        }
    }
}