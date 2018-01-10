// Karel Kroeze
// Tree.cs
// 2017-01-06

#define TRACE_ALIGNMENT

//#define TRACE_COMPACTION
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using static FluffyResearchTree.Assets;

namespace FluffyResearchTree
{
    public static class Tree
    {
        public static bool Initialized;
        public static IntVec2 Size = IntVec2.Zero;
        public static List<Node> _nodes;

        // data structures used in vertical alignment and compaction
        private static HashSet<Pair<Node, Node>> marks;

        private static Dictionary<Node, Node> roots;
        private static Dictionary<Node, Node> align;
        private static Dictionary<Node, Node> sink;
        private static Dictionary<Node, int> shift;
        private static Dictionary<Node, bool> positioned;

        internal static bool OrderDirty = true;

        public static List<Node> Nodes
        {
            get
            {
                if ( _nodes == null )
                    throw new Exception( "Trying to access leaves before they are initialized." );

                return _nodes;
            }
        }

        public static void HorizontalPositions()
        {
            // get list of techlevels
            var techlevels = Enum.GetValues(typeof(TechLevel)).Cast<TechLevel>();
            bool anyChange;
            var iteration = 1;
            var maxIterations = 50;

            do
            {
                Log.Debug("Assigning horizontal positions, iteration {0}", iteration);
                var min = 1;
                anyChange = false;

                foreach (var techlevel in techlevels)
                {
                    // enforce minimum x position based on techlevels
                    var nodes = Nodes.OfType<ResearchNode>().Where(n => n.Research.techLevel == techlevel);
                    if (!nodes.Any())
                        continue;

                    foreach (var node in nodes)
                        anyChange = node.SetDepth(min) || anyChange;

                    min = nodes.Max(n => n.X) + 1;

                    Log.Debug("\t{0}, change: {1}", techlevel, anyChange);
                }
            } while (anyChange && iteration++ < maxIterations);
        }

