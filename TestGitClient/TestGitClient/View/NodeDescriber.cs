using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGitClient.View
{
    internal class NodeDescriber
    {
        internal static string Decribe(Node n)
        {
            switch(n.Type)
            {
                case Node.NodeType.Syntax:
                    switch (n.SyntaxType)
                    {
                        case "MethodDeclaration":
                            var name = TreeHelper.GetFullName(n.SyntaxNode);
                            
                            if (name != null)
                            {
                                return "Methode " + name;
                            }

                            break;
                    }

                    break;                
            }
            return n.Content;
        }
    }
}
