using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using RimWorld;
using Verse;
using UnityEngine;

namespace ResearchEngine
{

    public static class ThingDef_Extensions
    {

        internal static FieldInfo _allRecipesCached;

        // Dummy for functions needing a ref list
        public static List<Def> nullDefs = null;

        #region Recipe Cache

        public static void RecacheRecipes(this ThingDef thingDef, Map map, bool validateBills)
        {
            if (_allRecipesCached == null)
            {
                _allRecipesCached = typeof(ThingDef).GetField("allRecipesCached", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            _allRecipesCached.SetValue(thingDef, null);

            if (
                (!validateBills) ||
                (Current.ProgramState != ProgramState.MapInitializing)
            )
            {
                return;
            }

            // Get the recached recipes
            var recipes = thingDef.AllRecipes;

            // Remove bill on any table of this def using invalid recipes
            var buildings = map.listerBuildings.AllBuildingsColonistOfDef(thingDef);
            foreach (var building in buildings)
            {
                var iBillGiver = building as IBillGiver;
                if (iBillGiver != null)
                {
                    for (int i = 0; i < iBillGiver.BillStack.Count; ++i)
                    {
                        var bill = iBillGiver.BillStack[i];
                        if (!recipes.Exists(r => bill.recipe == r))
                        {
                            iBillGiver.BillStack.Delete(bill);
                            continue;
                        }
                    }
                }
            }

        }

        #endregion

        #region Availability

        public static bool IsFoodMachine(this ThingDef thingDef)
        {
            if (typeof(Building_NutrientPasteDispenser).IsAssignableFrom(thingDef.thingClass))
            {
                return true;
            }
            return false;
        }

        public static bool IsIngestible(this ThingDef thingDef)
        {
            return thingDef.ingestible != null;
        }

        public static bool IsDrug(this ThingDef thingDef)
        {
            if (
                (thingDef.IsIngestible()) &&
                (thingDef.ingestible.drugCategory == DrugCategory.Hard ||
                (thingDef.ingestible.drugCategory == DrugCategory.Social))
            )
            {
                return true;
            }
            return false;
        }

        public static bool IsImplant(this ThingDef thingDef)
        {
            // Return true if a recipe exist implanting this thing def
            return
                DefDatabase<RecipeDef>.AllDefsListForReading.Exists(r => (
                 (r.addsHediff != null) &&
                 (r.IsIngredient(thingDef))
             ));
        }

        public static RecipeDef GetImplantRecipeDef(this ThingDef thingDef)
        {
            // Get recipe for implant
            return
                DefDatabase<RecipeDef>.AllDefsListForReading.Find(r => (
                 (r.addsHediff != null) &&
                 (r.IsIngredient(thingDef))
             ));
        }

        public static HediffDef GetImplantHediffDef(this ThingDef thingDef)
        {
            // Get hediff for implant
            var recipeDef = thingDef.GetImplantRecipeDef();
            return recipeDef != null
                ? recipeDef.addsHediff
                    : null;
        }

        public static bool EverHasRecipes(this ThingDef thingDef)
        {
            return (
                (!thingDef.GetRecipesCurrent().NullOrEmpty()) ||
                (!thingDef.GetRecipesUnlocked(ref nullDefs).NullOrEmpty()) ||
                (!thingDef.GetRecipesLocked(ref nullDefs).NullOrEmpty())
            );
        }

        public static bool EverHasRecipe(this ThingDef thingDef, RecipeDef recipeDef)
        {
            return (
                (thingDef.GetRecipesCurrent().Contains(recipeDef)) ||
                (thingDef.GetRecipesUnlocked(ref nullDefs).Contains(recipeDef)) ||
                (thingDef.GetRecipesLocked(ref nullDefs).Contains(recipeDef))
            );
        }

        public static List<JoyGiverDef> GetJoyGiverDefsUsing(this ThingDef thingDef)
        {
            var joyGiverDefs = DefDatabase<JoyGiverDef>.AllDefsListForReading.Where(def => (
               (!def.thingDefs.NullOrEmpty()) &&
               (def.thingDefs.Contains(thingDef))
           )).ToList();
            return joyGiverDefs;
        }



        #endregion

        #region Lists of affected data

        public static List<RecipeDef> GetRecipesUnlocked(this ThingDef thingDef, ref List<Def> researchDefs)
        {
            // Recipes that are unlocked on thing with research
            var recipeDefs = new List<RecipeDef>();
            if (researchDefs != null)
            {
                researchDefs.Clear();
            }

            // Look at recipes
            var recipes = DefDatabase<RecipeDef>.AllDefsListForReading.Where(r => (
             (r.researchPrerequisite != null) &&
             (
                 (
                     (r.recipeUsers != null) &&
                     (r.recipeUsers.Contains(thingDef))
                 ) ||
                 (
                     (thingDef.recipes != null) &&
                     (thingDef.recipes.Contains(r))
                 )
             ) &&
             (!r.IsLockedOut())
         )).ToList();

            // Look in advanced research too
            var advancedResearch = Controller.Data.AdvancedResearchDefs.Where(a => (
               (a.IsRecipeToggle) &&
               (!a.HideDefs) &&
               (a.thingDefs.Contains(thingDef))
           )).ToList();

            // Aggregate advanced research
            foreach (var a in advancedResearch)
            {
                recipeDefs.AddRangeUnique(a.recipeDefs);
                if (researchDefs != null)
                {
                    if (a.researchDefs.Count == 1)
                    {
                        // If it's a single research project, add that
                        researchDefs.AddUnique(a.researchDefs[0]);
                    }
                    else
                    {
                        // Add the advanced project instead
                        researchDefs.AddUnique(a);
                    }
                }
            }
            return recipeDefs;
        }

        public static List<RecipeDef> GetRecipesLocked(this ThingDef thingDef, ref List<Def> researchDefs)
        {
            // Things it is locked on with research
            var recipeDefs = new List<RecipeDef>();
            if (researchDefs != null)
            {
                researchDefs.Clear();
            }

            // Look in advanced research
            var advancedResearch = Controller.Data.AdvancedResearchDefs.Where(a => (
               (a.IsRecipeToggle) &&
               (a.HideDefs) &&
               (a.thingDefs.Contains(thingDef))
           )).ToList();

            // Aggregate advanced research
            foreach (var a in advancedResearch)
            {
                recipeDefs.AddRangeUnique(a.recipeDefs);

                if (researchDefs != null)
                {
                    if (a.researchDefs.Count == 1)
                    {
                        // If it's a single research project, add that
                        researchDefs.AddUnique(a.researchDefs[0]);
                    }
                    else if (a.ResearchConsolidator != null)
                    {
                        // Add the advanced project instead
                        researchDefs.AddUnique(a.ResearchConsolidator);
                    }
                }
            }

            return recipeDefs;
        }

        public static List<RecipeDef> GetRecipesCurrent(this ThingDef thingDef)
        {
            return thingDef.AllRecipes;
        }

        public static List<RecipeDef> GetRecipesAll(this ThingDef thingDef)
        {
            // Things it is locked on with research
            var recipeDefs = new List<RecipeDef>();

            recipeDefs.AddRangeUnique(thingDef.GetRecipesCurrent());
            recipeDefs.AddRangeUnique(thingDef.GetRecipesUnlocked(ref nullDefs));
            recipeDefs.AddRangeUnique(thingDef.GetRecipesLocked(ref nullDefs));

            return recipeDefs;
        }






        #endregion

    }

}