        public static void DrawLine( Pair<ResearchNode, ResearchNode> connection, Color color,
                                     bool reverseDirection = false )
        {
            Vector2 a, b;

            if ( reverseDirection )
            {
                a = connection.First.Right;
                b = connection.Second.Left;
            }
            else
            {
                a = connection.First.Left;
                b = connection.Second.Right;
            }

            GUI.color = color;
            var isHubLink = false;

            Vector2 left, right;
            // make sure line goes left -> right
            if ( a.x < b.x )
            {
                left = a;
                right = b;
            }
            else
            {
                left = b;
                right = a;
            }

            // if left and right are on the same level, just draw a straight line.
            if ( Math.Abs( left.y - right.y ) < 0.1f )
            {
                var line = new Rect( left.x, left.y - 2f, right.x - left.x, 4f );
                GUI.DrawTexture( line, EW );
            }

            // draw three line pieces and two curves.
            else
            {
                // determine top and bottom y positions
                float top = Math.Min( left.y, right.y ) + Settings.NodeMargins.x / 4f;
                float bottom = Math.Max( left.y, right.y ) - Settings.NodeMargins.x / 4f;

                // if these positions are more than X nodes apart, draw an invisible 'hub' link.
                if ( false )
                    // TODO: commented out for debug. Math.Abs( top - bottom ) > Settings.LineMaxLengthNodes * Settings.NodeSize.y )
                {
                    isHubLink = true;

                    // left to hub
                    var leftToHub = new Rect( left.x, left.y + 15f, Settings.NodeMargins.x / 4f, 4f );
                    GUI.DrawTexture( leftToHub, EW );

                    // hub to right
                    var hubToRight = new Rect( right.x - Settings.NodeMargins.x / 4f, right.y + 15f,
                                               Settings.NodeMargins.x / 4f, 4f );
                    GUI.DrawTexture( hubToRight, EW );

                    // left hub
                    var hub = new Rect( left.x + Settings.NodeMargins.x / 4f - Settings.HubSize / 2f,
                                        left.y + 17f - Settings.HubSize / 2f,
                                        Settings.HubSize,
                                        Settings.HubSize );
                    GUI.DrawTexture( hub, CircleFill );

                    // add tooltip
                    if ( !MainTabWindow_ResearchTree.hubTips.ContainsKey( hub ) )
                    {
                        MainTabWindow_ResearchTree.hubTips.Add( hub, new List<string>() );
                        MainTabWindow_ResearchTree.hubTips[hub].Add( "Fluffy.ResearchTree.LeadsTo".Translate() );
                    }
                    MainTabWindow_ResearchTree.hubTips[hub].Add( connection.First.Research.LabelCap );

                    // right hub
                    hub.position = new Vector2( right.x - Settings.NodeMargins.x / 4f - Settings.HubSize / 2f,
                                                right.y + 17f - Settings.HubSize / 2f );
                    GUI.DrawTexture( hub, CircleFill );

                    // add tooltip
                    if ( !MainTabWindow_ResearchTree.hubTips.ContainsKey( hub ) )
                    {
                        MainTabWindow_ResearchTree.hubTips.Add( hub, new List<string>() );
                        MainTabWindow_ResearchTree.hubTips[hub].Add( "Fluffy.ResearchTree.Requires".Translate() );
                    }
                    MainTabWindow_ResearchTree.hubTips[hub].Add( connection.Second.Research.LabelCap );
                }
                    // but when nodes are close together, just draw the link as usual.
                // left to curve
                var leftToCurve = new Rect( left.x, left.y - 2f, Settings.NodeMargins.x / 4f, 4f );
                GUI.DrawTexture( leftToCurve, EW );

                // curve to curve
                var curveToCurve = new Rect( left.x + Settings.NodeMargins.x / 2f - 2f, top, 4f, bottom - top );
                GUI.DrawTexture( curveToCurve, NS );

                // curve to right
                var curveToRight = new Rect( left.x + Settings.NodeMargins.x / 4f * 3, right.y - 2f,
                                             right.x - left.x - Settings.NodeMargins.x / 4f * 3, 4f );
                GUI.DrawTexture( curveToRight, EW );

                // curve positions
                var curveLeft = new Rect( left.x + Settings.NodeMargins.x / 4f, left.y - Settings.NodeMargins.x / 4f,
                                          Settings.NodeMargins.x / 2f, Settings.NodeMargins.x / 2f );
                var curveRight = new Rect( left.x + Settings.NodeMargins.x / 4f,
                                           right.y - Settings.NodeMargins.x / 4f, Settings.NodeMargins.x / 2f,
                                           Settings.NodeMargins.x / 2f );

                // going down
                if ( left.y < right.y )
                {
                    GUI.DrawTextureWithTexCoords( curveLeft, Circle, new Rect( 0.5f, 0.5f, 0.5f, 0.5f ) );
                    // bottom right quadrant
                    GUI.DrawTextureWithTexCoords( curveRight, Circle, new Rect( 0f, 0f, 0.5f, 0.5f ) );
                    // top left quadrant
                }
                // going up
                else
                {
                    GUI.DrawTextureWithTexCoords( curveLeft, Circle, new Rect( 0.5f, 0f, 0.5f, 0.5f ) );
                    // top right quadrant
                    GUI.DrawTextureWithTexCoords( curveRight, Circle, new Rect( 0f, 0.5f, 0.5f, 0.5f ) );
                    // bottom left quadrant
                }
            }

            // draw the end arrow (if not hub link)
            var end = new Rect( right.x - 16f, right.y - 8f, 16f, 16f );

            if ( !isHubLink )
                GUI.DrawTexture( end, End );

            // reset color
            GUI.color = Color.white;
        }

