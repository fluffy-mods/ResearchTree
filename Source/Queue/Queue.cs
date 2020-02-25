// Karel Kroeze
// Queue.cs
// 2016-12-28

using System.Collections.Generic;
using System.Linq;
//using Multiplayer.API;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using static FluffyResearchTree.Assets;
using static FluffyResearchTree.Constants;

namespace FluffyResearchTree
{
    public class Queue : WorldComponent
    {
        #region Fields

        private static readonly List<ResearchNode> _queue = new List<ResearchNode>();
        private static List<ResearchProjectDef> _saveableQueue;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Removes and returns the first node in the queue.
        /// </summary>
        /// <returns></returns>
        public static ResearchNode Pop
        {
            get
            {
                if ( _queue != null && _queue.Count > 0 )
                {
                    ResearchNode node = _queue[0];
                    _queue.RemoveAt( 0 );
                    return node;
                }

                return null;
            }
        }

        #endregion Properties

        public Queue(World world) : base(world)
        {
        }

        #region Methods

        public static void TryDequeue( ResearchNode node )
        {
            if ( _queue.Contains( node ) )
                Dequeue( node );
        }

//        [SyncMethod]
        public static void Dequeue( ResearchNode node )
        {
            // remove this node
            _queue.Remove( node );

            // remove all nodes that depend on it
            List<ResearchNode> followUps = _queue.Where( n => n.GetMissingRequiredRecursive().Contains( node ) ).ToList();
            foreach ( ResearchNode followUp in followUps )
                _queue.Remove( followUp );

            // if currently researching this node, stop that
            if ( Find.ResearchManager.currentProj == node.Research )
                Find.ResearchManager.currentProj = null;
        }

        public static void DrawLabels( Rect visibleRect )
        {
            Profiler.Start("Queue.DrawLabels");
            var i = 1;
            foreach ( ResearchNode node in _queue )
            {
                if ( node.IsVisible( visibleRect ) )
                {
                    var main = ColorCompleted[node.Research.techLevel];
                    var background = i > 1 ? ColorUnavailable[node.Research.techLevel] : main;
                    DrawLabel( node.QueueRect, main, background, i );
                }
                i++;
            }
            Profiler.End();
        }

        public static void DrawLabel( Rect canvas, Color main, Color background, int label )
        {
            // draw coloured tag
            GUI.color = main;
            GUI.DrawTexture( canvas, CircleFill);

            // if this is not first in line, grey out centre of tag
            if (background != main)
            {
                GUI.color = background;
                GUI.DrawTexture(canvas.ContractedBy( 2f ), CircleFill);
            }

            // draw queue number
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label( canvas, label.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static int NumQueued => _queue.Count - 1;

        public static void Enqueue( ResearchNode node, bool add )
        {
            Log.Debug($"Enqueuing: {node.Research.defName }");

            // if we're not adding, clear the current queue and current research project
            if ( !add )
            {
                _queue.Clear();
                Find.ResearchManager.currentProj = null;
            }

            // add to the queue if not already in it
            if ( !_queue.Contains( node ) )
                _queue.Add( node );

            // try set the first research in the queue to be the current project.
            ResearchNode next = _queue.First();
            Find.ResearchManager.currentProj = next?.Research; // null if next is null.
        }

//        [SyncMethod]
        public static void EnqueueRange( IEnumerable<ResearchNode> nodes, bool add )
        {
            TutorSystem.Notify_Event( "StartResearchProject" );

            // clear current Queue if not adding
            if ( !add )
            {
                _queue.Clear();
                Find.ResearchManager.currentProj = null;
            }

            // sorting by depth ensures prereqs are met - cost is just a bonus thingy.
            foreach ( ResearchNode node in nodes.OrderBy( node => node.X ).ThenBy( node => node.Research.CostApparent ) )
                Enqueue( node, true );
        }

        public static bool IsQueued( ResearchNode node )
        {
            return _queue.Contains( node );
        }

        public static void TryStartNext( ResearchProjectDef finished )
        {
            var current = _queue.FirstOrDefault()?.Research;
            Log.Debug( "TryStartNext: current; {0}, finished; {1}", current, finished );
            if ( finished != _queue.FirstOrDefault()?.Research )
            {
                TryDequeue( finished );
                return;
            }
            _queue.RemoveAt( 0 );
            var next = _queue.FirstOrDefault()?.Research;
            Log.Debug( "TryStartNext: next; {0}", next );
            Find.ResearchManager.currentProj = next;
            DoCompletionLetter( current, next );
        }

        private static void DoCompletionLetter( ResearchProjectDef current, ResearchProjectDef next )
        {
            // message
            string label = "ResearchFinished".Translate( current.LabelCap);
            string text = current.LabelCap + "\n\n" + current.description;

            if (next != null)
            {
                text += "\n\n" + "Fluffy.ResearchTree.NextInQueue".Translate(next.LabelCap);
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent );
            }
            else
            {
                text += "\n\n" + "Fluffy.ResearchTree.NextInQueue".Translate("Fluffy.ResearchTree.None".Translate());
                Find.LetterStack.ReceiveLetter( label, text, LetterDefOf.NeutralEvent );
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            // store research defs as these are the defining elements
            if ( Scribe.mode == LoadSaveMode.Saving )
                _saveableQueue = _queue.Select( node => node.Research ).ToList();

            Scribe_Collections.Look( ref _saveableQueue, "Queue", LookMode.Def );

            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                // initialize the queue
                foreach (ResearchProjectDef research in _saveableQueue)
                {
                    // find a node that matches the research - or null if none found
                    ResearchNode node = research.ResearchNode();

                    // enqueue the node
                    if (node != null)
                    {
                        Log.Debug("Adding {0} to queue", node.Research.LabelCap);
                        Enqueue(node, true);
                    }
                    else
                    {
                        Log.Debug("Could not find node for {0}", research.LabelCap);
                    }
                }
            }
        }
        
        #endregion Methods

        public static void DrawQueue( Rect canvas, bool interactible )
        {
            Profiler.Start( "Queue.DrawQueue" );
            if ( !_queue.Any() )
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = TechLevelColor;
                Widgets.Label( canvas, "Fluffy.ResearchTree.NothingQueued".Translate() );
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            var pos = canvas.min;
            for ( int i = 0; i < _queue.Count && pos.x + NodeSize.x < canvas.xMax; i++ )
            {
                var node = _queue[i];
                var rect = new Rect(
                    pos.x - Margin,
                    pos.y - Margin,
                    NodeSize.x + 2 * Margin,
                    NodeSize.y + 2 * Margin
                );
                node.DrawAt( pos, rect, true );
                if ( interactible && Mouse.IsOver( rect ))
                    MainTabWindow_ResearchTree.Instance.CenterOn( node );
                pos.x += NodeSize.x + Margin;
            }
            Profiler.End();
        }

        public static void Notify_InstantFinished()
        {
            foreach ( var node in new List<ResearchNode>( _queue ) )
                if ( node.Research.IsFinished )
                    TryDequeue( node );

            Find.ResearchManager.currentProj = _queue.FirstOrDefault()?.Research;
        }
    }
}
