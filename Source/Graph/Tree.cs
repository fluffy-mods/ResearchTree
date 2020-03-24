// Tree.cs
// Copyright Karel Kroeze, 2020-2020

//using Multiplayer.API;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using UnityEngine;
using Verse;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Prototype.Ranking;
using RimWorld;
using RimWorld.QuestGen;
using Verse.Noise;
using Curve = Microsoft.Msagl.Core.Geometry.Curves.Curve;

namespace FluffyResearchTree
{
    public static class Tree
    {
        public static  bool                            Initialized;

        public static Rect Rect { get; private set; }
        private static List<ResearchNode> _nodes;
        private static List<Edge<ResearchNode, ResearchNode>> _edges;

        private static bool _initializing;

        public static List<ResearchNode> Nodes
        {
            get
            {
                if ( _nodes != null ) return _nodes;
                PopulateNodes();
                return _nodes;
            }
        }

        public static List<Edge<ResearchNode, ResearchNode>> Edges
        {
            get
            {
                if ( _edges != null ) return _edges;
                throw new Exception( "Trying to access edges before they are initialized." );
            }
        }

        public static List<TechLevel> RelevantTechLevels => DefDatabase<ResearchProjectDef>
                                                           .AllDefsListForReading.Select( r => r.techLevel )
                                                           .Distinct()
                                                           .OrderBy( r => (int) r )
                                                           .ToList();

//        [SyncMethod]
        public static void Initialize()
        {
            if ( Initialized ) return;

            // make sure we only have one initializer running
            if ( _initializing )
                return;
            _initializing = true;

            // setup
            LongEventHandler.QueueLongEvent( CheckPrerequisites, "Fluffy.ResearchTree.PreparingTree.Setup", false, null );
            LongEventHandler.QueueLongEvent( CreateLayers, "Fluffy.ResearchTree.PreparingTree.Layers", false, null );

            LongEventHandler.QueueLongEvent( CreateGraph, "Fluffy.ResearchTree.PreparingTree.Layout", false, null );

            // done!
            LongEventHandler.QueueLongEvent( () => { Initialized = true; }, "Fluffy.ResearchTree.PreparingTree.Layout",
                                             false, null );

            // tell research tab we're ready
            LongEventHandler.QueueLongEvent( MainTabWindow_ResearchTree.Instance.Notify_TreeInitialized,
                                             "Fluffy.ResearchTree.RestoreQueue", false, null );
        }

        public static void CreateLayers()
        {
            // get list of techlevels
            var  techlevels = RelevantTechLevels;
            bool anyChange;
            var  iteration     = 1;
            var  maxIterations = 250;

            // assign horizontal positions based on tech levels and prerequisites
            do
            {
                var min = 1;
                anyChange = false;

                // by batching nodes per techlevel, we can set the next techlevel to start _after_ the previous
                foreach ( var techlevel in techlevels )
                {
                    var nodes = Nodes.Where( n => n.Research.techLevel == techlevel );
                    if ( !nodes.Any() )
                        continue;

                    foreach ( var node in nodes )
                    {
                        var isRoot  = node.Parents.NullOrEmpty();
                        var desired = isRoot ? 1 : node.Parents.Max( n => n.Layer ) + 1;
                        var layer   = Mathf.Max( desired, min );
                        if ( node.Layer != layer )
                        {
                            node.Layer = layer;
                            anyChange = true;
                        }
                    }
                    min = nodes.Max( n => n.Layer ) + 1;
                }
            } while ( anyChange && iteration++ < maxIterations );


            for ( int l = 1; l <= Nodes.Max( n => n.Layer ); l++ )
            {
                Log.Debug( l.ToString() );
                foreach ( var node in Nodes.Where( n => n.Layer == l ) )
                    Log.Debug( "\t" + node.Research.LabelCap );
            }

            // TODO: flatten layers, max N nodes per layer (because MSAGL is inflexible).
        }

