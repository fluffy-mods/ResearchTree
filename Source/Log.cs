// Log.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Diagnostics;

namespace FluffyResearchTree
{
    public static class Log
    {
        public static void Message( string msg, params object[] args )
        {
            Verse.Log.Message( Format( msg, args ) );
        }

        public static void Warning( string msg, params object[] args )
        {
            Verse.Log.Message( Format( msg, args ) );
        }

        private static string Format( string msg, params object[] args )
        {
            return "ResearchTree :: " + String.Format( msg, args );
        }

        [Conditional("DEBUG")]
        public static void Debug( string msg, params object[] args )
        {
            Verse.Log.Message( Format( msg, args ) );
        }

        [Conditional( "TRACE" )]
        public static void Trace( string msg, params object[] args )
        {
            Verse.Log.Message( Format( msg, args ) );
        }
    }
}