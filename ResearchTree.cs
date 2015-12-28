// ResearchTree/ResearchTree.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-12-21 13:45

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityCoreLibrary.ColorPicker;
using UnityEngine;
using Verse;

namespace FluffyResearchTree
{
    public class ResearchTree
    {
        public static List<Node> Forest;
        public static List<Tree> Trees;
        public static List<Node> Orphans;
        public static IntVec2 OrphanDepths;
        public static int OrphanWidth;
        public static Texture2D Button = ContentFinder<Texture2D>.Get( "button" );
        public static Texture2D ButtonActive = ContentFinder<Texture2D>.Get( "button-active" );
        public static Texture2D Circle3 = ContentFinder<Texture2D>.Get( "circle3" );
        public static Texture2D Circle = ContentFinder<Texture2D>.Get( "circle" );
        public static bool Initialized;
        public const int MinTrunkSize = 2;

        public static void DrawLine( Vector2 right, Vector2 left, Color color, int width )
        {
            GUI.color = color;
            bool flag = Math.Abs(left.y - right.y) < 0.1f;
            if( flag )
            {
                Widgets.DrawLine( left, right, color, width );
            }
            else
            {
                Widgets.DrawLine( left, new Vector2( left.x + Settings.Margin.x / 4f + 0.5f, left.y ), color, width );
                float y = Math.Min(left.y, right.y) + Settings.Margin.x / 4f;
                float y2 = Math.Max(left.y, right.y) - Settings.Margin.x / 4f;
                Widgets.DrawLine( new Vector2( left.x + Settings.Margin.x / 2f, y ), new Vector2( left.x + Settings.Margin.x / 2f, y2 ), color, width );
                Widgets.DrawLine( new Vector2( right.x - Settings.Margin.x / 4f - 0.5f, right.y ), right, color, width );
                Rect position = new Rect(left.x + Settings.Margin.x / 4f, left.y - Settings.Margin.x / 4f, Settings.Margin.x / 2f, Settings.Margin.x / 2f);
                Rect position2 = new Rect(right.x - Settings.Margin.x * 3f / 4f, right.y - Settings.Margin.x / 4f, Settings.Margin.x / 2f, Settings.Margin.x / 2f);
                Texture2D image = (width == 3) ? Circle3 : Circle;
                bool flag2 = left.y < right.y;
                if( flag2 )
                {
                    GUI.DrawTextureWithTexCoords( position, image, new Rect( 0.5f, 0.5f, 0.5f, 0.5f ) );
                    GUI.DrawTextureWithTexCoords( position2, image, new Rect( 0f, 0f, 0.5f, 0.5f ) );
                }
                else
                {
                    GUI.DrawTextureWithTexCoords( position, image, new Rect( 0.5f, 0f, 0.5f, 0.5f ) );
                    GUI.DrawTextureWithTexCoords( position2, image, new Rect( 0f, 0.5f, 0.5f, 0.5f ) );
                }
                GUI.color = Color.white;
            }
        }

        public static void Initialize()
        {
            // populate all nodes
            Forest = new List<Node>( DefDatabase<ResearchProjectDef>.AllDefsListForReading.ConvertAll( def => new Node( def ) ) );

            // create links between nodes
            foreach ( Node node in Forest )
            {
                node.CreateLinks();
            }

            // calculate Depth of each node
            foreach ( Node node in Forest )
            {
                node.SetDepth();
            }
            
            // get the main 'Trees', looping over all defs, find strings of Research named similarly.
            // We're aiming for finding things like Construction I/II/III/IV/V here.
            Dictionary<string, List<Node>> trunks = new Dictionary<string, List<Node>>();
            List<Node> orphans = new List<Node>(); // temp
            Orphans = new List<Node>();
            foreach ( Node node in Forest )
            {
                // try to remove the amount of random hits by requiring Trees to be directly linked.
                if( node.Parents.Any( parent => parent.Genus == node.Genus ) ||
                     node.Children.Any( child => child.Genus == node.Genus ) )
                {
                    if ( !trunks.ContainsKey( node.Genus ) )
                    {
                        trunks.Add( node.Genus, new List<Node>() );
                    }
                    trunks[node.Genus].Add( node );
                }
                else
                {
                    orphans.Add( node );
                }
            }

            // Assign the working dictionary to Tree objects, culling stumps.
            Trees = trunks.Where( trunk => trunk.Value.Count >= MinTrunkSize )
                            .Select( trunk => new Tree( trunk.Key, trunk.Value ) )
                            .ToList();

            // add too small Trees back into orphan list
            orphans.AddRange( trunks.Where( trunk => trunk.Value.Count < MinTrunkSize ).SelectMany( trunk => trunk.Value ) );

            // The order in which Trees should appear; ideally we want Trees with lots of cross-references to appear together.
            OrderTrunks();
            
            // Attach orphan nodes to the nearest Trunk.
            foreach ( Node orphan in orphans )
            {
                Tree closest = orphan.ClosestTree();
                if ( closest != null )
                {
                    closest.AddLeaf( orphan );
                }
                else
                {
                    Orphans.Add( orphan );
                }
            }

            // set some info about Orphans that we need for plotting
            OrphanDepths = new IntVec2( Orphans.Min( node => node.Depth ), Orphans.Max( node => node.Depth ) );
            OrphanWidth = ( from node in Orphans
                            group node by node.Depth into nodes
                            orderby nodes.Count() descending
                            select nodes.Count() ).First();

            // update nodes with position info
            FixPositions();

            // Assign colors to trunks
            int n = Trees.Count;
            for ( int i = 1; i <= Trees.Count; i++ )
            {
                Trees[i - 1].Color = ColorHelper.HSVtoRGB( (float)i / n, 1, 1);
            }
        }

        private static void OrderTrunks()
        {
            // if two or less Trees, optimization is pointless
            if ( Trees.Count < 3 ) return;

            // This is a form of the travelling salesman problem, but let's simplify immensely by taking a nearest-neighbour approach.
            List<Tree> trees = Trees.OrderBy( trunk => trunk.MinDepth ).ToList();
            Trees.Clear();

            // initialize list of Trees with the shallowest Trunk
            Tree first = trees.First();
            Trees.Add( first );
            trees.Remove( first );

            // add other Trees
            while ( trees.Count > 0 )
            {
                Tree next = trees.OrderByDescending( tree => tree.AffinityWith( Trees.Last() ) ).First();

                Trees.Add( next );
                trees.Remove( next );
            }
        }

        public static void FixPositions()
        {
            int curY = 0;

            foreach( Tree tree in Trees )
            {
                tree.StartY = curY;
                
                foreach( Node node in tree.Trunk )
                {
                    node.Pos = new IntVec2( node.Depth, curY );
                }

                // TODO: Better positioning.
                for ( int x = tree.MinDepth; x <= tree.MaxDepth; x++ )
                {
                    List<Node> nodes = tree.NodesAtDepth( x );
                    for ( int y = 0; y < nodes.Count; y++ )
                    {
                        // update Width to take account of trunks with 'gaps'
                        // also assumes the Trunk is at most 1 node wide - this might bite us in the arse.
                        if ( y + 2 > tree.Width ) tree.Width = y + 2;
                        nodes[y].Pos = new IntVec2( nodes[y].Depth, curY + y + 1 );
                    }
                }

                curY += tree.Width;
            }
        }
    }
}