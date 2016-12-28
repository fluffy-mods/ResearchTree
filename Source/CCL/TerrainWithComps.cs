using System;
using System.Collections.Generic;

using RimWorld;
using Verse;

namespace ResearchEngine
{

    public class TerrainWithComps : TerrainDef
    {

        #region XML Data

        public List<CompProperties> comps = new List<CompProperties>();

        #endregion

        //[Unsaved]

        #region Instance Data

        #endregion

        #region Process State

#if DEBUG
        public bool IsValidPlaceWorker(Type placeWorker)
        {
            if (
         //       (placeWorker != typeof(PlaceWorker_NotOnTerrain)) &&
           //     (placeWorker != typeof(PlaceWorker_OnlyOnTerrain)) &&
                (placeWorker != typeof(PlaceWorker_NotUnderRoof)) &&
        //        (placeWorker != typeof(PlaceWorker_OnlyUnderRoof)) &&
//(!placeWorker.IsSubclassOf(typeof(PlaceWorker_NotOnTerrain))) &&
        //        (!placeWorker.IsSubclassOf(typeof(PlaceWorker_OnlyOnTerrain))) &&
                (!placeWorker.IsSubclassOf(typeof(PlaceWorker_NotUnderRoof))) // &&
        //        (!placeWorker.IsSubclassOf(typeof(PlaceWorker_OnlyUnderRoof)))
            )
            {
                return false;
            }
            return true;
        }

        public override void PostLoad()
        {
            base.PostLoad();

            // Validate comps
            if (!comps.NullOrEmpty())
            {
                // Terrain with comps only supports a small set of comps
                for (int i = 0; i < comps.Count; ++i)
                {
                    var comp = comps[i];
                    var compClass = comp.compClass;
                }
            }
        }
#endif

        #endregion

        #region Query State

        public CompProperties GetCompProperties(Type compType)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i].compClass == compType)
                {
                    return comps[i];
                }
            }
            return (CompProperties)null;
        }

        #endregion
    }
}