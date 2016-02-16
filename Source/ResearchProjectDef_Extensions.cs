// ResearchTree/ResearchProjectDef_Extensions.cs
// 
// Copyright Karel Kroeze, 2016.
// 
// Created 2016-02-06 20:09

using System.Collections.Generic;
using System.Linq;
using Verse;
using CommunityCoreLibrary;
using UnityEngine;

namespace FluffyResearchTree
{
    public static class ResearchProjectDef_Extensions
    {
        private static Dictionary<Def, List<Pair<Def, string>>> cache = new Dictionary<Def, List<Pair<Def, string>>>();

        public static List<ResearchProjectDef> GetPrerequisitesRecursive( this ResearchProjectDef research )
        {
            List<ResearchProjectDef> result = new List<ResearchProjectDef>();
            if( research.prerequisites.NullOrEmpty() )
            {
                return result;
            }
            Stack<ResearchProjectDef> stack = new Stack<ResearchProjectDef>( research.prerequisites );
            
            while ( stack.Count > 0 )
            {
                var parent = stack.Pop();
                result.Add( parent );

                if( !parent.prerequisites.NullOrEmpty() )
                {
                    foreach (var grandparent in parent.prerequisites )
                    {
                        stack.Push( grandparent );
                    }
                }
            }

            return result.Distinct().ToList();
        }

        public static List<Pair<Def, string>> GetUnlockDefsAndDescs( this ResearchProjectDef research )
        {
            if (cache.ContainsKey( research ) )
            {
                return cache[research];
            }

            List<Pair<Def, string>> unlocks = new List<Pair<Def, string>>();

            // dumps recipes/plants unlocked, because of the peculiar way CCL helpdefs are done.
            List<ThingDef> dump = new List<ThingDef>();

            unlocks.AddRange( research.GetThingsUnlocked()
                                      .Where( d => d.IconTexture() != null )
                                      .Select( d => new Pair<Def,string>( d, "Fluffy.ResearchTree.AllowsBuildingX".Translate( d.LabelCap ) ) ) );
            unlocks.AddRange( research.GetTerrainUnlocked()
                                      .Where( d => d.IconTexture() != null )
                                      .Select( d => new Pair<Def, string>( d, "Fluffy.ResearchTree.AllowsBuildingX".Translate( d.LabelCap ) ) ) );
            unlocks.AddRange( research.GetRecipesUnlocked( ref dump )
                                      .Where( d => d.IconTexture() != null )
                                      .Select( d => new Pair<Def, string>( d, "Fluffy.ResearchTree.AllowsCraftingX".Translate( d.LabelCap ) ) ) );
            string sowTags = string.Join( " and ", research.GetSowTagsUnlocked( ref dump ).ToArray() );
            unlocks.AddRange( dump.Where( d => d.IconTexture() != null )
                                  .Select( d => new Pair<Def, string>( d, "Fluffy.ResearchTree.AllowsSowingXinY".Translate( d.LabelCap, sowTags ) ) ) );
            
            cache.Add( research, unlocks );
            return unlocks;
        } 
    }
}