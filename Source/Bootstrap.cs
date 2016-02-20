using CommunityCoreLibrary;
using RimWorld;
using System.Reflection;

namespace FluffyResearchTree
{
    public class Bootstrap : SpecialInjector
    {
        #region Methods

        public override void Inject()
        {
            // create detour
            MethodInfo source = typeof( ResearchManager ).GetMethod( "MakeProgress" );
            MethodInfo destination = typeof( Queue ).GetMethod( "MakeProgress" );
            Detours.TryDetourFromTo( source, destination );
        }

        #endregion Methods
    }
}