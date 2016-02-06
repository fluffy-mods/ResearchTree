// ResearchTree/Node.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-12-28 17:55

using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityCoreLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyResearchTree
{
    public class Node
    {
        // shortcuts to UI rects (these are only generated once and accessed through properties)
        private Rect _queueRect, _rect, _labelRect, _costLabelRect, _costIconRect, _iconsRect;
        private bool _rectSet;

        // research icon
        public static Texture2D ResearchIcon = ContentFinder<Texture2D>.Get( "Research" );

        // further offsets and positional variables
        public IntVec2 Pos;
        private const float Offset = 2f;
        private const float LabSize = 30f;
        public int Depth;
        private bool _largeLabel = false;

        // left/right edges of Rects
        private Vector2 _left = Vector2.zero;
        private Vector2 _right = Vector2.zero;

        // node relations
        public List<Node> Parents;
        public List<Node> Children;
        public Tree Tree;
        public string Genus;

        // what it's all about - the research project.
        public ResearchProjectDef Research;

        // enable linking to CCL help tab for details
        MainTabWindow_ModHelp helpWindow = DefDatabase<MainTabDef>.GetNamed("CCL_ModHelp", false).Window as MainTabWindow_ModHelp;

        /// <summary>
        /// Static UI rect for this node
        /// </summary>
        public Rect Rect
        {
            get
            {
                if( !_rectSet )
                {
                    CreateRects();
                }
                return _rect;
            }
        }

        /// <summary>
        /// Middle of left node edge
        /// </summary>
        public Vector2 Left
        {
            get
            {
                if ( _left == Vector2.zero )
                {
                    _left = new Vector2( Pos.x * ( Settings.Button.x + Settings.Margin.x ) + Offset,
                                         Pos.z * ( Settings.Button.y + Settings.Margin.y ) + Offset + Settings.Button.y / 2 );
                }
                return _left;
            }
        }

        /// <summary>
        /// Middle of right node edge
        /// </summary>
        public Vector2 Right
        {
            get
            {
                if ( _right == Vector2.zero )
                {
                    _right = new Vector2( Pos.x * ( Settings.Button.x + Settings.Margin.x ) + Offset + Settings.Button.x,
                                          Pos.z * ( Settings.Button.y + Settings.Margin.y ) + Offset + Settings.Button.y / 2 );
                }
                return _right;
            }
        }

        /// <summary>
        /// Tag UI Rect
        /// </summary>
        public Rect QueueRect
        {
            get
            {
                if( !_rectSet )
                {
                    CreateRects();
                }
                return _queueRect;
            }
        }

        public Rect LabelRect
        {
            get
            {
                if( !_rectSet )
                {
                    CreateRects();
                }
                return _labelRect;
            }
        }

        public Rect CostLabelRect
        {
            get
            {
                if( !_rectSet )
                {
                    CreateRects();
                }
                return _costLabelRect;
            }
        }

        public Rect CostIconRect
        {
            get
            {
                if( !_rectSet )
                {
                    CreateRects();
                }
                return _costIconRect;
            }
        }

        public Rect IconsRect
        {
            get
            {
                if( !_rectSet )
                {
                    CreateRects();
                }
                return _iconsRect;
            }
        }

        public Node( ResearchProjectDef research )
        {
            Research = research;
            List<string> parts = research.LabelCap.Split( " ".ToCharArray() ).ToList();
            parts.Remove( parts.Last() );
            Genus = string.Join( " ", parts.ToArray() );
            Parents = new List<Node>();
            Children = new List<Node>();
        }
        
        private void CreateRects()
        {
            // main rect
            _rect = new Rect( Pos.x * ( Settings.Button.x + Settings.Margin.x ) + Offset,
                              Pos.z * ( Settings.Button.y + Settings.Margin.y ) + Offset,
                              Settings.Button.x,
                              Settings.Button.y );

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
                                  _rect.height * .5f - 3f);

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
            _largeLabel = Text.CalcHeight( Research.LabelCap, _labelRect.width ) > _labelRect.height;

            // done
            _rectSet = true;
        }

        /// <summary>
        /// Set all prerequisites as parents of this node, and for each parent set this node as a child.
        /// </summary>
        public void CreateLinks()
        {
            foreach ( ResearchProjectDef prerequisite in Research.prerequisites )
            {
                if ( prerequisite != Research )
                {
                    Parents.Add( ResearchTree.Forest.FirstOrDefault( node => node.Research == prerequisite ) );
                }
            }
            foreach ( Node parent in Parents )
            {
                parent.Children.Add( this );
            }
        }

        /// <summary>
        /// Recursively determine the depth of this node.
        /// </summary>
        public void SetDepth()
        {
            List<Node> level = new List<Node>();
            level.Add( this );
            while ( level.Count > 0 &&
                    level.Any( node => node.Parents.Count > 0 ) )
            {
                // has any parent, increment level.
                Depth++;

                // set level to next batch of distinct Parents, where Parents may not be itself.
                level = level.SelectMany( node => node.Parents ).Distinct().Where( node => node != this ).ToList();

                // stop infinite recursion with loops of size greater than 2
                if ( Depth > 100 )
                {
                    Log.Error( Research.LabelCap +
                               " has more than 100 levels of prerequisites. Is the Research Tree defined as a loop?" );
                }
            }
        }

        /// <summary>
        /// Determine the closest tree by moving along parents and then children until a tree has been found. Returns first tree encountered, or NULL.
        /// </summary>
        /// <returns></returns>
        public Tree ClosestTree()
        {
            // go up through all Parents until we find a parent that is in a Tree
            Queue<Node> parents = new Queue<Node>();
            parents.Enqueue( this );

            while ( parents.Count > 0 )
            {
                Node current = parents.Dequeue();
                if ( current.Tree != null )
                {
                    return current.Tree;
                }

                // otherwise queue up the Parents to be checked
                foreach ( Node parent in current.Parents )
                {
                    parents.Enqueue( parent );
                }
            }

            // if that didn't work, try seeing if a child is in a Tree (unlikely, but whateva).
            Queue<Node> children = new Queue<Node>();
            children.Enqueue( this );

            while ( children.Count > 0 )
            {
                Node current = children.Dequeue();
                if ( current.Tree != null )
                {
                    return current.Tree;
                }

                // otherwise queue up the Children to be checked.
                foreach ( Node child in current.Children )
                {
                    children.Enqueue( child );
                }
            }

            // finally, if nothing stuck, return null
            return null;
        }

        /// <summary>
        /// Prints debug information.
        /// </summary>
        public void Debug()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine( Research.LabelCap + " (" + Depth + ", " + Genus + "):" );
            text.AppendLine( "- Parents" );
            foreach ( Node parent in Parents )
            {
                text.AppendLine( "-- " + parent.Research.LabelCap );
            }
            text.AppendLine( "- Children" );
            foreach ( Node child in Children )
            {
                text.AppendLine( "-- " + child.Research.LabelCap );
            }
            text.AppendLine( "" );
            Log.Message( text.ToString() );
        }

        /// <summary>
        /// Draw highlights around node, and optionally highlight links to parents/children of this node.
        /// </summary>
        /// <param name="color">color to use</param>
        /// <param name="linkParents">should links to parents be drawn?</param>
        /// <param name="linkChildren">should links to children be drawn?</param>
        public void Highlight( Color color, bool linkParents, bool linkChildren )
        {
            GUI.color = color;
            Widgets.DrawBox( Rect.ContractedBy( - 2f ), 2 );
            GUI.color = Color.white;
            if ( linkParents )
            {
                foreach ( Node parent in Parents )
                {
                    MainTabWindow_ResearchTree.highlightedConnections.Add( new Pair<Node, Node>( parent, this) );
                }
            }
            if ( linkChildren )
            {
                foreach ( Node child in Children )
                {
                    MainTabWindow_ResearchTree.highlightedConnections.Add( new Pair<Node, Node>( this, child ) );
                }
            }
        }

        /// <summary>
        /// Draw the node, including interactions.
        /// </summary>
        public void Draw()
        {
            // set color
            GUI.color = !Research.PrereqsFulfilled ? Tree.GreyedColor : Tree.MediumColor;

            // cop out if off-screen
            Rect screen = new Rect(MainTabWindow_ResearchTree._scrollPosition.x, MainTabWindow_ResearchTree._scrollPosition.y, Screen.width, Screen.height - 35);
            if (Rect.xMin > screen.xMax ||
                Rect.xMax < screen.xMin ||
                Rect.yMin > screen.yMax ||
                Rect.yMax < screen.yMin )
            {
                return;
            }

            // mouseover highlights
            if ( Mouse.IsOver( Rect ) )
            {
                // active button
                GUI.DrawTexture( Rect, ResearchTree.ButtonActive );

                // highlight this and all prerequisites if research not completed
                if ( !Research.IsFinished )
                {
                    Highlight( GenUI.MouseoverColor, true, false );
                    foreach ( Node prerequisite in GetMissingRequiredRecursive() )
                    {
                        prerequisite.Highlight( GenUI.MouseoverColor, true, false );
                    }
                }
                else // highlight followups
                {
                    foreach ( Node child in Children )
                    {
                        MainTabWindow_ResearchTree.highlightedConnections.Add( new Pair<Node, Node>( this, child ) );
                        child.Highlight( GenUI.MouseoverColor, false, false );
                    }
                }
            }
            // if not moused over, just draw the default button state
            else
            {
                GUI.DrawTexture( Rect, ResearchTree.Button );
            }

            // grey out center to create a progress bar effect, completely greying out research not started.
            if ( !Research.IsFinished )
            {
                Rect progressBarRect = Rect.ContractedBy( 2f );
                GUI.color = Tree.GreyedColor;
                progressBarRect.xMin += Research.PercentComplete * progressBarRect.width;
                GUI.DrawTexture( progressBarRect, BaseContent.WhiteTex );
            }

            // draw the research label
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = false;
            Text.Font = _largeLabel ? GameFont.Tiny : GameFont.Small;
            Widgets.Label( LabelRect, Research.LabelCap );

            // draw research cost and icon
            Text.Anchor = TextAnchor.UpperRight;
            Text.Font = GameFont.Small;
            Widgets.Label( CostLabelRect, Research.totalCost.ToStringByStyle( ToStringStyle.Integer ) );
            GUI.DrawTexture( CostIconRect, ResearchIcon );
            Text.WordWrap = true;

            // attach description and further info to a tooltip
            TooltipHandler.TipRegion( Rect, new TipSignal( GetResearchTooltipString(), Settings.TipID ) );

            // draw unlock icons
            List<Pair<Texture2D, string>> unlocks = Research.GetUnlockIconsAndDescs();
            for (int i = 0; i < unlocks.Count(); i++ )
            {
                Rect iconRect = new Rect( IconsRect.xMax - (i + 1) * ( Settings.Icon.x + 4f ),
                                          IconsRect.yMin + (IconsRect.height - Settings.Icon.y) / 2f,
                                          Settings.Icon.x,
                                          Settings.Icon.y);

                if (iconRect.xMin < IconsRect.xMin )
                {
                    // stop the loop if we're overflowing.
                    break;
                }

                // draw icon
                unlocks[i].First.DrawFittedIn( iconRect );

                // tooltip
                TooltipHandler.TipRegion( iconRect, new TipSignal( unlocks[i].Second, Settings.TipID, TooltipPriority.Pawn ) );
            }
            
            // if clicked and not yet finished, queue up this research and all prereqs.
            if ( Widgets.InvisibleButton( Rect ) && !Research.IsFinished )
            {
                // LMB is queue operations, RMB is info
                if ( Event.current.button == 0 )
                {
                    // if shift is held, add to queue, otherwise replace queue
                    Queue.EnqueueRange( GetMissingRequiredRecursive().Concat( new List<Node>( new[] { this } ) ), Event.current.shift );
                }
                else if ( Event.current.button == 1 )
                {
                    // right click links to CCL help def.
                    helpWindow.JumpTo( Research.GetHelpDef() );
                }
            }
        }

        /// <summary>
        /// Creates text version of research description and additional unlocks/prereqs/etc sections.
        /// </summary>
        /// <returns>string description</returns>
        private string GetResearchTooltipString()
        {
            // start with the descripton
            StringBuilder text = new StringBuilder();
            text.AppendLine( Research.description );
            text.AppendLine( "LClickReplaceQueue".Translate() );
            text.AppendLine( "ShiftLeftClickAddToQueue".Translate() );
            text.AppendLine( "RClickForDetails".Translate() );
            return text.ToString();
        }

        /// <summary>
        /// Get recursive list of all incomplete prerequisites
        /// </summary>
        /// <returns>List<Node> prerequisites</Node></returns>
        private List<Node> GetMissingRequiredRecursive()
        {
            List<Node> parents = new List<Node>( Parents.Where( node => !node.Research.IsFinished ) );
            List<Node> allParents = new List<Node>( parents );
            foreach ( Node current in parents )
            {
                allParents.AddRange( current.GetMissingRequiredRecursive() );
            }
            return allParents.Distinct().ToList();
        }

        public override string ToString()
        {
            return this.Research.LabelCap + this.Pos;
        }
    }
}