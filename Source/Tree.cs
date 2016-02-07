using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityCoreLibrary.ColorPicker;
using UnityEngine;
using Verse;

namespace FluffyResearchTree
{
    public class Tree
    {
        public string Genus;
        public List<Node> Trunk;
        public List<Node> Leaves;
        public int MinDepth;
        public int MaxDepth;
        public int Width;
        public int StartY;
        private Color _color;
        public Color GreyedColor;
        public Color MediumColor;

        public Color Color
        {
            get { return _color; }
            set
            {
                _color = value;

                float h, s, v;

                ColorHelper.RGBtoHSV( value, out h, out s, out v );
                GreyedColor = ColorHelper.HSVtoRGB( h, 0.1f, 0.25f );
                MediumColor = ColorHelper.HSVtoRGB( h, 0.7f, 0.8f );
            }
        }

        public Tree( string genus, List<Node> trunk )
        {
            Genus = genus;
            Trunk = trunk.OrderBy( node => node.Depth ).ToList();
            Leaves     = new List<Node>();

            if ( trunk.Any() )
            {
                MinDepth = trunk.Select( node => node.Depth ).Min();
                MaxDepth = trunk.Select( node => node.Depth ).Max();
                Width = 1;
            }
            else
            {
                MinDepth = MaxDepth = Width = 0;
            }

            // make all Trunk nodes a part of this Tree
            foreach ( Node node in trunk )
            {
                node.Tree = this;
            }
        }

        public List<Node> Children( int depth = 2 )
        {
            List<Node> children = new List<Node>( Trunk );
            List<Node> curLevel = new List<Node>( Trunk );

            while ( depth -- > 0 )
            {
                curLevel = curLevel.SelectMany( node => node.Children ).Distinct().ToList();
                children.AddRange( curLevel );
            }

            return children;
        }

        public List<Node> Parents( int depth = 2 )
        {
            List<Node> parents = new List<Node>( Trunk );
            List<Node> curLevel = new List<Node>( Trunk );

            while( depth-- > 0 )
            {
                curLevel = curLevel.SelectMany( node => node.Parents ).Distinct().ToList();
                parents.AddRange( curLevel );
            }

            return parents;
        }

        public float AffinityWith( Tree otherTree )
        {
            // get the number of relations between the two extended families.
            List<Node> family = new List<Node>();
            family.AddRange( Leaves );
            family.AddRange( Trunk );
            List<Node> otherFamily = otherTree.Children().Concat( otherTree.Parents() ).Distinct().ToList();

            // count of nodes that are a member of both families, divided by family size to get small child trees to be closer to 'main' tree.
            return (float)family.Intersect( otherFamily ).Count() / (float)Math.Sqrt( otherFamily.Count() );
        }

        public void AddLeaf( Node leaf )
        {
            // add it
            Leaves.Add( leaf );

            // mark it a part of this Tree
            leaf.Tree = this;

            // update depths and Width if necessary
            Width = Math.Max( Width, NodesAtDepth( leaf.Depth, true ).Count );
            MinDepth = Math.Min( MinDepth, leaf.Depth );
            MaxDepth = Math.Max( MaxDepth, leaf.Depth );
        }

        public List<Node> NodesAtDepth( int depth, bool includeTrunk = false )
        {
            List<Node> nodes = new List<Node>();
            if (includeTrunk) nodes.AddRange( Trunk.Where( node => node.Depth == depth ) );
            nodes.AddRange( Leaves.Where( node => node.Depth == depth ) );
            return nodes;
        }

        public override string ToString()
        {
            StringBuilder text = new StringBuilder();

            text.AppendLine( Genus.ToUpper() );
            text.AppendLine( "Trunk:" );
            foreach ( Node node in Trunk )
            {
                text.AppendFormat( node.ToString() );
            }

            text.AppendLine( "\n\nLeaves:" );
            foreach( Node node in Leaves )
            {
                text.AppendFormat( node.ToString() + ", " );
            }

            text.AppendLine( "\n\nAffinities:" );
            foreach ( Tree tree in ResearchTree.Trees )
            {
                if ( tree != this )
                {
                    text.AppendLine( tree.Genus + ": " + AffinityWith( tree ) );
                }
            }

            return text.ToString();
        }
    }
}