        //public static void GraphSharpTests()
        //{
        //    var graph = new GraphSharp.HierarchicalGraph<>();
        //}
        public static void Initialize()
        {
            // populate all nodes
            _nodes = new List<Node>( DefDatabase<ResearchProjectDef>.AllDefsListForReading
                                          // exclude hidden projects (prereq of itself is a common trick to hide research).
                                                                     .Where(
                                                                            def =>
                                                                            def.prerequisites.NullOrEmpty() ||
                                                                            !def.prerequisites.Contains( def ) )
                                                                     .Select( def => new ResearchNode( def ) as Node ) );

            // mark, but do not remove redundant prerequisites.
            foreach ( ResearchNode node in Nodes.OfType<ResearchNode>() )
            {
                if ( !node.Research.prerequisites.NullOrEmpty() )
                {
                    List<ResearchProjectDef> ancestors =
                        node.Research.prerequisites?.SelectMany( r => r.GetPrerequisitesRecursive() ).ToList();
                    if ( !ancestors.NullOrEmpty() &&
                         ( !node.Research.prerequisites?.Intersect( ancestors ).ToList().NullOrEmpty() ?? false ) )
                    {
                        Log.Warning( "ResearchTree :: redundant prerequisites for " + node.Research.LabelCap +
                                     " the following research: " +
                                     string.Join( ", ",
                                                  node.Research.prerequisites?.Intersect( ancestors )
                                                      .Select( r => r.LabelCap )
                                                      .ToArray() ) );
                    }
                    if ( node.Research.prerequisites.Any( r => r.techLevel > node.Research.techLevel ) )
                        Log.Error( "ResearchTree :: " + node.Research.defName +
                                   " has a lower techlevel than (one of) it's dependenc(y/ies)" );
                }
            }

            // create links between nodes
            foreach ( ResearchNode node in Nodes.OfType<ResearchNode>() )
                node.CreateLinks();

            // calculate Depth of each node
            // NOTE: These are the layers in graph terminology
            foreach ( ResearchNode node in Nodes.OfType<ResearchNode>() )
                node.SetDepth();

            // create dummy vertices for edges that span multiple layers
            var dummies = new List<Node>();
            foreach ( ResearchNode node in Nodes.OfType<ResearchNode>() )
                foreach ( ResearchNode child in node.Children.Where( child => child.X - node.X > 1 ) )
                    dummies.AddRange( CreateDummyNodes( node, child ) );

            // add dummy vertices to tree (can't do this in iteration because we'd be modifying the iteratee)
            Nodes.AddRange( dummies );

            // arrange nodes within layers to minimize edge crossings
            MinimizeCrossings();

            // create a visually pleasing layout, with a minimum of bends in long edges
            CreateLayout();

            // Done!
            Initialized = true;
        }

        private static void CreateLayout()
        {
            // three things happen;
            // mark type 1 (crossing between long and short edge)
            // create blocks of aligned vertices
            // collapse blocks to minimize vertical space used
            // Brandes, U., & Köpf, B. (2001, September). Fast and simple horizontal coordinate assignment. In International Symposium on Graph Drawing (pp. 31-44). Springer Berlin Heidelberg.

            int before = Crossings();

            MarkTypeIConflicts();

#if DEBUG
            Log.Message(
                        $"Conflicts: \n\t{string.Join( "\n\t", marks.Select( p => $"{p.First} -> {p.Second}" ).ToArray() )} " );
#endif

            HorizontalAlignment();

#if DEBUG
            IEnumerable<KeyValuePair<Node, Node>> blocks = roots.Where( p => p.Key == p.Value );
            var root_msg = new StringBuilder();
            foreach ( Node root in blocks.Select( p => p.Key ) )
            {
                root_msg.AppendLine( $"Block: {root}" );
                foreach ( KeyValuePair<Node, Node> node in roots.Where( p => p.Value == root ) )
                    root_msg.AppendLine( $"\t{node}" );
            }

            Log.Message( root_msg.ToString() );
            Log.Message( $"Align: \n\t{string.Join( "\n\t", align.Select( p => $"{p.Key} -> {p.Value}" ).ToArray() )} " );

#endif
            // todo: this is where things go horribly wrong.
            //            VerticalCompaction();

            //#if DEBUG
            //            Log.Message( $"Sink: \n\t{string.Join( "\n\t", sink.Select( p => $"{p.Key} -> {p.Value}" ).ToArray() )} " );
            //            Log.Message( $"Shift: \n\t{string.Join( "\n\t", shift.Select( p => $"{p.Key} -> {p.Value}" ).ToArray() )} " );
            //#endif

            //foreach ( var leaf in Nodes )
            //    leaf.Y = roots[leaf].Y;

            int Yoffset = Nodes.Min( n => n.Y ) - 1;
            foreach ( Node leaf in Nodes )
                leaf.Y -= Yoffset;

            int after = Crossings();
            Log.Message( $"CreateLayout: {before} -> {after}" );
        }

