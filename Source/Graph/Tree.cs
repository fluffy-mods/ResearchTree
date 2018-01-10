// Karel Kroeze
// Tree.cs
// 2017-01-06

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyResearchTree
{
    public static class Tree
    {
        public static bool Initialized;
        public static IntVec2 Size = IntVec2.Zero;
        private static List<Node> _nodes = new List<Node>();
        private static List<Edge<Node,Node>> _edges = new List<Edge<Node, Node>>();

        public static List<Node> Nodes
        {
            get
            {
                if (_nodes == null)
                    throw new Exception("Trying to access nodes before they are initialized.");

                return _nodes;
            }
        }

        public static List<Edge<Node,Node>> Edges
        {
            get
            {
                if (_edges == null)
                    throw new Exception("Trying to access edges before they are initialized.");

                return _edges;
            }
        }

        public static void Initialize()
        {
            PopulateNodes();
            CheckPrerequisites();
            CreateEdges();
            HorizontalPositions();
            NormalizeEdges();

            MinimizeCrossings();

            // Done!
            Initialized = true;
        }

        public static void HorizontalPositions()
        {
            // get list of techlevels
            var techlevels = Enum.GetValues( typeof( TechLevel ) ).Cast<TechLevel>();
            bool anyChange;
            var iteration = 1;
            var maxIterations = 50;

            do
            {
                Log.Debug( "Assigning horizontal positions, iteration {0}", iteration );
                var min = 1;
                anyChange = false;

                foreach ( var techlevel in techlevels )
                {
                    // enforce minimum x position based on techlevels
                    var nodes = Nodes.OfType<ResearchNode>().Where( n => n.Research.techLevel == techlevel );
                    if ( !nodes.Any() )
                        continue;

                    foreach ( var node in nodes )
                        anyChange = node.SetDepth(min) || anyChange;

                    min = nodes.Max( n => n.X ) + 1;

                    Log.Debug( "\t{0}, change: {1}", techlevel, anyChange );
                }
            } while ( anyChange && iteration++ < maxIterations );
        }

        private static void NormalizeEdges()
        {
            Log.Debug( "Normalizing edges."  );
            foreach ( var edge in new List<Edge<Node, Node>>( Edges.Where( e => e.Span > 1 ) ) )
            {
                Log.Debug( "\tCreating dummy chain for {0}", edge );

                // remove and decouple long edge
                Edges.Remove( edge );
                edge.In.OutEdges.Remove( edge );
                edge.Out.InEdges.Remove( edge );
                var cur = edge.In;
                var yOffset = ( edge.Out.Yf - edge.In.Yf ) / edge.Span;

                // create and hook up dummy chain
                for ( int x = edge.In.X + 1; x < edge.Out.X; x++ )
                {
                    var dummy = new DummyNode();
                    dummy.X = x;
                    dummy.Yf = edge.In.Yf + yOffset * ( x - edge.In.X );
                    var dummyEdge = new Edge<Node, Node>( cur, dummy );
                    cur.OutEdges.Add( dummyEdge );
                    dummy.InEdges.Add( dummyEdge );
                    _nodes.Add( dummy );
                    Edges.Add( dummyEdge );
                    cur = dummy;
                    Log.Debug( "\t\tCreated dummy {0}", dummy );
                }

                // hook up final dummy to out node
                var finalEdge = new Edge<Node, Node>( cur, edge.Out );
                cur.OutEdges.Add( finalEdge );
                edge.Out.InEdges.Add( finalEdge );
                Edges.Add( finalEdge );
            }
        }

        private static void CreateEdges()
        {
            Log.Debug( "Creating edges."  );
            // create links between nodes
            foreach ( ResearchNode node in Nodes.OfType<ResearchNode>() )
            {
                if ( node.Research.prerequisites.NullOrEmpty() )
                    continue;
                foreach ( var prerequisite in node.Research.prerequisites )
                {
                    ResearchNode prerequisiteNode = prerequisite;
                    var edge = new Edge<Node, Node>( prerequisiteNode, node );
                    Edges.Add( edge );
                    node.InEdges.Add( edge );
                    prerequisiteNode.OutEdges.Add( edge );
                    Log.Debug( "\tCreated edge {0}", edge );
                }
            }
        }

        private static void CheckPrerequisites()
        {
// check prerequisites
            Log.Debug( "Checking prerequisites."  );
            foreach ( ResearchNode node in Nodes.OfType<ResearchNode>() )
            {
                if ( !node.Research.prerequisites.NullOrEmpty() )
                {
                    if ( node.Research.prerequisites.NullOrEmpty() )
                        continue;

                    // warn about badly configured techlevels
                    if ( node.Research.prerequisites.Any( r => r.techLevel > node.Research.techLevel ) )
                        Log.Warning( "\t{0} has a lower techlevel than (one of) it's dependenc(y/ies)", node.Research.defName );

                    // get (redundant) ancestors.
                    var ancestors = node.Research.prerequisites?.SelectMany( r => r.GetPrerequisitesRecursive() ).ToList();
                    var redundant = ancestors.Intersect( node.Research.prerequisites );
                    if ( redundant.Any() )
                    {
                        Log.Warning( "\tredundant prerequisites for {0}: {1}", node.Research.LabelCap, string.Join( ", ", redundant.Select( r => r.LabelCap ).ToArray() ) );
                        foreach ( var redundantPrerequisite in redundant )
                            node.Research.prerequisites.Remove( redundantPrerequisite );
                    }
                }
            }
        }

        private static void PopulateNodes()
        {
            Log.Debug( "Populating nodes." );
            // populate all nodes
            _nodes = new List<Node>( DefDatabase<ResearchProjectDef>.AllDefsListForReading
                // exclude hidden projects (prereq of itself is a common trick to hide research).
                .Where( def => def.prerequisites.NullOrEmpty() || !def.prerequisites.Contains( def ) )
                .Select( def => new ResearchNode( def ) as Node ) );
            Log.Debug( "\t{0} nodes", _nodes.Count );
        }

        private static void Collapse()
        {
            Log.Debug( "Collapsing nodes." );
            var pre = Size;
            for ( int l = 1; l <= Size.x; l++ )
            {
                var nodes = Layer( l, true );
                int Y = 1;
                foreach ( var node in nodes )
                    node.Y = Y++;
            }
            Log.Debug("{0} -> {1}", pre, Size);
        }

        internal static void DrawDebug()
        {
            foreach ( Node v in Nodes )
            {
                foreach ( Node w in v.OutNodes )
                    Widgets.DrawLine( v.Center, w.Center, Color.white, 1 );
            }
        }

        internal static string DebugTip( Node v )
        {
            return "";
        }
        
        private static Node NodeAt( int X, int Y ) { return Nodes.FirstOrDefault( n => n.X == X && n.Y == Y ); }
        
        public static void MinimizeCrossings()
        {
            // initialize each layer by putting nodes with the most (recursive!) children on bottom
            for ( var X = 1; X <= Size.x; X++ )
            {
                List<Node> nodes = Layer( X ).OrderBy( n => n.Descendants.Count ).ToList();
                for ( var i = 0; i < nodes.Count; i++ )
                    nodes[i].Y = i + 1;
            }

            // up-down sweeps of median reordering
            // burnout; number of iterations without progress
            int iteration = 0, max_iterations = 50, burnout = 2;
            while ( burnout > 0 && iteration < max_iterations )
                if ( !BarymetricSweep( iteration++ ) )
                    burnout--;

            // collapse to make the graph more dense
            Collapse();

            // run two more barymetric sweeps to give a somewhat pleasing layout
            BarymetricSweep( iteration++ );
            BarymetricSweep( iteration );

            // perform sweeps of adjacent node reorderings
            iteration = 0;
            burnout = 2;
            while ( burnout > 0 && iteration < max_iterations )
                if ( !GreedySweep( iteration++ ) )
                    burnout--;
        }

        private static bool GreedySweep( int iteration )
        {
            // count number of crossings before sweep
            int before = Crossings();

            // do up/down sweep on aternating iterations
            if ( iteration % 2 == 0 )
                for ( var l = 1; l <= Size.x; l++ )
                    GreedySweep_Layer( l );
            else
                for ( int l = Size.x; l >= 1; l-- )
                    GreedySweep_Layer( l );

            // count number of crossings after sweep
            int after = Crossings();

#if DEBUG
            Log.Message( $"GreedySweep: {before} -> {after}" );
#endif

            // return progress
            return after < before;
        }

        private static void GreedySweep_Layer( int l )
        {
            // The objective here is twofold;
            // 1: Swap nodes to reduce the number of crossings
            // 2: Swap nodes so that inner edges (edges between dummies)
            //    avoid crossings at all costs.
            //
            // If I'm reasoning this out right, both objectives should be served by
            // minimizing the amount of crossings between each pair of nodes.
            List<Node> layer = Layer( l, true );
            for ( var i = 0; i < layer.Count - 1; i++ )
                if ( Crossings( layer[i + 1], layer[i] ) < Crossings( layer[i], layer[i + 1] ) )
                    Swap( layer[i], layer[i + 1] );
        }

        private static void Swap( Node A, Node B )
        {
            if ( A.X != B.X )
                throw new Exception( "Can't swap nodes on different layers" );

            // swap Y positions of adjacent nodes
            int tmp = A.Y;
            A.Y = B.Y;
            B.Y = tmp;
        }

        private static string DebugGrid()
        {
            var grid = "";
            for ( int y = 1; y <= Size.z; y++ )
            {
                for ( int x = 1; x <= Size.x; x++ )
                {
                    var node = NodeAt( x, y );
                    grid += node != null ? "+" : ".";
                }
                grid += "\n";
            }
            return grid;
        }

        private static bool BarymetricSweep( int iteration )
        {
            // count number of crossings before sweep
            int before = Crossings();

            // do up/down sweep on alternating iterations
            if ( iteration % 2 == 0 )
                for ( var i = 2; i <= Size.x; i++ )
                    BarymetricSweep_Layer( i, true );
            else
                for ( int i = Size.x - 1; i > 0; i-- )
                    BarymetricSweep_Layer( i, false );

            // count number of crossings after sweep
            int after = Crossings();

            // did we make progress? please?
            Log.Debug( $"BarymetricSweep {iteration} ({( iteration % 2 == 0 ? "up" : "down" )}): {before} -> {after}" );
            return after < before;
        }

        private static void BarymetricSweep_Layer( int layer, bool up )
        {
            var means = Layer( layer )
                .ToDictionary( n => n, n => GetBarycentre( n, up ? n.InNodes : n.OutNodes ) )
                .OrderBy( n => n.Value );

            // create groups of nodes at similar means
            var cur = float.MinValue;
            Dictionary<float,List<Node>> groups = new Dictionary<float, List<Node>>(); 
            foreach ( var mean in means )
            {
                if ( Math.Abs( mean.Value - cur ) > Constants.Epsilon )
                {
                    cur = mean.Value;
                    groups[cur] = new List<Node>();
                }
                groups[cur].Add( mean.Key );
            }

            // position nodes as close to their desired mean as possible
            var Y = 1f;
            foreach ( var group in groups )
            {
                var mean = group.Key;
                var N = group.Value.Sum( n => n is ResearchNode ? 1 : .5f );
                Y = Mathf.Max( Y, mean - ( N - 1 ) / 2 );

                foreach ( var node in group.Value )
                {
                    node.Yf = Y;
                    Y += node is ResearchNode ? 1f : .5f;
                }
            }
        }

        private static float GetBarycentre( Node node, List<Node> neighbours )
        {
            if ( neighbours.NullOrEmpty() )
                return node.Yf;

            return neighbours.Sum( n => n.Yf ) / neighbours.Count;
        }
        
        private static int UpperCrossings( Node a, Node b )
        {
            if ( a.X != b.X )
                throw new Exception( "a and b must be on the same rank." );

            IEnumerable<int> A = a.InNodes?.Select( n => n.Y );
            IEnumerable<int> B = b.InNodes?.Select( n => n.Y );

            return Crossings( A, B );
        }

        /// <summary>
        /// Return the total amount of crossings of edges between A and B, assuming that A and B are
        /// Y coordinates of vertices adjacent to A and B, and that Y(A) \lt Y(B);
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        private static int Crossings( IEnumerable<int> A, IEnumerable<int> B )
        {
#if TRACE_CROSSINGS
            Log.Message( $"Crossings( A, B ) called" );

            if ( A == null )
                Log.Message( "A NULL" );
            else
                Log.Message( "A: " + string.Join( ", ", A.Select( i => i.ToString() ).ToArray() ) );

            if ( B == null )
                Log.Message( "B NULL" );
            else
                Log.Message( "B: " + string.Join( ", ", B.Select( i => i.ToString() ).ToArray() ) );
#endif

            if ( A == null || B == null )
                return 0;

            if ( !A.Any() || !B.Any() )
                return 0;

            var crossings = 0;
            foreach ( int a in A )
                foreach ( int b in B )
                    if ( a > b )
                        crossings++;

#if TRACE_CROSSINGS
            Log.Message( "\tCrossings: " + crossings  );
#endif

            return crossings;
        }

        private static int LowerCrossings( Node a, Node b )
        {
            if ( a.X != b.X )
                throw new Exception( "a and b must be on the same rank." );

            IEnumerable<int> A = a.OutNodes?.Select( n => n.Y );
            IEnumerable<int> B = b.OutNodes?.Select( n => n.Y );

            return Crossings( A, B );
        }

        private static int Crossings()
        {
#if TRACE_CROSSINGS
            Log.Message( "Crossings() called, tree size: " + Size );
#endif
            var crossings = 0;
            for ( var i = 1; i < Size.x; i++ )
                crossings += Crossings( i, false );

            return crossings;
        }

        private static int Crossings( Node A, Node B ) { return UpperCrossings( A, B ) + LowerCrossings( A, B ); }

        private static int Crossings( int depth )
        {
            return Crossings( depth, true ) + Crossings( depth, false );
        }

        private static int Crossings( int depth, bool up )
        {
#if TRACE_CROSSINGS
            Log.Message( $"Crossings( {depth}, {up} ) called, nodes at {depth}: {NodesAtDepth(depth).Count}" );
#endif
            if ( up && depth - 1 < 0 )
                throw new ArgumentOutOfRangeException( nameof( depth ) );
            if ( !up && depth + 1 > Size.x )
                throw new ArgumentOutOfRangeException( nameof( depth ) );

            var crossings = 0;
            List<Node> nodes = Layer( depth, true );
            for ( var i = 0; i < nodes.Count - 1; i++ )
                crossings += up ? UpperCrossings( nodes[i], nodes[i + 1] ) : LowerCrossings( nodes[i], nodes[i + 1] );

            return crossings;
        }

        public static bool OrderDirty;
        public static List<Node> Layer( int depth, bool ordered = false )
        {
            if ( ordered && OrderDirty )
            {
                _nodes = Nodes.OrderBy( n => n.X ).ThenBy( n => n.Y ).ToList();
                OrderDirty = false;
            }

            return Nodes.Where( n => n.X == depth ).ToList();
        }

        public new static string ToString()
        {
            var text = new StringBuilder();

            for ( var l = 1; l <= Nodes.Max( n => n.X ); l++ )
            {
                text.AppendLine( $"Layer {l}:" );
                List<Node> layer = Layer( l, true );

                foreach ( Node n in layer )
                {
                    text.AppendLine( $"\t{n}" );
                    text.AppendLine( $"\t\tAbove: " + string.Join( ", ", n.InNodes.Select( a => a.ToString() ).ToArray() ) );
                    text.AppendLine( $"\t\tBelow: " + string.Join( ", ", n.OutNodes.Select( b => b.ToString() ).ToArray() ) );
                }
            }

            return text.ToString();
        }
    }
}
