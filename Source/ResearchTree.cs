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

            // if left and right are on the same level, just draw a straight line.
            if( Math.Abs( left.y - right.y ) < 0.1f )
            {
                Widgets.DrawLine( left, right, color, width );
            }

            // draw three line pieces and two curves.
            else
            {
                // left to curve
                Widgets.DrawLine( left, new Vector2( left.x + Settings.Margin.x / 4f + 0.5f, left.y ), color, width );

                // determine top and bottom y positions
                float top = Math.Min(left.y, right.y) + Settings.Margin.x / 4f;
                float bottom = Math.Max(left.y, right.y) - Settings.Margin.x / 4f;

                // curve to curve
                Widgets.DrawLine( new Vector2( left.x + Settings.Margin.x / 2f, top ), new Vector2( left.x + Settings.Margin.x / 2f, bottom ), color, width );

                // curve to right
                Widgets.DrawLine( new Vector2( right.x - Settings.Margin.x / 4f - 0.5f, right.y ), right, color, width );

                // curve positions
                Rect curveLeft = new Rect(left.x + Settings.Margin.x / 4f, left.y - Settings.Margin.x / 4f, Settings.Margin.x / 2f, Settings.Margin.x / 2f);
                Rect curveRight = new Rect(right.x - Settings.Margin.x * 3f / 4f, right.y - Settings.Margin.x / 4f, Settings.Margin.x / 2f, Settings.Margin.x / 2f);

                // curve texture
                Texture2D image = (width == 3) ? Circle3 : Circle;

                // going down
                if( left.y < right.y )
                {
                    GUI.DrawTextureWithTexCoords( curveLeft, image, new Rect( 0.5f, 0.5f, 0.5f, 0.5f ) ); // bottom right quadrant
                    GUI.DrawTextureWithTexCoords( curveRight, image, new Rect( 0f, 0f, 0.5f, 0.5f ) ); // top left quadrant
                }
                // going up
                else
                {
                    GUI.DrawTextureWithTexCoords( curveLeft, image, new Rect( 0.5f, 0f, 0.5f, 0.5f ) ); // top right quadrant
                    GUI.DrawTextureWithTexCoords( curveRight, image, new Rect( 0f, 0.5f, 0.5f, 0.5f ) ); // bottom left quadrant
                }

                // reset color
                GUI.color = Color.white;
            }
        }

        public static void Initialize()
        {
            // populate all nodes
            Forest = new List<Node>( DefDatabase<ResearchProjectDef>.AllDefsListForReading
                                        // exclude hidden projects (prereq of itself is a common trick to hide research).
                                        .Where( def => !def.prerequisites.Contains( def ) )
                                        .Select( def => new Node( def ) ) );

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
            
            // Attach orphan nodes to the nearest Trunk, or the orphanage trunk
            Tree Orphanage = new Tree( "orphans", new List<Node>() );
            foreach ( Node orphan in orphans )
            {
                Tree closest = orphan.ClosestTree() ?? Orphanage;
                closest.AddLeaf( orphan );
            }

            // Assign colors to trunks
            int n = Trees.Count;
            for( int i = 1; i <= Trees.Count; i++ )
            {
                Trees[i - 1].Color = ColorHelper.HSVtoRGB( (float)i / n, 1, 1 );
            }

            // add orphanage tree, and color it grey.
            Trees.Add( Orphanage );
            Orphanage.Color = Color.grey;

            // update nodes with position info
            FixPositions();

            // Done!
            Initialized = true;
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

                // position child nodes as close to their parents as possible
                for ( int x = tree.MinDepth; x <= tree.MaxDepth; x++ )
                {
                    // put nodes that are children of the trunk first.
                    List<Node> nodes = tree.NodesAtDepth( x ).OrderBy( node => node.Parents.Any( parent => node.Tree.Trunk.Contains( parent )) ? 0 : 1 ).ToList();

                    foreach ( Node node in nodes )
                    {
                        // try find the closest matching position
                        int bestPos = curY + 1;
                        if (node.Parents.Any())
                            bestPos = node.Parents.Select( parent => parent.Pos.z ).Min();

                        // bump down if taken or trunk level
                        while ( nodes.Any( n => n.Pos.z == bestPos ) || bestPos == curY )
                            bestPos++;

                        // extend tree width if necessary
                        tree.Width = Math.Max( tree.Width, bestPos - curY + 1 );

                        // set position
                        node.Pos = new IntVec2( node.Depth, bestPos );
                    }
                }

                // sort all nodes by their depths, then by their z position.
                tree.Leaves = tree.Leaves.OrderBy( node => node.Depth ).ThenBy( node => node.Pos.z ).ToList();

                // do a reverse pass to position parent nodes next to their children
                for ( int x = tree.MaxDepth; x >= tree.MinDepth; x-- )
                {
                    List<Node> nodes = tree.NodesAtDepth( x );
                    foreach ( Node node in nodes )
                    {
                        Log.Message( "Try: " + node.Research.LabelCap );
                        // if this node has children;
                        if ( node.Children.Count > 0 )
                        {
                            Log.Message( "Do: " + node.Research.LabelCap );
                            // ideal position would be right next to top child
                            Node topChild = node.Children.OrderBy( child => child.Pos.z ).First();
                            int bestPos = topChild.Pos.z;

                            // keep checking until we have a decent position
                            // if that is indeed the current position, great, move to next
                            if ( bestPos == node.Pos.z )
                                continue;

                            // we're only checking leaves - don't allow them to be on trunk positions.
                            if ( bestPos == node.Tree.StartY )
                                bestPos++;
                           
                            // otherwise, check if position is taken (exclude this node itself from matches
                            while ( nodes.Any( n => n.Pos.z == bestPos && n != node ) )
                            {
                                Log.Message( "Pos: " + bestPos );
                                // does the node at that position have the same child?
                                Node otherNode = nodes.First(n => n.Pos.z == bestPos);
                                if ( !otherNode.Children.Contains( topChild ) )
                                {
                                    // if not, switch them around
                                    bestPos = otherNode.Pos.z;
                                    otherNode.Pos.z = node.Pos.z;
                                }
                                // or just bump it down otherwise
                                else
                                {
                                    bestPos++;
                                }
                            }

                            // we should now have a decent position
                            // extend tree width if necessary
                            tree.Width = Math.Max( tree.Width, bestPos - curY + 1 );

                            // set position
                            node.Pos = new IntVec2( node.Depth, bestPos );
                        }
                    }
                }
                

                curY += tree.Width;
            }
        }
    }
}