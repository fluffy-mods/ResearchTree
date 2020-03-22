// Constants.cs
// Copyright Karel Kroeze, 2018-2020

using UnityEngine;
using Verse;

namespace FluffyResearchTree
{
    public static class Constants
    {
        public const           double  Epsilon                     = 1e-4;
        public const           float   HubSize                     = 16f;
        public const           float   DetailedModeZoomLevelCutoff = 1.5f;
        public const           float   Margin                      = 6f;
        public const           float   QueueLabelSize              = 30f;
        public const           float   SmallQueueLabelSize         = 20f;
        public const           float   AbsoluteMaxZoomLevel        = 3f;
        public const           float   ZoomStep                    = .05f;
        public static readonly IntVec2 IconSize                    = new IntVec2( 18, 18 );
        public static readonly IntVec2 NodeMargins                 = new IntVec2( 50, 10 );
        public static readonly IntVec2 NodeSize                    = new IntVec2( 200, 50 );
        public static readonly float   TopBarHeight                = NodeSize.z + Margin * 2;
    }
}