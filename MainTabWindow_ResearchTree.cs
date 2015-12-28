// ResearchTree/LogHeadDB.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-12-21 13:30

using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyResearchTree
{
    public class MainTabWindow_ResearchTree : MainTabWindow
    {
        private Vector2 _scrollPosition = Vector2.zero;

        public override void PreOpen()
        {
            base.PreOpen();

            // set to topleft (for some reason core alignment overlaps bottom buttons). 
            // size is set with RequestedTabSize to fullscreen.
            currentWindowRect.x = 0f;
            currentWindowRect.y = 0f;

            if ( !ResearchTree.Initialized )
            {
                ResearchTree.Initialized = true;
                // initialize tree
                ResearchTree.Initialize();

                // create detour
                MethodInfo source = typeof (ResearchManager).GetMethod( "MakeProgress" );
                MethodInfo destination = typeof (Queue).GetMethod( "MakeProgress" );
                CommunityCoreLibrary.Detours.TryDetourFromTo( source, destination );
            }
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

        public override Vector2 RequestedTabSize => new Vector2( Screen.width, Screen.height );

        public override void DoWindowContents( Rect canvas )
        {
            // get total size of Research Tree
            int maxDepth = Math.Max( ResearchTree.Trees.Max( tree => tree.MaxDepth ), ResearchTree.OrphanDepths.x );
            int totalWidth = ResearchTree.Trees.Sum( tree => tree.Width ) + ResearchTree.OrphanWidth + ResearchTree.Trees.Count;

            float width = ( maxDepth + 1 ) * ( Settings.Button.x + Settings.Margin.x ); // zero based
            float height = totalWidth * ( Settings.Button.y + Settings.Margin.y );

            // main view rect
            Rect view = new Rect( 0f, 0f, width, height );
            Widgets.BeginScrollView( canvas, ref _scrollPosition, view );
            GUI.BeginGroup( view );

            Text.Anchor = TextAnchor.MiddleCenter;

            foreach ( Tree tree in ResearchTree.Trees )
            {
                foreach ( Node node in tree.Trunk.Concat( tree.Leaves ) )
                {
                    node.Draw();

                    foreach ( Node parent in node.Parents )
                    {
                        ResearchTree.DrawLine( node.Left, parent.Right, parent.Research.IsFinished ? Color.white : Color.grey, 1 );
                    }
                }
            }

            Queue.DrawLabels();

            Text.Anchor = TextAnchor.UpperLeft;

            GUI.EndGroup();
            Widgets.EndScrollView();
        }
    }
}