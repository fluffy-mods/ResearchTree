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
            //If Semi Random Research mod is loaded, suppress vanilla completion dialog.
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "It is used, ignore compiler messages")]
            private static void Prefix(ref bool doCompletionDialog)
            {
                if (ModLister.GetActiveModWithIdentifier("CaptainMuscles.SemiRandomResearch") == null)
                {
                    doCompletionDialog = false;
                }
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
