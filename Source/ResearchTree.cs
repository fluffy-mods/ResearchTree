// ResearchTree.cs
// Copyright Karel Kroeze, 2018-2018

using System.Reflection;
using Harmony;
using Verse;

namespace FluffyResearchTree
{
    public class ResearchTree : Mod
    {
        public ResearchTree( ModContentPack content ) : base( content )
        {
            var harmony = HarmonyInstance.Create( "Fluffy.ResearchTree" );
            harmony.PatchAll( Assembly.GetExecutingAssembly() );
        }
    }
}