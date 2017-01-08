using HugsLib.Source.Detour;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace FluffyResearchTree
{
    public class ResearchManager_Detour
    {
        #region Fields

        private const BindingFlags ALL = (BindingFlags) 60;

        private static FieldInfo _globalProgressFactor_FI
            = typeof( ResearchManager ).GetField( "GlobalProgressFactor", ALL );

        private static FieldInfo _progress_FI = typeof( ResearchManager ).GetField( "progress", ALL );

        #endregion Fields

        #region Properties

        public static float GlobalProgressFactor
        {
            get
            {
                if ( _globalProgressFactor_FI == null )
                    throw new Exception( "GlobalProgressFactor field info null" );

                return (float)_globalProgressFactor_FI.GetValue( Find.ResearchManager );
            }
        }

        public static Dictionary<ResearchProjectDef, float> Progress
        {
            get
            {
                if ( _progress_FI == null )
                    throw new Exception( "progress field info null" );

                return _progress_FI.GetValue( Find.ResearchManager ) as Dictionary<ResearchProjectDef, float>;
            }
        }

        #endregion Properties

        #region Methods

        ///  <summary>
        ///  Override for Verse.ResearchMananager.ResearchPerformed
        ///
        ///  Changes default pop-up when research is complete to an inbox message, and starts the next research in the queue - if available.
        ///  </summary>
        ///  <param name="amount"></param>
        /// <param name="researcher"></param>
        [DetourMethod( typeof( RimWorld.ResearchManager ), "ResearchPerformed" )]
        public void ResearchPerformed( float amount, Pawn researcher )
        {
            // get research manager instance
            ResearchManager manager = Find.ResearchManager;

            if ( manager.currentProj == null )
            {
                Log.Error( "Researched without having an active project." );
                return;
            }

            amount *= GlobalProgressFactor;
            if ( researcher != null && researcher.Faction != null )
                amount /= manager.currentProj.CostFactor( researcher.Faction.def.techLevel );
            if ( DebugSettings.fastResearch )
                amount *= 500f;
            researcher?.records.AddTo( RecordDefOf.ResearchPointsResearched, amount );

            Dictionary<ResearchProjectDef, float> progress = Progress;
            progress[manager.currentProj] = manager.GetProgress( manager.currentProj ) + amount;

            // if not finished we're done
            if ( !manager.currentProj.IsFinished )
                return;

            // otherwise, do some additional stuff;
            manager.ReapplyAllMods();

            // remove current from queue
            ResearchNode completed = Queue.Pop;

            if ( researcher != null )
                TaleRecorder.RecordTale( TaleDefOf.FinishedResearchProject, researcher, completed.Research );

            // message
            string label = "ResearchFinished".Translate( completed.Research.LabelCap );
            string text = "ResearchFinished".Translate( completed.Research.LabelCap ) + "\n\n" + completed.Research.DescriptionDiscovered;

            // if there's something on the queue start it, and push an appropriate message
            ResearchNode next;
            if ( Queue.TryStartNext( out next ) )
            {
                text += "\n\n" + "NextInQueue".Translate( next.Research.LabelCap );
                Find.LetterStack.ReceiveLetter( label, text, LetterType.Good );
            }
            else
            {
                text += "\n\n" + "NextInQueue".Translate( "none".Translate() );
                Find.LetterStack.ReceiveLetter( label, text, LetterType.BadNonUrgent );
            }
        }

        #endregion Methods
    }
}
