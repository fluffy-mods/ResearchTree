// ResearchTree/LogHeadDB.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-12-21 13:30

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommunityCoreLibrary;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FluffyResearchTree
{
    public class MainTabWindow_ResearchTree : MainTabWindow
    {
        // tree view stuff
        internal static Vector2 _scrollPosition = Vector2.zero;
        private bool _noBenchWarned;

        // collect lines to be drawn
        private static List<Pair<Node, Node>> connections = new List<Pair<Node, Node>>();
        protected internal static List<Pair<Node, Node>> highlightedConnections = new List<Pair<Node, Node>>();
        
        public override void PreOpen()
        {
            base.PreOpen();
            
            if ( !ResearchTree.Initialized )
            {
                // initialize tree
                ResearchTree.Initialize();

                // spit out debug info
#if DEBUG
                foreach ( Tree tree in ResearchTree.Trees )
                {
                    Log.Message( tree.ToString() );
                }
                Log.Message( ResearchTree.Orphans.ToString() );
#endif

                // create detour
                MethodInfo source = typeof (ResearchManager).GetMethod( "MakeProgress" );
                MethodInfo destination = typeof (Queue).GetMethod( "MakeProgress" );
                Detours.TryDetourFromTo( source, destination );
            }

            // set to topleft (for some reason core alignment overlaps bottom buttons). 
            currentWindowRect.x = 0f;
            currentWindowRect.y = 0f;
            currentWindowRect.width = Screen.width;
            currentWindowRect.height = Screen.height - 35f;
        }

        public override float TabButtonBarPercent
        {
            get
            {
                if ( Find.ResearchManager.currentProj != null )
                {
                    return Find.ResearchManager.currentProj.PercentComplete;
                }
                return 0;
            }
        }
        
        public override void DoWindowContents( Rect canvas )
        {
            DrawTree( canvas );
            Log.Message( _scrollPosition.ToString() );
        }

        private void DrawTree( Rect canvas )
        {
            // clear connections list
            connections.Clear();
            highlightedConnections.Clear();

            // get total size of Research Tree
            int maxDepth = 0, totalWidth = 0;

            if ( ResearchTree.Trees.Any() )
            {
                maxDepth = ResearchTree.Trees.Max( tree => tree.MaxDepth );
                totalWidth = ResearchTree.Trees.Sum( tree => tree.Width );
            }
            
            maxDepth = Math.Max( maxDepth, ResearchTree.Orphans.MaxDepth );
            totalWidth += ResearchTree.Orphans.Width;

            float width = ( maxDepth + 1 ) * ( Settings.Button.x + Settings.Margin.x ); // zero based
            float height = totalWidth * ( Settings.Button.y + Settings.Margin.y );

            // main view rect
            Rect view = new Rect( 0f, 0f, width, height );
            Widgets.BeginScrollView( canvas, ref _scrollPosition, view );
            GUI.BeginGroup( view );

            Text.Anchor = TextAnchor.MiddleCenter;

            // draw Trees
            foreach ( Tree tree in ResearchTree.Trees )
            {
                foreach ( Node node in tree.Trunk.Concat( tree.Leaves ) )
                {
                    node.Draw();

                    foreach ( Node parent in node.Parents )
                    {
                        connections.Add( new Pair<Node, Node>( node, parent ));
                    }
                }
            }

            // draw Orphans
            foreach( Node node in ResearchTree.Orphans.Leaves )
            {
                node.Draw();

                foreach( Node parent in node.Parents )
                {
                    connections.Add( new Pair<Node, Node>( node, parent ) );
                }
            }

            // draw the connections
            DrawConnections();

            // draw queue labels
            Queue.DrawLabels();

            // reset anchor
            Text.Anchor = TextAnchor.UpperLeft;

            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        public void DrawConnections()
        {
            // draw regular connections, not done first to better highlight done.
            foreach( Pair<Node, Node> connection in connections.Where( pair => !pair.Second.Research.IsFinished ) )
            {
                ResearchTree.DrawLine( connection );
            }

            // draw connections from completed nodes
            foreach( Pair<Node, Node> connection in connections.Where( pair => pair.Second.Research.IsFinished ) )
            {
                ResearchTree.DrawLine( connection );
            }

            // draw highlight connections
            foreach ( Pair<Node, Node> connection in highlightedConnections )
            {
                ResearchTree.DrawLine( connection.First.Right, connection.Second.Left, GenUI.MouseoverColor );
            }
        }
    }
}