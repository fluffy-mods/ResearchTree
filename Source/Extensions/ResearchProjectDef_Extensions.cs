// Karel Kroeze
// ResearchProjectDef_Extensions.cs
// 2016-12-28

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace FluffyResearchTree
{
    public static class ResearchProjectDef_Extensions
    {
        #region Fields

        private static Dictionary<Def, List<Pair<Def, string>>> _unlocksCache =
            new Dictionary<Def, List<Pair<Def, string>>>();

        #endregion Fields

        #region Methods
        
        public static List<ResearchProjectDef> ExclusiveDescendants( this ResearchProjectDef research )
        {
            var descendants = new List<ResearchProjectDef>();

            // recursively go through all children
            // populate initial queue
            var queue =
                new Queue<ResearchProjectDef>(
                    DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(
                                                                                res =>
                                                                                res.prerequisites.Contains( research ) ) );

            // for each item in queue, determine if there's something unlocking it
            // if not, add to the list, and queue up children.
            while ( queue.Count > 0 )
            {
                ResearchProjectDef current = queue.Dequeue();

                descendants.Add( current );
                foreach (
                    ResearchProjectDef descendant in
                        DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(
                                                                                    res =>
                                                                                    res.prerequisites.Contains( current ) )
                    )
                    queue.Enqueue( descendant );
            }

            return descendants;
        }

        public static IEnumerable<ThingDef> GetPlantsUnlocked( this ResearchProjectDef research )
        {
            return DefDatabase<ThingDef>.AllDefsListForReading
                                        .Where( td => td.plant?.sowResearchPrerequisites?.Contains( research ) ?? false );
        }

        public static List<ResearchProjectDef> GetPrerequisitesRecursive( this ResearchProjectDef research )
        {
            // keep a list of prerequites
            var prerequisites = new List<ResearchProjectDef>();
            if ( research.prerequisites.NullOrEmpty() )
                return prerequisites;

            // keep a stack of prerequisites that should be checked
            var stack = new Stack<ResearchProjectDef>( research.prerequisites.Where( parent => parent != research ) );

            // keep on checking everything on the stack until there is nothing left
            while ( stack.Count > 0 )
            {
                // add to list of prereqs
                ResearchProjectDef parent = stack.Pop();
                prerequisites.Add( parent );

                // add prerequitsite's prereqs to the stack
                if ( !parent.prerequisites.NullOrEmpty() )
                {
                    foreach ( ResearchProjectDef grandparent in parent.prerequisites )
                    {
                        // but only if not a prerequisite of itself, and not a cyclic prerequisite
                        if ( grandparent != parent && !prerequisites.Contains( grandparent ) )
                            stack.Push( grandparent );
                    }
                }
            }

            return prerequisites.Distinct().ToList();
        }

        public static IEnumerable<RecipeDef> GetRecipesUnlocked( this ResearchProjectDef research )
        {
            // recipe directly locked behind research
            IEnumerable<RecipeDef> direct =
                DefDatabase<RecipeDef>.AllDefsListForReading.Where( rd => rd.researchPrerequisite == research );

            // recipe building locked behind research
            IEnumerable<RecipeDef> building =
                DefDatabase<ThingDef>.AllDefsListForReading
                                     .Where( td => td.researchPrerequisites?.Contains( research ) ?? false
                                                   && !td.AllRecipes.NullOrEmpty() )
                                     .SelectMany( td => td.AllRecipes );

            // return union of these two sets
            return direct.Concat( building ).Distinct();
        }

        public static IEnumerable<TerrainDef> GetTerrainUnlocked( this ResearchProjectDef research )
        {
            return DefDatabase<TerrainDef>.AllDefsListForReading
                                          .Where( td => td.researchPrerequisites?.Contains( research ) ?? false );
        }

        public static IEnumerable<ThingDef> GetThingsUnlocked( this ResearchProjectDef research )
        {
            return DefDatabase<ThingDef>.AllDefsListForReading
                                        .Where( td => td.researchPrerequisites?.Contains( research ) ?? false );
        }

        public static List<Pair<Def, string>> GetUnlockDefsAndDescs( this ResearchProjectDef research )
        {
            if ( _unlocksCache.ContainsKey( research ) )
                return _unlocksCache[research];

            var unlocks = new List<Pair<Def, string>>();

            unlocks.AddRange( research.GetThingsUnlocked()
                                      .Where( d => d.IconTexture() != null )
                                      .Select(
                                              d =>
                                              new Pair<Def, string>( d,
                                                                     "Fluffy.ResearchTree.AllowsBuildingX".Translate(
                                                                                                                     d
                                                                                                                         .LabelCap ) ) ) );
            unlocks.AddRange( research.GetTerrainUnlocked()
                                      .Where( d => d.IconTexture() != null )
                                      .Select(
                                              d =>
                                              new Pair<Def, string>( d,
                                                                     "Fluffy.ResearchTree.AllowsBuildingX".Translate(
                                                                                                                     d
                                                                                                                         .LabelCap ) ) ) );
            unlocks.AddRange( research.GetRecipesUnlocked()
                                      .Where( d => d.IconTexture() != null )
                                      .Select(
                                              d =>
                                              new Pair<Def, string>( d,
                                                                     "Fluffy.ResearchTree.AllowsCraftingX".Translate(
                                                                                                                     d
                                                                                                                         .LabelCap ) ) ) );
            unlocks.AddRange( research.GetPlantsUnlocked()
                                      .Where( d => d.IconTexture() != null )
                                      .Select(
                                              d =>
                                              new Pair<Def, string>( d,
                                                                     "Fluffy.ResearchTree.AllowsPlantingX".Translate(
                                                                                                                     d
                                                                                                                         .LabelCap ) ) ) );

            _unlocksCache.Add( research, unlocks );
            return unlocks;
        }

        public static Node Node( this ResearchProjectDef research )
        {
            return ResearchTree.Forest.FirstOrDefault( node => node.Research == research );
        }

        #endregion Methods
    }
}
