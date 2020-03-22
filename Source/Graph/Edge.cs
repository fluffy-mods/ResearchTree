// Edge.cs
// Copyright Karel Kroeze, 2018-2020

using System;
using System.Threading;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using UnityEngine;
using Verse;
using static FluffyResearchTree.Assets;
using static FluffyResearchTree.Constants;

namespace FluffyResearchTree
{
    public class Edge<T1, T2> : Edge where T1 : Node where T2 : Node
    { 
        private T1 _in;
        private T2 _out;

        public Edge( T1 @in, T2 @out ): base( @in, @out )
        {
            _in     = @in;
            _out    = @out;
        }

        public T1 In
        {
            get => _in;
            set => _in     = value;
        }

        public T2 Out
        {
            get => _out;
            set => _out    = value;
        }

        public int DrawOrder
        {
            get
            {
                if ( Out.Highlighted )
                    return 3;
                if ( Out.Completed )
                    return 2;
                if ( Out.Available )
                    return 1;
                return 0;
            }
        }

        public const int NUM_STEPS = 25;
        public const float EPSILON = NUM_STEPS / 10f;
        public void Draw( Rect visibleRect )
        {
            if ( !In.IsVisible( visibleRect ) && !Out.IsVisible( visibleRect ) )
                return;

            if ( Curve is Curve curve )
                foreach ( var segment in curve.Segments )
                    Draw( segment );
            else Draw( Curve );
        }

        public void Draw( ICurve segment )
        {
            var stepSize = ( segment.ParEnd - segment.ParStart ) / NUM_STEPS;
            for ( int step = 0; step < NUM_STEPS; step++ )
            {
                var start = segment[segment.ParStart + step * stepSize];
                var end = segment[segment.ParStart + ( step + 1 ) * stepSize];
                Widgets.DrawLine( new Vector2( (float) start.X, (float) start.Y ),
                                  new Vector2( (float) end.X, (float) end.Y ),
                                  Out.EdgeColor, 2f );
            }
        }
        
        public override string ToString()
        {
            return _in + " -> " + _out;
        }
    }
}