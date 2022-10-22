// ResearchTree.cs
// Copyright Karel Kroeze, 2020-2020

using HarmonyLib;
using System.Reflection;
using Verse;

namespace FluffyResearchTree
{
    public class ResearchTree : Mod
    {
        public ResearchTree(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("Fluffy.ResearchTree");
            harmony.PatchAll(Assembly.GetExecutingAssembly());


        }
    }
}