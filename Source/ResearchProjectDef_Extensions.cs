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
        private static Dictionary<Def, List<Pair<Texture2D, string>>> cache = new Dictionary<Def, List<Pair<Texture2D, string>>>();

        public static List<Pair<Texture2D, string>> GetUnlockIconsAndDescs( this ResearchProjectDef research )
        {
            if (cache.ContainsKey( research ) )
            {
                return cache[research];
            }

            List<Pair<Texture2D, string>> unlocks = new List<Pair<Texture2D, string>>();

            // dumps recipes/plants unlocked, because of the peculiar way CCL helpdefs are done.
            List<ThingDef> dump = new List<ThingDef>();

            unlocks.AddRange( research.GetThingsUnlocked()
                                      .Where( d => d.Icon() != null )
                                      .Select( d => new Pair<Texture2D,string>( d.Icon(), "AllowsBuildingX".Translate( d.LabelCap ) ) ) );
            unlocks.AddRange( research.GetTerrainUnlocked()
                                      .Where( d => d.Icon() != null )
                                      .Select( d => new Pair<Texture2D, string>( d.Icon(), "AllowsBuildingX".Translate( d.LabelCap ) ) ) );
            unlocks.AddRange( research.GetRecipesUnlocked( ref dump )
                                      .Where( d => d.Icon() != null )
                                      .Select( d => new Pair<Texture2D, string>( d.Icon(), "AllowsCraftingX".Translate( d.LabelCap ) ) ) );
            string sowTags = string.Join( ", ", research.GetSowTagsUnlocked( ref dump ).ToArray() );
            unlocks.AddRange( dump.Where( d => d.Icon() != null )
                                  .Select( d => new Pair<Texture2D, string>( d.Icon(), "AllowsSowingXinY".Translate( d.LabelCap, sowTags ) ) ) );
            
            cache.Add( research, unlocks );
            return unlocks;
        } 
    }
}