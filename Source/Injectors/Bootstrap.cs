using ResearchEngine;
using RimWorld;
using System.Reflection;

namespace ResearchTree
{
    public class Bootstrap : SpecialInjector
    {
        #region Methods

        public override bool Inject()
        {
            // create detour
            MethodInfo source = typeof( ResearchManager ).GetMethod( "ResearchPerformed" );
            MethodInfo destination = typeof( Queue ).GetMethod( "ResearchPerformed" );
            Detours.TryDetourFromTo( source, destination );

			return true;
        }

        #endregion Methods
    }
}