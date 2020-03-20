using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResearchTree.Graph
{
    using FluffyResearchTree;
    using Verse;

    class FakeResearchNode : ResearchNode
    {
        public ResearchNode LinkedNode;
        public FakeResearchNode(ResearchNode node) : base(research: node.Research)
        {
            LinkedNode = node;
            
        }

        protected override void OnClick()
        {
            Tree.ActiveTree = Tree.Trees[LinkedNode.Research.tab.defName];
            Tree.ActiveTree.Initialize();
            MainTabWindow_ResearchTree.Instance.CenterOn(LinkedNode);

            
        }

        public FakeResearchNode(ResearchProjectDef research) : base(research)
        {
        }
    }
}
