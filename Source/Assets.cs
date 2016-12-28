// Karel Kroeze
// Assets.cs
// 2016-12-28

using UnityEngine;
using Verse;

namespace FluffyResearchTree
{
    [StaticConstructorOnStartup]
    public static class Assets
    {
        #region Fields

        public static Texture2D Button = ContentFinder<Texture2D>.Get( "button" );
        public static Texture2D ButtonActive = ContentFinder<Texture2D>.Get( "button-active" );
        public static Texture2D Circle = ContentFinder<Texture2D>.Get( "circle" );
        public static Texture2D End = ContentFinder<Texture2D>.Get( "end" );
        public static Texture2D EW = ContentFinder<Texture2D>.Get( "ew" );
        public static Texture2D MoreIcon = ContentFinder<Texture2D>.Get( "more" );
        public static Texture2D NS = ContentFinder<Texture2D>.Get( "ns" );
        public static Texture2D ResearchIcon = ContentFinder<Texture2D>.Get( "Research" );
        internal static readonly Texture2D CircleFill = ContentFinder<Texture2D>.Get( "circle-fill" );

        #endregion Fields
    }
}
