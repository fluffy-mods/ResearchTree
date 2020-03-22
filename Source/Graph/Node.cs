// Node.cs
// Copyright Karel Kroeze, 2019-2020

// #define TRACE_POS

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using UnityEngine;
using Verse;
using static FluffyResearchTree.Constants;

namespace FluffyResearchTree
{
    public class Node: Microsoft.Msagl.Core.Layout.Node
    {
        protected bool                   _largeLabel;
        protected IntVec2 _pos = IntVec2.Zero;

        protected Rect
            _queueRect,
            _rect,
            _labelRect,
            _costLabelRect,
            _costIconRect,
            _iconsRect,
            _lockRect;

        protected bool _rectsSet;

        protected Vector2 _topLeft = Vector2.zero,
                          _right   = Vector2.zero,
                          _left    = Vector2.zero;

        public Node(): base( CurveFactory.CreateRectangle( NodeSize.z, NodeSize.x, new Point() ) ) 
        // note that nodes are sideways, we want a left -> right layering and msagl gives us a top->down layering, so we'll rotate in post
        {}

        public List<Node> Descendants
        {
            get { return OutEdges.Select( e => e.Target ).OfType<ResearchNode>().SelectMany( n => n.Descendants ).ToList(); }
        }

        public Rect CostIconRect
        {
            get
            {
                if ( !_rectsSet )
                    SetRects();

                return _costIconRect;
            }
        }

        public Rect CostLabelRect
        {
            get
            {
                if ( !_rectsSet )
                    SetRects();

                return _costLabelRect;
            }
        }

        public virtual Color Color     => Color.white;
        public virtual Color EdgeColor => Color;

        public Rect IconsRect
        {
            get
            {
                if ( !_rectsSet )
                    SetRects();

                return _iconsRect;
            }
        }

        public Rect LabelRect
        {
            get
            {
                if ( !_rectsSet )
                    SetRects();

                return _labelRect;
            }
        }

        /// <summary>
        ///     Middle of left node edge
        /// </summary>
        public Vector2 Left
        {
            get
            {
                if ( !_rectsSet )
                    SetRects();

                return _left;
            }
        }

        /// <summary>
        ///     Tag UI Rect
        /// </summary>
        public Rect QueueRect
        {
            get
            {
                if ( !_rectsSet )
                    SetRects();

                return _queueRect;
            }
        }

        public Rect LockRect
        {
            get
            {
                if ( !_rectsSet )
                    SetRects();

                return _lockRect;
            }
        }

        /// <summary>
        ///     Static UI rect for this node
        /// </summary>
        public Rect Rect
        {
            get
            {
                if ( !_rectsSet )
                    SetRects();

                return _rect;
            }
        }

        /// <summary>
        ///     Middle of right node edge
        /// </summary>
        public Vector2 Right
        {
            get
            {
                if ( !_rectsSet )
                    SetRects();

                return _right;
            }
        }

        public virtual int X
        {
            get => (int) _pos.x;
            set
            {
                _pos.x    = value;
                _rectsSet = false;
            }
        }

        public virtual int Y
        {
            get => (int) _pos.z;
            set
            {
                _pos.z    = value;
                _rectsSet = false;
            }
        }

        public virtual IntVec2 Pos
        {
            get => _pos;
            set
            {
                _pos      = value + NodeSize / 2;
                _rectsSet = false;
            }
        }

        public virtual string Label { get; }

        public virtual bool Completed   => false;
        public virtual bool Available   => false;
        public virtual bool Highlighted { get; set; }

        public override string ToString()
        {
            return Label + _pos;
        }

        public void SetRects()
        {
            // origin
            _topLeft = new Vector2( X - NodeSize.x / 2, Y - NodeSize.z / 2 );

            SetRects( _topLeft );
        }

        public void SetRects( Vector2 topLeft )
        {
            // main rect
            _rect = new Rect( topLeft.x,
                              topLeft.y,
                              NodeSize.x,
                              NodeSize.z );

            // left and right edges
            _left  = new Vector2( _rect.xMin, _rect.yMin + _rect.height / 2f );
            _right = new Vector2( _rect.xMax, _left.y );

            // queue rect
            _queueRect = new Rect( _rect.xMax - QueueLabelSize                    / 2f,
                                   _rect.yMin + ( _rect.height - QueueLabelSize ) / 2f, QueueLabelSize,
                                   QueueLabelSize );

            // label rect
            _labelRect = new Rect( _rect.xMin             + 6f,
                                   _rect.yMin             + 3f,
                                   _rect.width * 2f / 3f  - 6f,
                                   _rect.height     * .5f - 3f );

            // research cost rect
            _costLabelRect = new Rect( _rect.xMin                  + _rect.width * 2f / 3f,
                                       _rect.yMin                  + 3f,
                                       _rect.width * 1f / 3f - 16f - 3f,
                                       _rect.height * .5f          - 3f );

            // research icon rect
            _costIconRect = new Rect( _costLabelRect.xMax,
                                      _rect.yMin + ( _costLabelRect.height - 16f ) / 2,
                                      16f,
                                      16f );

            // icon container rect
            _iconsRect = new Rect( _rect.xMin,
                                   _rect.yMin + _rect.height * .5f,
                                   _rect.width,
                                   _rect.height * .5f );

            // lock icon rect
            _lockRect = new Rect( 0f, 0f, 32f, 32f );
            _lockRect = _lockRect.CenteredOnXIn( _rect );
            _lockRect = _lockRect.CenteredOnYIn( _rect );

            // see if the label is too big
            _largeLabel = Text.CalcHeight( Label, _labelRect.width ) > _labelRect.height;

            // done
            _rectsSet = true;
        }

        public virtual bool IsVisible( Rect visibleRect )
        {
            return !(
                Rect.xMin > visibleRect.xMax ||
                Rect.xMax < visibleRect.xMin ||
                Rect.yMin > visibleRect.yMax ||
                Rect.yMax < visibleRect.yMin );
        }

        public virtual void Draw( Rect visibleRect, bool forceDetailedMode = false )
        {
        }
    }
}