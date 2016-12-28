// Karel Kroeze
// Node.cs
// 2016-12-28

using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyResearchTree
{
    public class Node
    {
        #region Fields

        public List<Node> Children = new List<Node>();
        public int Depth;
        public string Genus;
        public List<Node> Parents = new List<Node>();
        public IntVec2 Pos;
        public ResearchProjectDef Research;
        public Tree Tree;
        private const float LabSize = 30f;
        private const float Offset = 2f;

        private bool _largeLabel;
        private Vector2 _left = Vector2.zero;

        private Rect _queueRect,
                     _rect,
                     _labelRect,
                     _costLabelRect,
                     _costIconRect,
                     _iconsRect;

        private static Dictionary<ResearchProjectDef, bool> _buildingPresentCache = new Dictionary<ResearchProjectDef, bool>();

        private bool _rectsSet;
        private Vector2 _right = Vector2.zero;

        #endregion Fields

        #region Constructors

        public Node( ResearchProjectDef research )
        {
            Research = research;

            // get the Genus, this is the research family name, and will be used to group research together.
            // First see if we have a ":" in the name
            List<string> parts = research.LabelCap.Split( ":".ToCharArray() ).ToList();
            if ( parts.Count > 1 )
            {
                Genus = parts.First();
            }
            else
            // otherwise, strip the last word (intended to catch 1,2,3/ I,II,III,IV suffixes)
            {
                parts = research.LabelCap.Split( " ".ToCharArray() ).ToList();
                parts.Remove( parts.Last() );
                Genus = string.Join( " ", parts.ToArray() );
            }
            Parents = new List<Node>();
            Children = new List<Node>();
        }

        #endregion Constructors

        #region Properties

        public Rect CostIconRect
        {
            get
            {
                if ( !_rectsSet )
                    CreateRects();

                return _costIconRect;
            }
        }

        public Rect CostLabelRect
        {
            get
            {
                if ( !_rectsSet )
                    CreateRects();

                return _costLabelRect;
            }
        }

        public Rect IconsRect
        {
            get
            {
                if ( !_rectsSet )
                    CreateRects();

                return _iconsRect;
            }
        }

        public Rect LabelRect
        {
            get
            {
                if ( !_rectsSet )
                    CreateRects();

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
                if ( _left == Vector2.zero )
                {
                    _left = new Vector2( Pos.x * ( Settings.NodeSize.x + Settings.NodeMargins.x ) + Offset,
                                         Pos.z * ( Settings.NodeSize.y + Settings.NodeMargins.y ) + Offset +
                                         Settings.NodeSize.y / 2 );
                }
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
                    CreateRects();

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
                    CreateRects();

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
                if ( _right == Vector2.zero )
                {
                    _right =
                        new Vector2(
                            Pos.x * ( Settings.NodeSize.x + Settings.NodeMargins.x ) + Offset + Settings.NodeSize.x,
                            Pos.z * ( Settings.NodeSize.y + Settings.NodeMargins.y ) + Offset + Settings.NodeSize.y / 2 );
                }
                return _right;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Determine the closest tree by moving along parents and then children until a tree has been found. Returns first tree encountered, or NULL.
        /// </summary>
        /// <returns></returns>
        public Tree ClosestTree()
        {
            // go up through all Parents until we find a parent that is in a Tree
            var parents = new Queue<Node>();
            parents.Enqueue( this );

            while ( parents.Count > 0 )
            {
                Node current = parents.Dequeue();
                if ( current.Tree != null )
                    return current.Tree;

                // otherwise queue up the Parents to be checked
                foreach ( Node parent in current.Parents )
                    parents.Enqueue( parent );
            }

            // if that didn't work, try seeing if a child is in a Tree (unlikely, but whateva).
            var children = new Queue<Node>();
            children.Enqueue( this );

            while ( children.Count > 0 )
            {
                Node current = children.Dequeue();
                if ( current.Tree != null )
                    return current.Tree;

                // otherwise queue up the Children to be checked.
                foreach ( Node child in current.Children )
                    children.Enqueue( child );
            }

            // finally, if nothing stuck, return null
            return null;
        }

        /// <summary>
        /// Set all prerequisites as parents of this node, and for each parent set this node as a child.
        /// </summary>
        public void CreateLinks()
        {
            if ( Research.prerequisites.NullOrEmpty() )
                return;

            // 'vanilla' prerequisites
            foreach ( ResearchProjectDef prerequisite in Research.prerequisites )
            {
                // skip self prerequisite
                if ( prerequisite != Research )
                {
                    Node parent = ResearchTree.Forest.FirstOrDefault( node => node.Research == prerequisite );
                    if ( parent != null )
                        Parents.Add( parent );
                }
            }
            
            foreach ( Node parent in Parents )
            {
                parent.Children.Add( this );
            }
        }

        /// <summary>
        /// Prints debug information.
        /// </summary>
        public void Debug()
        {
            var text = new StringBuilder();
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

        public static void ClearCaches()
        {
            _buildingPresentCache.Clear();
            _missingFacilitiesCache.Clear();
        }

        private static Dictionary<ResearchProjectDef, List<ThingDef>> _missingFacilitiesCache =
            new Dictionary<ResearchProjectDef, List<ThingDef>>();

        public List<ThingDef> MissingFacilities() { return MissingFacilities( Research ); }

        public static List<ThingDef> MissingFacilities( ResearchProjectDef research )
        {
            // try get from cache
            Log.Message( research.LabelCap );
            List<ThingDef> missing;
            if ( _missingFacilitiesCache.TryGetValue( research, out missing ) )
                return missing;

            // get list of all researches required before this
            List<ResearchProjectDef> thisAndPrerequisites = research.GetPrerequisitesRecursive().Where( rpd => !rpd.IsFinished ).ToList();
            thisAndPrerequisites.Add( research );
                        
            // get list of all available research benches
            var availableBenches = Find.Maps.SelectMany( map => map.listerBuildings.allBuildingsColonist )
                                       .OfType<Building_ResearchBench>();
            var availableBenchDefs = availableBenches.Select( b => b.def ).Distinct();
            missing = new List<ThingDef>();
            
            // check each for prerequisites
            // TODO: We should really build this list recursively so we can re-use results for prerequisites.
            foreach ( ResearchProjectDef rpd in thisAndPrerequisites )
            {
                if ( rpd.requiredResearchBuilding == null )
                    continue;

                if ( !availableBenchDefs.Contains( rpd.requiredResearchBuilding ) )
                    missing.Add( rpd.requiredResearchBuilding );
            
                if ( rpd.requiredResearchFacilities.NullOrEmpty() )
                    continue;

                foreach ( ThingDef facility in rpd.requiredResearchFacilities )
                    if ( !availableBenches.Any( b => b.HasFacility( facility ) ) )
                        missing.Add( facility );
            }

            // add to cache
            missing = missing.Distinct().ToList();
            _missingFacilitiesCache.Add( research, missing );
            return missing;
        }

        public bool BuildingPresent() { return BuildingPresent( Research ); }

        public static bool BuildingPresent( ResearchProjectDef research )
        {
            // try get from cache
            bool result;
            if ( _buildingPresentCache.TryGetValue( research, out result ) )
                return result;

            // do the work manually
            if ( research.requiredResearchBuilding == null )
                result = true;
            else
                result = Find.Maps.SelectMany( map => map.listerBuildings.allBuildingsColonist )
                             .OfType<Building_ResearchBench>()
                             .Any( b => research.CanBeResearchedAt( b, true ) );

            if ( result )
                result = research.GetPrerequisitesRecursive().All( BuildingPresent );

            // update cache
            _buildingPresentCache.Add( research, result );
            return result;
        }

        public Color DrawColor
        {
            get
            {
                return Research.IsFinished || ( BuildingPresent() && Research.PrerequisitesCompleted ) ? Tree.MediumColor : Tree.GreyedColor;
            }
        }

        /// <summary>
        /// Draw the node, including interactions.
        /// </summary>
        public void Draw()
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

            // researches that are completed or could be started immediately, and that have the required building(s) available
            GUI.color = DrawColor;

            // mouseover highlights
            if ( Mouse.IsOver( Rect ) && BuildingPresent() )
            {
                // active button
                GUI.DrawTexture( Rect, Assets.ButtonActive );

                // highlight this and all prerequisites if research not completed
                if ( !Research.IsFinished )
                {
                    List<Node> prereqs = GetMissingRequiredRecursive();
                    Highlight( GenUI.MouseoverColor, true, false );
                    foreach ( Node prerequisite in prereqs )
                        prerequisite.Highlight( GenUI.MouseoverColor, true, false );
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
                GUI.DrawTexture( Rect, Assets.Button );
            }

            // grey out center to create a progress bar effect, completely greying out research not started.
            if ( !Research.IsFinished )
            {
                Rect progressBarRect = Rect.ContractedBy( 2f );
                GUI.color = Tree.GreyedColor;
                progressBarRect.xMin += Research.ProgressPercent * progressBarRect.width;
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
            Widgets.Label( CostLabelRect, Research.CostApparent.ToStringByStyle( ToStringStyle.Integer ) );
            GUI.DrawTexture( CostIconRect, Assets.ResearchIcon );
            Text.WordWrap = true;

            // attach description and further info to a tooltip
            TooltipHandler.TipRegion( Rect, GetResearchTooltipString() );
            if ( !BuildingPresent() )
            {
                TooltipHandler
                    .TipRegion( Rect, 
                                "Fluffy.ResearchTree.MissingFacilities".Translate( string.Join( ", ", MissingFacilities().Select( td => td.LabelCap ).ToArray() ) ) );
            }
            // new TipSignal( GetResearchTooltipString(), Settings.TipID ) );

            // draw unlock icons
            List<Pair<Def, string>> unlocks = Research.GetUnlockDefsAndDescs();
            for ( var i = 0; i < unlocks.Count; i++ )
            {
                var iconRect = new Rect( IconsRect.xMax - ( i + 1 ) * ( Settings.IconSize.x + 4f ),
                                         IconsRect.yMin + ( IconsRect.height - Settings.IconSize.y ) / 2f,
                                         Settings.IconSize.x,
                                         Settings.IconSize.y );

                if ( iconRect.xMin - Settings.IconSize.x < IconsRect.xMin &&
                     i + 1 < unlocks.Count )
                {
                    // stop the loop if we're about to overflow and have 2 or more unlocks yet to print.
                    iconRect.x = IconsRect.x + 4f;
                    GUI.DrawTexture( iconRect, Assets.MoreIcon, ScaleMode.ScaleToFit );
                    string tip = string.Join( "\n",
                                              unlocks.GetRange( i, unlocks.Count - i ).Select( p => p.Second ).ToArray() );
                    TooltipHandler.TipRegion( iconRect, tip );
                    // new TipSignal( tip, Settings.TipID, TooltipPriority.Pawn ) );
                    break;
                }

                // draw icon
                unlocks[i].First.DrawColouredIcon( iconRect );

                // tooltip
                TooltipHandler.TipRegion( iconRect, unlocks[i].Second );
                // new TipSignal( unlocks[i].Second, Settings.TipID, TooltipPriority.Pawn ) );
            }

            // if clicked and not yet finished, queue up this research and all prereqs.
            if ( Widgets.ButtonInvisible( Rect ) && BuildingPresent() )
            {
                // LMB is queue operations, RMB is info
                if ( Event.current.button == 0 && !Research.IsFinished )
                {
                    if ( !Queue.IsQueued( this ) )
                    {
                        // if shift is held, add to queue, otherwise replace queue
                        Queue.EnqueueRange( GetMissingRequiredRecursive().Concat( new List<Node>( new[] { this } ) ),
                                            Event.current.shift );
                    }
                    else
                    {
                        Queue.Dequeue( this );
                    }
                }
            }
        }

        /// <summary>
        /// Get recursive list of all incomplete prerequisites
        /// </summary>
        /// <returns>List<Node> prerequisites</Node></returns>
        public List<Node> GetMissingRequiredRecursive()
        {
            var parents = new List<Node>( Parents.Where( node => !node.Research.IsFinished ) );
            var allParents = new List<Node>( parents );
            foreach ( Node parent in parents )
                allParents.AddRange( parent.GetMissingRequiredRecursive() );

            return allParents.Distinct().ToList();
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
            Widgets.DrawBox( Rect.ContractedBy( -2f ), 2 );
            GUI.color = Color.white;
            if ( linkParents )
            {
                foreach ( Node parent in Parents )
                {
                    MainTabWindow_ResearchTree.highlightedConnections.Add( new Pair<Node, Node>( parent, this ) );
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
        /// Recursively determine the depth of this node.
        /// </summary>
        public void SetDepth()
        {
            var level = new List<Node>();
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

        public override string ToString()
        {
            return Research.LabelCap + Pos;
        }

        private void CreateRects()
        {
            // main rect
            _rect = new Rect( Pos.x * ( Settings.NodeSize.x + Settings.NodeMargins.x ) + Offset,
                              Pos.z * ( Settings.NodeSize.y + Settings.NodeMargins.y ) + Offset,
                              Settings.NodeSize.x,
                              Settings.NodeSize.y );

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
            _largeLabel = Text.CalcHeight( Research.LabelCap, _labelRect.width ) > _labelRect.height;

            // done
            _rectsSet = true;
        }

        /// <summary>
        /// Creates text version of research description and additional unlocks/prereqs/etc sections.
        /// </summary>
        /// <returns>string description</returns>
        private string GetResearchTooltipString()
        {
            // start with the descripton
            var text = new StringBuilder();
            text.AppendLine( Research.description );
            text.AppendLine();

            if ( Queue.IsQueued( this ) )
            {
                text.AppendLine( "Fluffy.ResearchTree.LClickRemoveFromQueue".Translate() );
            }
            else
            {
                text.AppendLine( "Fluffy.ResearchTree.LClickReplaceQueue".Translate() );
                text.AppendLine( "Fluffy.ResearchTree.SLClickAddToQueue".Translate() );
            }
            text.AppendLine( "Fluffy.ResearchTree.RClickForDetails".Translate() );

            return text.ToString();
        }

        #endregion Methods
    }
}
