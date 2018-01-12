// ResearchTree/LogHeadDB.cs
//
// Copyright Karel Kroeze, 2015.
//
// Created 2015-12-21 13:30

using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static FluffyResearchTree.Constants;

namespace FluffyResearchTree
{
    public class MainTabWindow_ResearchTree : MainTabWindow
    {
        internal static Vector2 _scrollPosition = Vector2.zero;

        public override void PreOpen()
        {
            base.PreOpen();

            if ( !Tree.Initialized )
                // initialize tree
                Tree.Initialize();

            // clear node availability caches
            ResearchNode.ClearCaches();

            // set to topleft (for some reason vanilla alignment overlaps bottom buttons).
            windowRect.x = 0f;
            windowRect.y = 0f;
            windowRect.width = UI.screenWidth;
            windowRect.height = UI.screenHeight - 35f;
        }

        private static Rect _viewRect;
        private static Rect _treeRect;

        public override void DoWindowContents( Rect canvas )
        {
            if ( !Tree.Initialized )
                return;

            // size of tree
            float width = Tree.Size.x * ( NodeSize.x + NodeMargins.x );
            float height = Tree.Size.z * ( NodeSize.y + NodeMargins.y );
            _treeRect = new Rect( 0f, 0f, width, height );

            // layout
            var topRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                canvas.width,
                TopBarHeight );
            _viewRect = canvas;
            _viewRect.yMin += TopBarHeight + Margin;

            GUI.DrawTexture( _viewRect, Assets.SlightlyDarkBackground );
            _viewRect = _viewRect.ContractedBy( Constants.Margin );

            // visible area of _treeRect
            var visibleRect = new Rect(
                _scrollPosition.x,
                _scrollPosition.y,
                _viewRect.width,
                _viewRect.height );

            DrawTopBar( topRect );

            Widgets.BeginScrollView( _viewRect, ref _scrollPosition, _treeRect );
            GUI.BeginGroup( _treeRect );

            Tree.Draw( visibleRect );
            Queue.DrawLabels( visibleRect );

            GUI.EndGroup();
            Widgets.EndScrollView();

            // cleanup;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawTopBar( Rect canvas )
        {
            var searchRect = canvas;
            var queueRect = canvas;
            searchRect.width = 200f;
            queueRect.xMin += 200f + Constants.Margin;

            GUI.DrawTexture( searchRect, Assets.SlightlyDarkBackground );
            GUI.DrawTexture( queueRect, Assets.SlightlyDarkBackground );

            DrawSearchBar( searchRect.ContractedBy( Constants.Margin ) );
            Queue.DrawQueue( queueRect.ContractedBy( Constants.Margin ) );
        }

        private string _query = "";

        private void DrawSearchBar( Rect canvas )
        {
            Profiler.Start( "DrawSearchBar" );
            var iconRect = new Rect(
                    canvas.xMax - Constants.Margin - 16f,
                    0f,
                    16f,
                    16f )
                .CenteredOnYIn( canvas );
            var searchRect = new Rect(
                    canvas.xMin,
                    0f,
                    canvas.width,
                    30f )
                .CenteredOnYIn( canvas );

            GUI.DrawTexture( iconRect, Assets.Search );
            var query = Widgets.TextField( searchRect, _query );

            if ( query != _query )
            {
                _query = query;
                Find.WindowStack.FloatMenu?.Close( false );

                if ( query.Length > 2 )
                {
                    // open float menu with search results, if any.
                    var options = new List<FloatMenuOption>();

                    foreach ( var result in Tree.Nodes.OfType<ResearchNode>()
                        .Select( n => new { node = n, match = n.Matches( query ) } )
                        .Where( result => result.match > 0 )
                        .OrderBy( result => result.match ) )
                    {
                        options.Add( new FloatMenuOption( result.node.Label, () => CenterOn( result.node ),
                            MenuOptionPriority.Default, () => CenterOn( result.node ) ) );
                    }

                    if ( !options.Any() )
                        options.Add( new FloatMenuOption( "Fluffy.ResearchTree.NoResearchFound".Translate(), null ) );

                    Find.WindowStack.Add( new FloatMenu_Fixed( options,
                        UI.GUIToScreenPoint( new Vector2( searchRect.xMin, searchRect.yMax ) ) ) );
                }
            }
            Profiler.End();
        }

        public static void CenterOn( Node node )
        {
            var position = new Vector2(
                ( NodeSize.x + NodeMargins.x ) * ( node.X - .5f ),
                ( NodeSize.y + NodeMargins.y ) * ( node.Y - .5f ) );

            node.Highlighted = true;

            position -= new Vector2( UI.screenWidth, UI.screenHeight ) / 2f;

            position.x = Mathf.Clamp( position.x, 0f, _treeRect.width - _viewRect.width );
            position.y = Mathf.Clamp( position.y, 0f, _treeRect.height - _viewRect.height );
            _scrollPosition = position;
        }
    }
}
