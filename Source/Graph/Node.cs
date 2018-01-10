// Karel Kroeze
// Node.cs
// 2016-12-28
// #define TRACE_POS
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace FluffyResearchTree
{
    public class Node
    {
        #region Fields

        protected IntVec2 _pos = IntVec2.Zero;
        public List<Node> _below = new List<Node>();
        public List<Node> _above = new List<Node>();

        protected const float LabSize = 30f;

        protected const float Offset = 2f;
        
        protected bool _largeLabel;


        protected Rect _queueRect,
                     _rect,
                     _labelRect,
                     _costLabelRect,
                     _costIconRect,
                     _iconsRect;

        protected bool _rectsSet;

        protected Vector2 _topLeft = Vector2.zero,
                            _right = Vector2.zero,
                             _left = Vector2.zero;

        #endregion Fields

        #region Constructors

        public Node()
        {
            _above = new List<Node>();
            _below = new List<Node>();
        }

        #endregion Constructors

        #region Properties

        public List<Node> Descendants
        {
            get { return _below.Concat( _below.SelectMany( node => node.Descendants ) ).ToList(); }
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

        public Color DrawColor => Color.white;

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
        /// Middle of left node edge
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
        /// Tag UI Rect
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

        /// <summary>
        /// Static UI rect for this node
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
        /// Middle of right node edge
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

        public Vector2 Center => ( Left + Right ) / 2f;

        public virtual int X
        {
            get { return _pos.x; }
            set
            {
#if TRACE_POS
                Log.Message( Label + " X: " + _pos.x + " -> " + value );
#endif
                _pos.x = value;
                Tree.Size.x = Tree.Nodes.Max( n => n.X );
                Tree.orderDirty = true;
            }
        }

        public virtual int Y
        {
            get { return _pos.z; }
            set
            {
#if TRACE_POS
                Log.Message( Label + " Y: " + _pos.z + " -> " + value );
#endif
                _pos.z = value;
                Tree.Size.z = Tree.Nodes.Max( n => n.Y );
                Tree.orderDirty = true;
            }
        }

        #endregion Properties

        #region Methods

        public virtual string Label { get; }

        public List<Node> Above => _above;

        public List<Node> Below => _below;

        /// <summary>
        /// Prints debug information.
        /// </summary>
        public virtual void Debug()
        {
            var text = new StringBuilder();
            text.AppendLine( Label + " (" + X + ", " + Y + "):" );
            text.AppendLine( "- Parents" );
            foreach ( Node parent in Above )
            {
                text.AppendLine( "-- " + parent.Label );
            }

            text.AppendLine( "- Children" );
            foreach ( Node child in Below )
            {
                text.AppendLine( "-- " + child.Label );
            }

            text.AppendLine( "" );
            Log.Message( text.ToString() );
        }

        

        public override string ToString()
        {
            return Label + _pos;
        }

        public void SetRects()
        {
            // origin
            _topLeft = new Vector2( ( X - 1 ) * ( Settings.NodeSize.x + Settings.NodeMargins.x ),
                                    ( Y - 1 ) * ( Settings.NodeSize.y + Settings.NodeMargins.y ) );

            // main rect
            _rect = new Rect( _topLeft.x,
                              _topLeft.y,
                              Settings.NodeSize.x,
                              Settings.NodeSize.y );

            // left and right edges
            _left = new Vector2( _rect.xMin, _rect.yMin + _rect.height / 2f );
            _right = new Vector2( _rect.xMax, _left.y );

            // queue rect
            _queueRect = new Rect( _rect.xMax - LabSize / 2f,
                                   _rect.yMin + ( _rect.height - LabSize ) / 2f,
                                   LabSize,
                                   LabSize );

            // label rect
            _labelRect = new Rect( _rect.xMin + 6f,
                                   _rect.yMin + 3f,
                                   _rect.width * 2f / 3f - 6f,
                                   _rect.height * .5f - 3f );

            // research cost rect
            _costLabelRect = new Rect( _rect.xMin + _rect.width * 2f / 3f,
                                       _rect.yMin + 3f,
                                       _rect.width * 1f / 3f - 16f - 3f,
                                       _rect.height * .5f - 3f );

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

            // see if the label is too big
            _largeLabel = Text.CalcHeight( Label, _labelRect.width ) > _labelRect.height;

            // done
            _rectsSet = true;
        }


        #endregion Methods

        public virtual void Draw() { }
    }
}
