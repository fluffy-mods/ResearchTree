// Queue_HarmonyPatches.cs
// Copyright Karel Kroeze, 2020-2020

using HarmonyLib;
using RimWorld;
using Verse;

namespace FluffyResearchTree
{
    public class HarmonyPatches_Queue
    {
        [HarmonyPatch(typeof(ResearchManager), "FinishProject")]
        public class DoCompletionDialog
        {
            // suppress vanilla completion dialog, we never want to show it.
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "It is used, ignore compiler messages")]
            private static void Prefix(ref bool doCompletionDialog)
            {
                doCompletionDialog = false;
            }


            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "It is used, ignore compiler messages")]
            private static void Postfix(ResearchProjectDef proj)
            {
                if (proj.IsFinished)
                {
                    Log.Debug("Patch of FinishProject: {0} finished", proj.label);
                    Queue.TryStartNext(proj);
                }
            }
        }
    }
}
