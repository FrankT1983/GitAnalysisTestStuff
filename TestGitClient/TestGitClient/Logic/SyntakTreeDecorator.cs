using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace TestGitClient
{
    public class SyntakTreeDecorator
    {
        public SyntaxNode node;
        public Node equivilantGraphNode;

        public List<SyntakTreeDecorator> childs = new List<SyntakTreeDecorator>();               
    }
}
