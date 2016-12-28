using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using RimWorld;
using Verse;

namespace ResearchEngine
{

    [StaticConstructorOnStartup]
    public static class Find_Extensions
    {

        // Suffixes which (may) need to be removed to find the mod for a def
        private static List<string> def_suffixes;

        static Find_Extensions()
        {
            def_suffixes = new List<string>(){
                "_Frame",
                "_Blueprint",
                "_Blueprint_Install",
                "_Corpse",
                "_Leather",
                "_Meat"
            };
        }

        // This is a safe method of fetching a map component of a specified type
        // If an instance of the component doesn't exist or map isn't loaded it will return null
        public static T MapComponent<T>() where T : MapComponent
        {
            if (
                (Find.VisibleMap == null) ||
                (Find.VisibleMap.components.NullOrEmpty())
            )
            {
                return null;
            }
            return Find.VisibleMap.components.FirstOrDefault(c => c.GetType() == typeof(T)) as T;
        }

        public static MapComponent MapComponent(Type t)
        {
            if (
                (Find.VisibleMap == null) ||
                (Find.VisibleMap.components.NullOrEmpty())
            )
            {
                return null;
            }
            return Find.VisibleMap.components.FirstOrDefault(c => c.GetType() == t);
        }

        // Get the def of a specific type for a specific mod
        public static Def DefOfTypeForMod(ModContentPack mod, Def searchDef)
        {
            if (mod == null)
            {
                return null;
            }
            return mod.AllDefs.FirstOrDefault(def => (
               (def.GetType() == searchDef.GetType()) &&
               (def.defName == searchDef.defName)
           ));
        }

        // Get the def list of a specific type for a specific mod
        public static List<T> DefListOfTypeForMod<T>(ModContentPack mod) where T : Def, new()
        {
            if (mod == null)
            {
                return null;
            }
            var list = mod.AllDefs.Where(def => (
               (def.GetType() == typeof(T))
            )).ToList();
            return list.ConvertAll(def => ((T)def));
        }
    }
}
