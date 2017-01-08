// ResearchTree/LogHeadDB.cs
//
// Copyright Karel Kroeze, 2015.
//
// Created 2015-12-21 13:30

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FluffyResearchTree
{
    public class MainTabWindow_ResearchTree : MainTabWindow
    {
        internal static Vector2 _scrollPosition                     = Vector2.zero;
        public static List<Pair<ResearchNode, ResearchNode>> connections = new List<Pair<ResearchNode, ResearchNode>>();
        public static List<Pair<ResearchNode, ResearchNode>> highlightedConnections =
            new List<Pair<ResearchNode, ResearchNode>>();
        public static Dictionary<Rect, List<String>> hubTips        = new Dictionary<Rect, List<string>>();
        public static List<ResearchNode> nodes = new List<ResearchNode>();

        public override void PreOpen()
        {
            base.PreOpen();

            if ( !Tree.Initialized )
            {
                // initialize tree
                Tree.Initialize();

                // spit out debug info
#if DEBUG
                Log.Message( "ResearchTree :: duplicated positions:\n " + string.Join( "\n", Tree.Leaves.Where( n => Tree.Leaves.Any( n2 => n != n2 &&  n.X == n2.X && n.Y == n2.Y ) ).Select( n => n.X + ", " + n.Y + ": " + n.Label ).ToArray() ) );
                Log.Message( "ResearchTree :: out-of-bounds nodes:\n" + string.Join( "\n", Tree.Leaves.Where( n => n.X < 1 || n.Y < 1  ).Select( n => n.ToString() ).ToArray()  ) );
                Log.Message( Tree.ToString() );
#endif
            }
            
            // clear node availability caches
            ResearchNode.ClearCaches();

            // set to topleft (for some reason vanilla alignment overlaps bottom buttons).
            windowRect.x = 0f;
            windowRect.y = 0f;
            windowRect.width = Screen.width;
            windowRect.height = Screen.height - 35f;
        }

        public override float TabButtonBarPercent
        {
            get
            {
                if ( Find.ResearchManager.currentProj != null )
                {
                    return Find.ResearchManager.currentProj.ProgressPercent;
                }
                return 0;
            }
        }

        public override void DoWindowContents( Rect canvas )
        {
            PrepareTreeForDrawing();
            DrawTree( canvas );
        }

        private void PrepareTreeForDrawing()
        {
            foreach ( ResearchNode node in Tree.Leaves.OfType<ResearchNode>() )
            {
                nodes.Add( node );

                foreach ( ResearchNode parent in node.Parents )
                {
                    connections.Add( new Pair<ResearchNode, ResearchNode>( node, parent ) );
                }
            }
        }

        public void DrawTree( Rect canvas )
        {
            // set size of rect
            float width = ( Tree.Size.x ) * ( Settings.NodeSize.x + Settings.NodeMargins.x ); 
            float height = Tree.Size.z * ( Settings.NodeSize.y + Settings.NodeMargins.y );

            // main view rect
            Rect view = new Rect( 0f, 0f, width, height );
            Widgets.BeginScrollView( canvas, ref _scrollPosition, view );
            GUI.BeginGroup( view );

            Text.Anchor = TextAnchor.MiddleCenter;

            //// draw regular connections, not done first to better highlight done.
            //foreach ( var connection in connections.Where( pair => !pair.Second.Research.IsFinished ) )
            //{
            //    Tree.DrawLine( connection, Color.grey );
            //}

            //// draw connections from completed nodes
            //foreach ( var connection in connections.Where( pair => pair.Second.Research.IsFinished ) )
            //    Tree.DrawLine( connection, Color.green );
            //connections.Clear();

            //// draw highlight connections on top
            //foreach ( var connection in highlightedConnections )
            //    Tree.DrawLine( connection, GenUI.MouseoverColor, true );
            //highlightedConnections.Clear();

            // draw nodes on top of lines
            foreach ( ResearchNode node in nodes )
                node.Draw();
            nodes.Clear();

#if DEBUG
            foreach ( DummyNode dummyNode in Tree.Leaves.OfType<DummyNode>() )
                dummyNode.Draw();

            Tree.DrawDebug();
#endif

            // register hub tooltips
            foreach ( KeyValuePair<Rect, List<string>> pair in hubTips )
            {
                string text = string.Join( "\n", pair.Value.ToArray() );
                TooltipHandler.TipRegion( pair.Key, text );
            }
            hubTips.Clear();

            // draw Queue labels
            Queue.DrawLabels();

            // reset anchor
            Text.Anchor = TextAnchor.UpperLeft;

            GUI.EndGroup();
            Widgets.EndScrollView();
        }
    }
}
