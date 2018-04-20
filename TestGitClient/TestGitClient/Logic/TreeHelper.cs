using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TestGitClient
{
    public class TreeHelper
    {
        static public SyntakTreeDecorator SyntaxTreeFromString(string content)
        {
            return SyntaxTreeFromString(content, null, null, null);
        }

        private static int nodeId = 0;
        static private string NextGenericId()
        {
            var tmp = nodeId;
            nodeId++;
            return tmp.ToString();
        }

     
        static public SyntakTreeDecorator SyntaxTreeFromString(string content, Node fileNode, List<Node> allNodes, List<Edge> allEdges)
        {
            var foo = CSharpSyntaxTree.ParseText(content);


            var enrichedTree = new SyntakTreeDecorator();
            enrichedTree.node = foo.GetRoot();
            enrichedTree.equivilantGraphNode = new Node(NextGenericId(), Node.NodeType.Syntax, fileNode.Id) {SyntaxNode = foo.GetRoot() };
            nodeId++;
          

            var subEdges = new List<Edge>();
            var subNodes = new List<Node>();
            var childTrees = AddChildsToNodes(foo.GetRoot(), enrichedTree.equivilantGraphNode, subNodes, subEdges, 1);

            if (allNodes != null)
            {
                allNodes.Add(enrichedTree.equivilantGraphNode);
                allNodes.AddRange(subNodes);
            }
            if (allEdges != null)
            {
                allEdges.Add(new Edge(fileNode, enrichedTree.equivilantGraphNode, Edge.EdgeType.InFile));
                allEdges.AddRange(subEdges);
            }


            enrichedTree.childs.AddRange(childTrees);


            return enrichedTree;
        }

        static public List<SyntakTreeDecorator> AddChildsToNodes(SyntaxNode rootNode, Node parentGraphNode, List<Node> allNodes, List<Edge> allEdges, int currentLevel)
        {
            var kind = rootNode.Kind();
            // break early for stuff
            if (
                (kind == SyntaxKind.UsingDirective)
                || (kind == SyntaxKind.SimpleBaseType) || (kind == SyntaxKind.ParameterList) || (kind == SyntaxKind.ExpressionStatement) || (kind == SyntaxKind.LocalDeclarationStatement) || (kind == SyntaxKind.NotEqualsExpression)
                || (kind == SyntaxKind.EnumMemberDeclaration) || (kind == SyntaxKind.FieldDeclaration) || (kind == SyntaxKind.AttributeList)
                || (kind == SyntaxKind.ConstructorDeclaration) || (kind == SyntaxKind.MethodDeclaration) || (kind == SyntaxKind.DestructorDeclaration)
                //|| (kind == SyntaxKind.ClassDeclaration)
                || (kind == SyntaxKind.Block)
                )
            { return null; }

            var results = new List<SyntakTreeDecorator>();
            var childs = rootNode.ChildNodes().ToList();
            foreach (var c in childs)
            {

                var nodeName = GetName(c);
                var n = new Node(NextGenericId(), c.Kind().ToString(), c.Kind().ToString() + (nodeName!=null?" "+nodeName:""));
                var n2 = new SyntakTreeDecorator();
                n2.equivilantGraphNode = n;
                n2.node = c;                
                n.SyntaxNode = c;
                n.DistanceFromSyntaxRoot = currentLevel;             

                if (allNodes != null) { allNodes.Add(n); }
                if (parentGraphNode != null)
                {
                    switch (parentGraphNode.Type)
                    {
                        case Node.NodeType.Syntax:
                            allEdges.Add(new Edge(parentGraphNode, n, Edge.EdgeType.SyntaxHierarchialyAbove)); break;
                        case Node.NodeType.FileCS:
                            allEdges.Add(new Edge(parentGraphNode, n, Edge.EdgeType.InFile)); break;
                        default:
                            allEdges.Add(new Edge(parentGraphNode, n, Edge.EdgeType.Generic)); break;
                    }
                }

                var childtrees = AddChildsToNodes(c, n, allNodes, allEdges, currentLevel +1);
                if (childtrees != null) { n2.childs.AddRange(childtrees); }

                results.Add(n2);
            }
            return results;
        }

        static public SyntakTreeDecorator ParseCsFileAndAddToGraph(List<Node> allNodes, List<Edge> allEdges, Commit commit, string path, Node fileNode)
        {
            if (commit == null) { return null; }
            if (commit.Tree[path] == null) { return null; }

            var blob = commit.Tree[path].Target as Blob;
            if (blob == null)
            { return null; }

            var contentStream = blob.GetContentStream();
            using (var tr = new StreamReader(contentStream, Encoding.UTF8))
            {
                string content = tr.ReadToEnd();

                var subNodes = new List<Node>();
                var subEdges = new List<Edge>();
                var enrichedTree = SyntaxTreeFromString(content, fileNode, subNodes, subEdges);               
            
                
                allNodes.AddRange(subNodes);
                allEdges.AddRange(subEdges);

                return enrichedTree;
            }
        }

        public static string GetName(SyntaxNode node)
        {
            {
                var typed = node as Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax;
                if (typed != null)
                {
                    return typed.Identifier.Value.ToString();
                }
            }

            {
                var typed = node as Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax;
                if (typed != null)
                {
                    return typed.Identifier.Value.ToString();
                }
            }

            {
                var typed = node as Microsoft.CodeAnalysis.CSharp.Syntax.NamespaceDeclarationSyntax;
                if (typed != null)
                {
                    return typed.Name.ToString();
                }
            }

            {
                var typed = node as Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax;
                if (typed != null)
                {
                    return typed.ToFullString().Trim();
                }
            }

            return null;
        }

        public static string GetFullName(SyntaxNode node)
        {
            {
                var typed = node as Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax;
                if (typed != null)
                {
                    return typed.Identifier.Value.ToString();
                }
            }

            {
                var typed = node as Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax;
                if (typed != null)
                {
                    var methodName = GetName(node);
                    var classname = GetName(node.Parent);
                    return classname + "::" + methodName;
                }
            }

            {
                var typed = node as Microsoft.CodeAnalysis.CSharp.Syntax.NamespaceDeclarationSyntax;
                if (typed != null)
                {
                    return typed.Name.ToString();
                }
            }

            {
                var typed = node as Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax;
                if (typed != null)
                {
                    return typed.ToFullString().Trim();
                }
            }

            return null;
        }

    }
}

    