        internal static void DrawDebug()
        {
            foreach ( Node v in Nodes )
            {
                if ( v != roots[v] )
                    Widgets.DrawLine( v.Center, roots[v].Center, Color.red, 1 );
                if ( v != align[v] && Math.Abs( align[v].X - v.X ) <= 1 )
                    Widgets.DrawLine( v.Center, align[v].Center, Color.blue, 4 );
                foreach ( Node w in v.OutNodes )
                    Widgets.DrawLine( v.Right, w.Left, Color.white, 1 );
            }
        }

        private static void HorizontalAlignment()
        {
            // Brandes & Kopf, 2001, p37 (Alg 2).
            roots = Nodes.ToDictionary( n => n, n => n );
            align = Nodes.ToDictionary( n => n, n => n );

            var msg = new StringBuilder();
            msg.AppendLine( "Horizontal alignment log" );

            // loop over layers
            for ( int l = Size.x - 1; l > 0; l-- )
            {
                msg.AppendLine( $"Layer {l}" );
                int r = -1;
                List<Node> layer = Layer( l, true );
                List<Node> below = Layer( l + 1, true );

                // loop over nodes in layer
                for ( var pos_v = 0; pos_v < layer.Count; pos_v++ )
                {
                    Node v = layer[pos_v];
                    msg.AppendLine( $"\tChecking {v}" );

                    // if node has any neighbours on layer l+1
                    if ( v.OutNodes.Any() )
                    {
                        List<Node> neighbours = v.OutNodes;
                        neighbours.SortBy( n => n.Y );
                        int d = neighbours.Count;
                        msg.AppendLine( $"\t\thas {d} neighbours" );
                        int[] medians = {(int) Math.Floor( ( d - 1f ) / 2f ), (int) Math.Ceiling( ( d - 1f ) / 2f )};
                        foreach ( int m in medians )
                        {
                            msg.AppendLine( "\t\t" + ( align[v] == v ? "not yet aligned" : "already aligned" ) );
                            // if not yet aligned, and m is a valid node index
                            if ( align[v] == v )
                            {
                                // if the median node is not marked as type 1
                                Node u = neighbours[m];
                                msg.AppendLine( $"\t\ttrying to align with {u}" );
                                int pos_u = below.IndexOf( u );
                                var edge = new Pair<Node, Node>( v, u );
                                if ( marks.Contains( edge ) )
                                    msg.AppendLine( $"\t\t{v} -> {u} is marked as a type I conflict" );
                                if ( r >= pos_u )
                                    msg.AppendLine( $"\t\tpos(u) = {pos_u}, >= r = {r}" );
                                if ( !marks.Contains( edge )
                                     && r < pos_u )
                                {
                                    msg.AppendLine( $"\t\t aligning {u} to {v} " );
                                    align[u] = v;
                                    roots[v] = roots[u];
                                    align[v] = roots[v];
                                    r = pos_u;
                                }
                            }
                        }
                    }
                }
            }
#if DEBUG
            Log.Message( msg.ToString() );
#endif
        }

        private static void VerticalCompaction()
        {
            // Brandes & Kopf, 2001, p38 (Alg 3).

            var msg = new StringBuilder( "Vertical compaction log" );
            sink = Nodes.ToDictionary( n => n, n => n );
            shift = Nodes.ToDictionary( n => n, n => int.MaxValue );
            positioned = Nodes.ToDictionary( n => n, n => false );

            foreach ( Node v in Nodes )
                if ( roots[v] == v )
                    PlaceBlock( v, ref msg, 1 );

            Log.Message( msg.ToString() );

            foreach ( Node v in Nodes )
            {
                PositionNode( v, roots[v].Y );
                if ( shift[sink[roots[v]]] < int.MaxValue )
                    PositionNode( v, v.Y + shift[sink[roots[v]]] );
            }

            // done!
        }

