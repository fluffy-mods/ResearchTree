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
using UnityEngine;
using Verse;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
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
            LongEventHandler.QueueLongEvent( CheckPrerequisites, "Fluffy.ResearchTree.PreparingTree.Setup", false,
                                             null );
            LongEventHandler.QueueLongEvent( CreateGraph, "Blub!", false, null );

            // done!
            LongEventHandler.QueueLongEvent( () => { Initialized = true; }, "Fluffy.ResearchTree.PreparingTree.Layout",
                                             false, null );

            // tell research tab we're ready
            LongEventHandler.QueueLongEvent( MainTabWindow_ResearchTree.Instance.Notify_TreeInitialized,
                                             "Fluffy.ResearchTree.RestoreQueue", false, null );
        }

        public static void CreateGraph()
        {
            // create graph
            var graph = new GeometryGraph();
            foreach ( var node in Nodes )
                graph.Nodes.Add( node );
//            
//            // create clusters
//            foreach ( var level in RelevantTechLevels )
//                graph.Nodes.Add( new Cluster( Nodes.Where( n => n.Research.techLevel == level ) ) );
            
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
                        Log.Debug( "blub" );
                        Log.Error( $"{parent?.LabelCap.Resolve() ?? "NULL"} unknown", false );
                        continue;
                    }

                    var edge = new Edge<ResearchNode, ResearchNode>( parent.ResearchNode(), node );
                    graph.Edges.Add( edge );
                    Edges.Add( edge );
                }
            }

            // set up layout settings
            var settings = new SugiyamaLayoutSettings
            {
                NodeSeparation  = Constants.NodeMargins.z,
                LayerSeparation = Constants.NodeMargins.x,
                GridSizeByX = Constants.NodeMargins.z,
                GridSizeByY = Constants.NodeMargins.x,
//                SnapToGridByY = SnapToGridByY.Top
            };

            // add constraints based on tech level.
//            var previousTech = Nodes.Where( n => n.Research.techLevel == RelevantTechLevels[0] );
//            for ( int i = 1; i < RelevantTechLevels.Count; i++ )
//            {
//                var currentTech = Nodes.Where( n => n.Research.techLevel == RelevantTechLevels[i] );
//                foreach( var previous in previousTech )
//                    foreach ( var current in currentTech )
//                        settings.AddUpDownConstraint( previous, current );
//                previousTech = currentTech;
//            }
            // add layers based on techlevel
//            foreach ( var level in RelevantTechLevels )
//                settings.PinNodesToSameLayer( Nodes.Where( n => n.Research.techLevel == level ).ToArray() );

            // do the layout
            // LayoutHelpers.CalculateLayout( graph, settings, null );

            var layout = new LayeredLayout( graph, settings );
            layout.Run();
            
            // rotate top-down to left-right, hopefully
            graph.Transform( PlaneTransformation.Rotation( Math.PI / 2.0 ) );

            // BoundingBox has origin at bottom left
            // theoretically, we should be able to use a transform to flip the y axis, but it really doesn't matter, we'll just use bottom as the origin.
            // rescale bottom left to 0, 0
            graph.Translate( -graph.BoundingBox.LeftBottom ); // + new Point( Constants.NodeSize.x, Constants.NodeSize.z ) );

            // set tree rect and node pos
            // TODO: just make rect and pos a wrapper for the BoundingBox
            Rect = new Rect( 0, 0, (int)graph.BoundingBox.Width, (int)graph.BoundingBox.Height );
            foreach ( var node in Nodes )
                node.Pos = new IntVec2( (int)node.BoundingBox.Left, (int)node.BoundingBox.Bottom );
            
            var debug = new StringBuilder();
            debug.AppendLine( Rect.ToString() );
            foreach ( var node in Nodes )
                debug.AppendLine( $"{node.Research.LabelCap.Resolve()} :: {node.Pos}" );

            foreach ( var edge in Edges )
            {
                debug.AppendLine( $"{( (ResearchNode) edge.Source ).Research.LabelCap.Resolve()} => " +
                                  $"{( (ResearchNode) edge.Target ).Research.LabelCap.Resolve()}" );
                if ( edge.Curve is Curve curve )
                    foreach ( var segment in curve.Segments )
                        debug.AppendLine( "\t" + segment.BoundingBox.Center.ToString() );
                else
                    debug.AppendLine( "\t" + edge.BoundingBox.Center.ToString() );
            }

            Log.Debug( debug.ToString() );
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