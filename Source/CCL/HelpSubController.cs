using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;

namespace ResearchEngine.Controller
{

    /// <summary>
    /// This controller generates the implied help defs
    /// </summary>
    internal class HelpSubController : SubController
    {

        public override string Name => "Help Generator";

        // Override sequence priorities
        public override int InitializationPriority => 80;

        public override bool Initialize()
        {
            // Don't auto-gen help if "quicktest" or "nohelp" command line switches are used
            if (
                (!GenCommandLine.CommandLineArgPassed("quicktest")) &&
                (!GenCommandLine.CommandLineArgPassed("nohelp"))
            )
            {

                LongEventHandler.SetCurrentEventText("LibraryHelpGen".Translate());

                var stringBuilder = new StringBuilder();

                var startTime = DateTime.Now;

                if (!HelpBuilder.ResolveImpliedDefs())
                {
                    strReturn = "Unexpected error in HelpBuilder.ResolveImpliedDefs()";
                    State = SubControllerState.InitializationError;
                    return false;
                }

                var finishTime = DateTime.Now;
                var finalTime = finishTime - startTime;

                LongEventHandler.SetCurrentEventText("Initializing".Translate());
            }
            strReturn = "Initialized";
            State = SubControllerState.Hybernating;
            return true;
        }

    }

}
