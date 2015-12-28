// ResearchTree/Node.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-12-28 17:55

using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityCoreLibrary;
using UnityEngine;
using Verse;

namespace FluffyResearchTree
{
    public class Node
    {
        // shortcuts to UI rects (these are only generated once and accessed through properties)
        private Rect _tagRect;
        private Rect _rect;
        private bool _rectSet;

        // further offsets and positional variables
        public IntVec2 Pos;
        private const float Offset = 2f;
        private const float LabSize = 30f;
        public int Depth;

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
        public Rect TagRect
        {
            get
            {
                if ( !_rectSet )
                {
                    CreateRects();
                }
                return _tagRect;
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
            _rect = new Rect( Pos.x * ( Settings.Button.x + Settings.Margin.x ) + Offset,
                              Pos.z * ( Settings.Button.y + Settings.Margin.y ) + Offset, Settings.Button.x,
                              Settings.Button.y );
            _tagRect = new Rect( _rect.xMax - LabSize / 2f, _rect.yMin + ( _rect.height - LabSize ) / 2f, LabSize,
                                 LabSize );
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
                foreach ( Node current in Parents )
                {
                    ResearchTree.DrawLine( Left, current.Right, GenUI.MouseoverColor, 3 );
                }
            }
            if ( linkChildren )
            {
                foreach ( Node current2 in Children )
                {
                    ResearchTree.DrawLine( current2.Left, Right, GenUI.MouseoverColor, 3 );
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
                        ResearchTree.DrawLine( child.Left, Right, GenUI.MouseoverColor, 3 );
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
            Widgets.Label( Rect, Research.LabelCap );

            // attach description and further info to a tooltip
            TooltipHandler.TipRegion( Rect, GetResearchTooltipString() );

            // if clicked and not yet finished, queue up this research and all prereqs.
            if ( Widgets.InvisibleButton( Rect ) && !Research.IsFinished )
            {
                // if shift is held, add to queue, otherwise replace queue
                Queue.EnqueueRange( GetMissingRequiredRecursive().Concat( new List<Node>( new[] { this } ) ), Event.current.shift );
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

            // need to get the CCL helpDef for further info sections
            HelpDef helpDef = Research.GetHelpDef();
            if ( helpDef != null )
            {
                foreach ( HelpDetailSection section in helpDef.HelpDetailSections )
                {
                    text.AppendLine();
                    text.AppendLine( section.Label );
                    // def linked sections (can't do links here, so just create the strings)
                    if ( section.KeyDefs != null )
                    {
                        foreach ( DefStringTriplet triplet in section.KeyDefs )
                        {
                            text.AppendLine( section.InsetString + triplet.Prefix + triplet.Def.LabelCap + triplet.Suffix );
                        }
                    }

                    // string sections
                    if ( section.StringDescs != null )
                    {
                        foreach ( string str in section.StringDescs )
                        {
                            text.AppendLine( section.InsetString + str );
                        }
                    }
                }
            }
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
    }
}