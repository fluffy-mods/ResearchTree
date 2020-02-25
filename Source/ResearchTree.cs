// ResearchTree.cs
// Copyright Karel Kroeze, 2018-2018

using System.Reflection;
using HarmonyLib;
//using Multiplayer.API;
using Verse;

namespace FluffyResearchTree
{
    public class ResearchTree : Mod
    {
        public ResearchTree( ModContentPack content ) : base( content )
        {
            var harmony = new Harmony( "Fluffy.ResearchTree" );
            harmony.PatchAll( Assembly.GetExecutingAssembly() );

//            if ( MP.enabled )
//                MP.RegisterAll();
        }
    }
}