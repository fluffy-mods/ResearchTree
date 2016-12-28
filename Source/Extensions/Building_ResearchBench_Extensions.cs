using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace FluffyResearchTree
{
    public static class Building_ResearchBench_Extensions
    {
        public static bool HasFacility( this Building_ResearchBench building, ThingDef facility )
        {
            CompAffectedByFacilities comp = building.GetComp<CompAffectedByFacilities>();
            if ( comp == null )
                return false;

            if ( comp.LinkedFacilitiesListForReading.Select( f => f.def ).Contains( facility ) )
                return true;

            return false;
        }
    }
}