        private static string Tabs( int n ) { return new String( '\t', n ); }

        private static void PlaceBlock( Node v, ref StringBuilder msg, int d )
        {
            // function place_block, Brandes & Kopf, 2001, p38 (Alg 3).
            msg.AppendLine( $"{Tabs( d )}placing block of {v}" );
            if ( positioned[v] )
            {
                msg.AppendLine( $"{Tabs( d )}already positioned" );
                return;
            }

            // I have absolutely zero idea of what's going on here.
            Node w = v;
            do
            {
                List<Node> layer = Layer( w.X, true );
                int index = layer.IndexOf( w );
                msg.AppendLine( $"{Tabs( d )}index: {index}" );
                if ( index > 0 )
                {
                    Node u = roots[layer[index - 1]];
                    PlaceBlock( u, ref msg, d + 1 );

                    if ( sink[v] == v )
                    {
                        msg.AppendLine( $"{Tabs( d )}sink(v) == v [{v}]" );
                        msg.AppendLine( $"{Tabs( d )}setting sink(v) -> sink(u) [{sink[u]}]" );
                        sink[v] = sink[u];
                    }
                    if ( sink[v] != sink[u] )
                    {
                        msg.AppendLine( $"{Tabs( d )}sink(v) != sink(u) [{sink[v]} != {sink[u]}]" );
                        msg.AppendLine(
                                       $"{Tabs( d )}setting shift(sink(u)) = Min( shift(sink(u)), y(v) - y(u) - 1) [Min({shift[sink[u]]}, {v.Y} - {u.Y} - 1]" );
                        shift[sink[u]] = Math.Min( shift[sink[u]], v.Y - u.Y - 1 );
                    }
                    else
                    {
                        msg.AppendLine( $"{Tabs( d )}sink(v) == sink(u) [{sink[v]} == {sink[u]}]" );
                        msg.AppendLine( $"{Tabs( d )}setting y(v) to Max( y(v), y(u) + 1 [{v.Y}, {u.Y + 1}" );
                        PositionNode( v, Math.Max( v.Y, u.Y + 1 ) );
                    }
                }

                msg.AppendLine( $"{Tabs( d )}moving from {w} to {align[w]}" );
                w = align[w];
            } while ( w != v );
        }

        // small wrapper for positioning to keep track of defined positions in the horizontal compaction stage.
        private static void PositionNode( Node node, int Y )
        {
            node.Y = Y;
            positioned[node] = true;
        }

        private static void MarkTypeIConflicts()
        {
            // Brandes & Kopf, 2001, p36 (Alg 1).
            marks = new HashSet<Pair<Node, Node>>();
            for ( var l = 1; l < Size.x; l++ )
            {
                int left_inner = 1, right_inner = 1;
                List<Node> layer = Layer( l, true );
                List<Node> next_layer = Layer( l + 1, true );
                int layer_size = layer.Max( n => n.Y );
                int next_layer_size = next_layer.Max( n => n.Y );

                for ( int i = 1, i1 = 1; i1 <= next_layer_size; i1++ )
                {
                    Node node = NodeAtPos( l + 1, i1 );
                    // find vertices that are part of an inner (long) edge
                    if ( node is DummyNode || node == next_layer.Last() )
                    {
                        // right_inner position is the endpoint of the edge on the
                        // current layer.
                        if ( node is DummyNode )
                            right_inner = node.InNodes.First().Y;
                        else
                            right_inner = layer_size;

                        // keeping track of nodes already checked, mark edges that
                        // cross nearest inner boundaries.
                        while ( i < i1 )
                        {
                            Node check = NodeAtPos( l + 1, i++ );
                            if ( check?.InNodes?.Any() ?? false )
                                foreach ( Node neighbour in check.InNodes )
                                    if ( neighbour.Y < left_inner || neighbour.Y > right_inner )
                                    {
#if TRACE_CONFLICTS
                                        Log.Message( $"Conflict: {neighbour} -> {check}, @{node} ({l}; {left_inner}-{right_inner})" );
#endif
                                        marks.Add( new Pair<Node, Node>( neighbour, check ) );
                                    }
                        }

                        // right inner is now left inner.
                        left_inner = right_inner;
                    }
                }
            }
        }

