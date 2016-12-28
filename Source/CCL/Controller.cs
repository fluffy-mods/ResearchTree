using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using Verse;

namespace ResearchEngine.Controller
{

    [StaticConstructorOnStartup]
    public static class Data
    {
        #region Instance Data

        private static List<AdvancedResearchDef> advancedResearchDefs;
        internal static SubController[] SubControllers;

        #endregion


        #region Static Properties
        public static List<AdvancedResearchDef> AdvancedResearchDefs
        {
            get
            {
                if (advancedResearchDefs == null)
                {
                    // Get the initial ordered raw set of advanced research
                    advancedResearchDefs = DefDatabase<AdvancedResearchDef>.AllDefs.OrderBy(a => a.Priority).ToList();
                }
                return advancedResearchDefs;
            }
        }
        #endregion
    }
}
