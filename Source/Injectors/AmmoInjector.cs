using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using ResearchEngine;

namespace ResearchTree
{
    [StaticConstructorOnStartup]
    internal static class Injector
    {

        static Injector()
        {
            LongEventHandler.QueueLongEvent(Inject, "LibraryStartup", false, null);
        }

        public static void Inject()
        {
            var injectorOR = new Bootstrap();
            if (injectorOR.Inject()) Log.Message("Research tree :: Injected");
            else Log.Error("failed to get injected research tree.");

        }

        public static bool InjectAmmos()
        {


            return true;
        }
    }
}