        public static void CreateGraph()
        {
            // create graph
            var graph = new GeometryGraph();
            foreach ( var node in Nodes )
                graph.Nodes.Add( node );
            
            // add nodes and edges
            _edges = new List<Edge<ResearchNode, ResearchNode>>();
            foreach ( var node in Nodes )
            {
                if ( node.Research.prerequisites.NullOrEmpty() )
                    continue;
                foreach ( var parent in node.Research.prerequisites )
                {
                    if ( parent?.ResearchNode() == null )
                    {
                        Log.Error( $"{parent?.LabelCap.Resolve() ?? "NULL"} unknown", false );
                        continue;
                    }

                    var edge = new Edge<ResearchNode, ResearchNode>( parent.ResearchNode(), node );
                    graph.Edges.Add( edge );
                    Edges.Add( edge );
                }
            }


//            // add dummy nodes for each techlevel in an attempt to at least roughly cluster tech levels together
//            var dummies = new List<Node>();
//            foreach ( var level in RelevantTechLevels )
//            {
//                var dummy = new Node( new IntVec2( 10, 10 ) );
//                graph.Nodes.Add( dummy );
//                dummies.Add( dummy );
//                foreach ( var node in Nodes.Where( n => n.Research.techLevel == level  ) )
//                    graph.Edges.Add( new Edge( dummy, node ) );
//            }

            var edgeSettings = new EdgeRoutingSettings();
            edgeSettings.EdgeRoutingMode = EdgeRoutingMode.SugiyamaSplines;

            // set up layout settings (note that we're going to rotate everything by 90 degrees!
            var sugiyamaSettings = new SugiyamaLayoutSettings
            {
                NodeSeparation  = Constants.NodeMargins.z,
                LayerSeparation = Constants.NodeMargins.x,
                GridSizeByX = Constants.NodeMargins.z,
                GridSizeByY = Constants.NodeMargins.x,
//                PackingMethod = PackingMethod.Columns,
                EdgeRoutingSettings = edgeSettings
            };
            
            // enforce layers
            for ( int l = 1; l <= Nodes.Max( n => n.Layer ); l++ )
                sugiyamaSettings.PinNodesToSameLayer( Nodes.Where( n => n.Layer == l ).ToArray() );

            // line up era dummies
//            sugiyamaSettings.AddUpDownVerticalConstraints( dummies.ToArray() );

            LayoutHelpers.CalculateLayout( graph, sugiyamaSettings, null );

            // rotate 90 degrees from top->bottom to left->right
            graph.Transform( PlaneTransformation.Rotation( Math.PI / 2.0 ) );
            // flip top-bottom
            graph.Transform( new PlaneTransformation( 1, 0, 0, 0, -1, 0 ) );
            // set bottom left to be 0,0 (note that we use this to be top left)
            graph.Translate( -graph.BoundingBox.LeftBottom ); // + new Point( Constants.NodeSize.x, Constants.NodeSize.z ) );

            // set tree rect and node pos
            Rect = new Rect( 0, 0, (int)graph.BoundingBox.Width + Constants.NodeSize.x, (int)graph.BoundingBox.Height + Constants.NodeSize.z );
            foreach ( var node in Nodes )
                node.Pos = new IntVec2( (int)node.BoundingBox.Left, (int)node.BoundingBox.Bottom );
        }


        private static void CheckPrerequisites()
        {
            // check prerequisites
            Log.Debug( "Checking prerequisites." );
            Profiler.Start();

            var nodes = new Queue<ResearchNode>( Nodes.OfType<ResearchNode>() );
            // remove redundant prerequisites
            while ( nodes.Count > 0 )
            {
                var node = nodes.Dequeue();
                if ( node.Research.prerequisites.NullOrEmpty() )
                    continue;

                var ancestors = node.Research.prerequisites?.SelectMany( r => r.Ancestors() ).ToList();
                var redundant = ancestors.Intersect( node.Research.prerequisites );
                if ( redundant.Any() )
                {
                    Log.Warning( "\tredundant prerequisites for {0}: {1}", node.Research.LabelCap,
                                 string.Join( ", ", redundant.Select( r => r.LabelCap ).ToArray() ) );
                    foreach ( var redundantPrerequisite in redundant )
                        node.Research.prerequisites.Remove( redundantPrerequisite );
                }
            }

            // fix bad techlevels
            nodes = new Queue<ResearchNode>( Nodes.OfType<ResearchNode>() );
            while ( nodes.Count > 0 )
            {
                var node = nodes.Dequeue();
                if ( !node.Research.prerequisites.NullOrEmpty() )
                    // warn and fix badly configured techlevels
                    if ( node.Research.prerequisites.Any( r => r.techLevel > node.Research.techLevel ) )
                    {
                        Log.Warning( "\t{0} has a lower techlevel than (one of) it's prerequisites",
                                     node.Research.defName );
                        node.Research.techLevel = node.Research.prerequisites.Max( r => r.techLevel );

                        // re-enqeue all descendants
                        foreach ( var descendant in node.Descendants.OfType<ResearchNode>() )
                            nodes.Enqueue( descendant );
                    }
            }

            Profiler.End();
        }

        private static void PopulateNodes()
        {
            Log.Debug( "Populating nodes." );
            Profiler.Start();

            var projects = DefDatabase<ResearchProjectDef>.AllDefsListForReading;

            // find hidden nodes (nodes that have themselves as a prerequisite)
            var hidden = projects.Where( p => p.prerequisites?.Contains( p ) ?? false );

            // find locked nodes (nodes that have a hidden node as a prerequisite)
            var locked = projects.Where( p => p.Ancestors().Intersect( hidden ).Any() );

            // populate all nodes
            _nodes = new List<ResearchNode>( DefDatabase<ResearchProjectDef>.AllDefsListForReading
                                                                    .Except( hidden )
                                                                    .Except( locked )
                                                                    .Select( def => new ResearchNode( def ) ) );
            Log.Debug( "\t{0} nodes", _nodes.Count );
            Profiler.End();
        }

        [Conditional( "DEBUG" )]
        internal static void DebugDraw()
        {
            foreach ( var v in Nodes )
            {
                foreach ( var w in v.OutEdges ) Widgets.DrawLine( v.Right, ( (ResearchNode) w.Target ).Left , Color.white, 1 );
            }
        }
        
        public static void Draw( Rect visibleRect )
        {
            foreach ( var edge in Edges.OrderBy( e => e.DrawOrder ) )
                edge.Draw( visibleRect );
            foreach ( var node in Nodes )
                node.Draw( visibleRect );
        }
    }
}