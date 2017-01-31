// Karel Kroeze
// DummyNode.cs
// 2017-01-05

using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyResearchTree
{
    public class DummyNode : Node
    {
        #region Overrides of Node

        public override string Label
        {
            get { return "DUMMY: " + ( Parent?.Label ?? "??" ) + " -> " + ( Child?.Label ?? "??" ); }
        }

        #endregion

        #region Overrides of Node

        public override void Draw()
        {
            // cop out if off-screen
            var screen = new Rect( MainTabWindow_ResearchTree._scrollPosition.x,
                                   MainTabWindow_ResearchTree._scrollPosition.y, Screen.width, Screen.height - 35 );
            if ( Rect.xMin > screen.xMax ||
                 Rect.xMax < screen.xMin ||
                 Rect.yMin > screen.yMax ||
                 Rect.yMax < screen.yMin )
            {
                return;
            }

            Widgets.DrawBox( Rect );
            Widgets.Label( Rect, Label );
        }

        #endregion

        public DummyNode() : base() { }

        public ResearchNode Parent
        {
            get
            {
                var parent = Above.FirstOrDefault() as ResearchNode;
                if ( parent != null )
                    return parent;

                var dummyParent = Above.FirstOrDefault() as DummyNode;

                return dummyParent?.Parent;
            }
        }
        
        public ResearchNode Child
        {
            get
            {
                var child = Below.FirstOrDefault() as ResearchNode;
                if ( child != null )
                    return child;

                var dummyChild = Below.FirstOrDefault() as DummyNode;

                return dummyChild?.Child;
            }
        }
    }
}