        private static Node NodeAtPos( int X, int Y ) { return Nodes.FirstOrDefault( n => n.X == X && n.Y == Y ); }

        public static List<Node> CreateDummyNodes( ResearchNode parent, ResearchNode child )
        {
            // decouple parent and child
            parent.OutNodes.Remove( child );
            child.InNodes.Remove( parent );

            // create dummy nodes
            int n = child.X - parent.X;
            var dummies = new List<Node>( n );
            Node last = parent;

            for ( var i = 1; i < n; i++ )
            {
                // create empty dummy
                var dummy = new DummyNode();
                dummies.Add( dummy );

                // hook up the chain
                last.OutNodes.Add( dummy );
                dummy.InNodes.Add( last );
                dummy.X = last.X + 1;

                // this is now last
                last = dummy;
            }

            // hook up child
            last.OutNodes.Add( child );
            child.InNodes.Add( last );

            // done!
            return dummies;
        }

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
                if ( !MedianSweep( iteration++ ) )
                    burnout--;

            // (note that the last iteration without progress often _increases_ the amount of crossings,
            // hence, we run one last iteration, hopefully resetting crossings to the previous minimum.
            MedianSweep( iteration );

            // perform sweeps of adjacent node reorderings
            iteration = 0;
            max_iterations = 50;
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

        private static bool MedianSweep( int iteration )
        {
            // count number of crossings before sweep
            int before = Crossings();

            // do up/down sweep on alternating iterations
            if ( iteration % 2 == 0 )
            {
                for ( var i = 1; i < Size.x; i++ )
                {
                    List<Node> nodes = Layer( i );
                    List<Pair<Node, float>> medians =
                        nodes.Select( n => new Pair<Node, float>( n, GetMedianY( n.InNodes ) ) ).ToList();
                    SetLayerPositions( medians );
                }
            }
            else
            {
                for ( int i = Size.x; i > 1; i-- )
                {
                    List<Node> nodes = Layer( i );
                    List<Pair<Node, float>> medians =
                        nodes.Select( n => new Pair<Node, float>( n, GetMedianY( n.OutNodes ) ) ).ToList();
                    SetLayerPositions( medians );
                }
            }

            // count number of crossings after sweep
            int after = Crossings();

#if DEBUG
            Log.Message( $"MedianSweep: {before} -> {after}" );
#endif

            // did we make progress? please?
            return after < before;
        }

        private static float GetMedianY( List<Node> nodes )
        {
            if ( nodes.NullOrEmpty() )
                return -1;

            return nodes.Sum( n => n.Y ) / (float) nodes.Count;
        }

        private static void SetLayerPositions( List<Pair<Node, float>> nodeMedianPairs )
        {
            // we can be fairly straightforward here, as we're only concerned with crossing edges.
            // determining the best Y coordinates for a pretty graph will be handled later.
            IEnumerable<Node> nodes = nodeMedianPairs.OrderBy( p => p.Second )
                                                     .ThenBy( p => p.First.Descendants.Count )
                                                     .Select( p => p.First );

            // set Y positions 1, |nodes|
            var Y = 1;
            foreach ( Node node in nodes )
                node.Y = Y++;
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
                crossings += Crossings( i );

            return crossings;
        }

        private static int Crossings( Node A, Node B ) { return UpperCrossings( A, B ) + LowerCrossings( A, B ); }

        private static int Crossings( int depth, bool up = false )
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
                    //text.AppendLine( $"\t\tAlign: {align[n]}" );
                    //text.AppendLine( $"\t\tRoot: {roots[n]}" );
                }
            }

            return text.ToString();
        }
    }
}
