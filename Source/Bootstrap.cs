using CommunityCoreLibrary;
using RimWorld;
using System.Reflection;

namespace FluffyResearchTree
{
    public class Bootstrap : SpecialInjector
    {
        #region Methods

        public override bool Inject()
        {
			Verse.Log.Message("Inject");
            // create detour
            MethodInfo source = typeof( ResearchManager ).GetMethod( "ResearchPerformed" );
            MethodInfo destination = typeof( Queue ).GetMethod( "ResearchPerformed" );
            Detours.TryDetourFromTo( source, destination );

			return true;
        }

        #endregion Methods
    }
}