using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityCoreLibrary;
using RimWorld;
using System.Reflection;

namespace FluffyResearchTree
{
    public class Bootstrap : SpecialInjector
    {
        public override void Inject()
        {
            // create detour
            MethodInfo source = typeof (ResearchManager).GetMethod( "MakeProgress" );
            MethodInfo destination = typeof (Queue).GetMethod( "MakeProgress" );
            Detours.TryDetourFromTo( source, destination );
        }
    }
}
