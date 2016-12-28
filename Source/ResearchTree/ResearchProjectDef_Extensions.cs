// ResearchTree/ResearchProjectDef_Extensions.cs
//
// Copyright Karel Kroeze, 2016.
//
// Created 2016-02-06 20:09

using ResearchEngine;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ResearchTree
{
    public static class ResearchProjectDef_Extensions
    {
        #region Fields

        private static Dictionary<Def, List<Pair<Def, string>>> _unlocksCache = new Dictionary<Def, List<Pair<Def, string>>>();

        #endregion Fields

        #region Methods

        public static List<ResearchProjectDef> ExclusiveDescendants( this ResearchProjectDef research )
        {
            List<ResearchProjectDef> descendants = new List<ResearchProjectDef>();

            // recursively go through all children
            // populate initial queue
            Queue<ResearchProjectDef> queue = new Queue<ResearchProjectDef>( DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where( res => res.prerequisites.Contains( research ) ) );

            // for each item in queue, determine if there's something unlocking it
            // if not, add to the list, and queue up children.
            while ( queue.Count > 0 )
            {
                ResearchProjectDef current = queue.Dequeue();

                if ( !ResearchEngine.Controller.Data.AdvancedResearchDefs.Any(
                        ard => ard.IsResearchToggle &&
                               !ard.HideDefs &&
                               !ard.IsLockedOut() &&
                               ard.effectedResearchDefs.Contains( current ) ) &&
                     !descendants.Contains( current ) )
                {
                    descendants.Add( current );
                    foreach ( ResearchProjectDef descendant in DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where( res => res.prerequisites.Contains( current ) ) )
                    {
                        queue.Enqueue( descendant );
                    }
                }
            }

            return descendants;
        }

        public static List<ResearchProjectDef> GetPrerequisitesRecursive( this ResearchProjectDef research )
        {
            List<ResearchProjectDef> result = new List<ResearchProjectDef>();
            if ( research.prerequisites.NullOrEmpty() )
            {
                return result;
            }
            Stack<ResearchProjectDef> stack = new Stack<ResearchProjectDef>( research.prerequisites.Where( parent => parent != research ) );

            while ( stack.Count > 0 )
            {
                var parent = stack.Pop();
                result.Add( parent );

                if ( !parent.prerequisites.NullOrEmpty() )
                {
                    foreach ( var grandparent in parent.prerequisites )
                    {
                        if ( grandparent != parent )
                            stack.Push( grandparent );
                    }
                }
            }

            return result.Distinct().ToList();
        }

        public static List<Pair<Def, string>> GetUnlockDefsAndDescs( this ResearchProjectDef research )
        {
            if ( _unlocksCache.ContainsKey( research ) )
            {
                return _unlocksCache[research];
            }

            List<Pair<Def, string>> unlocks = new List<Pair<Def, string>>();

            // dumps recipes/plants unlocked, because of the peculiar way CCL helpdefs are done.
            List<ThingDef> dump = new List<ThingDef>();

            unlocks.AddRange( research.GetThingsUnlocked()
                                      .Where( d => d.IconTexture() != null )
                                      .Select( d => new Pair<Def, string>( d, "Fluffy.ResearchTree.AllowsBuildingX".Translate( d.LabelCap ) ) ) );
            unlocks.AddRange( research.GetTerrainUnlocked()
                                      .Where( d => d.IconTexture() != null )
                                      .Select( d => new Pair<Def, string>( d, "Fluffy.ResearchTree.AllowsBuildingX".Translate( d.LabelCap ) ) ) );
            unlocks.AddRange( research.GetRecipesUnlocked( ref dump )
                                      .Where( d => d.IconTexture() != null )
                                      .Select( d => new Pair<Def, string>( d, "Fluffy.ResearchTree.AllowsCraftingX".Translate( d.LabelCap ) ) ) );
            string sowTags = string.Join( " and ", research.GetSowTagsUnlocked( ref dump ).ToArray() );
            unlocks.AddRange( dump.Where( d => d.IconTexture() != null )
                                  .Select( d => new Pair<Def, string>( d, "Fluffy.ResearchTree.AllowsSowingXinY".Translate( d.LabelCap, sowTags ) ) ) );

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