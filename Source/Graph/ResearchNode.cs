using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluffyResearchTree;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using static FluffyResearchTree.Constants;

namespace FluffyResearchTree
{
    public class ResearchNode : Node
    {
        #region Fields

        public ResearchProjectDef Research;

        private static Dictionary<ResearchProjectDef, bool> _buildingPresentCache = new Dictionary<ResearchProjectDef, bool>();
        
        private static Dictionary<ResearchProjectDef, List<ThingDef>> _missingFacilitiesCache =
            new Dictionary<ResearchProjectDef, List<ThingDef>>();

        #endregion Fields

        public List<ResearchNode> Parents
        {
            get
            {
                var parents = InNodes.OfType<ResearchNode>();
                parents.Concat( InNodes.OfType<DummyNode>().Select( dn => dn.Parent ) );
                return parents.ToList();
            }
        }

        public List<ResearchNode> Children
        {
            get
            {
                var children = OutNodes.OfType<ResearchNode>();
                children.Concat( OutNodes.OfType<DummyNode>().Select( dn => dn.Child ) );
                return children.ToList();
            }
        }

        #region Constructors
        
        public ResearchNode( ResearchProjectDef research ) : base()
        {
            Research = research;

            // initialize position at vanilla y position, leave x at zero - we'll determine this ourselves
            _pos = new Vector2( 0, research.researchViewY + 1 );
        }

        #endregion Constructors

        #region Methods

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

        public static void ClearCaches()
        {
            _buildingPresentCache.Clear();
            _missingFacilitiesCache.Clear();
        }

        public static implicit operator ResearchNode( ResearchProjectDef def )
        {
            return Tree.Nodes.OfType<ResearchNode>().FirstOrDefault( n => n.Research == def );
        }

        public static List<ThingDef> MissingFacilities( ResearchProjectDef research )
        {
            // try get from cache
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

        public bool BuildingPresent()
        {
            return BuildingPresent( Research );
        }
        
        /// <summary>
        /// Draw the node, including interactions.
        /// </summary>
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

            // researches that are completed or could be started immediately, and that have the required building(s) available
            GUI.color = DrawColor;

            // mouseover highlights
            if ( Mouse.IsOver( Rect ) ) // TODO: commented out for debug && BuildingPresent() )
            {
                // active button
                GUI.DrawTexture( Rect, Assets.ButtonActive );

                // highlight this and all prerequisites if research not completed
                if ( !Research.IsFinished )
                {
                    List<ResearchNode> prereqs = GetMissingRequiredRecursive();
                    Highlight( GenUI.MouseoverColor, true, false );
                    foreach ( ResearchNode prerequisite in prereqs )
                        prerequisite.Highlight( GenUI.MouseoverColor, true, false );
                }
                else // highlight followups
                {
                    foreach ( ResearchNode child in Children )
                    {
                        MainTabWindow_ResearchTree.highlightedConnections.Add( new Pair<ResearchNode, ResearchNode>( this, child ) );
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
                GUI.color = DrawColor / 2f;
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
#if DEBUG
            TooltipHandler.TipRegion( Rect, Tree.DebugTip( this ) );
#endif
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
                var iconRect = new Rect( IconsRect.xMax - ( i + 1 ) * ( IconSize.x + 4f ),
                                         IconsRect.yMin + ( IconsRect.height - IconSize.y ) / 2f,
                                         IconSize.x,
                                         IconSize.y );

                if ( iconRect.xMin - IconSize.x < IconsRect.xMin &&
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
                        Queue.EnqueueRange( GetMissingRequiredRecursive().Concat( new List<ResearchNode>( new[] { this } ) ),
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
        public List<ResearchNode> GetMissingRequiredRecursive()
        {
            var parents = Research.prerequisites?.Where( rpd => !rpd.IsFinished ).Select( rpd => rpd.Node() );
            if (parents == null)
                return new List<ResearchNode>();
            var allParents = new List<ResearchNode>( parents );
            foreach ( ResearchNode parent in parents )
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
                foreach ( ResearchNode parent in Parents )
                    MainTabWindow_ResearchTree.highlightedConnections.Add( new Pair<ResearchNode, ResearchNode>( parent, this ) );
             
            if ( linkChildren )
                foreach ( ResearchNode child in Children )
                    MainTabWindow_ResearchTree.highlightedConnections.Add( new Pair<ResearchNode, ResearchNode>( this, child ) );

        }

        public List<ThingDef> MissingFacilities()
        {
            return MissingFacilities( Research );
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

        #region Overrides of Node

        public override string Label => Research.LabelCap;

        #endregion
    }
}
