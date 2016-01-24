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
        private Vector2 _scrollPosition = Vector2.zero;
        private bool            _noBenchWarned;
        
        public override void PreOpen()
        {
            base.PreOpen();
            
            if ( !ResearchTree.Initialized )
            {
                // initialize tree
                ResearchTree.Initialize();

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
        }



        private void DrawTree( Rect canvas )
        {
            // get total size of Research Tree
            int maxDepth = ResearchTree.Trees.Max( tree => tree.MaxDepth );
            int totalWidth = ResearchTree.Trees.Sum( tree => tree.Width );

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