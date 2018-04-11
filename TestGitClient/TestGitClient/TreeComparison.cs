using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TestGitClient
{
    [Flags]
    public enum ModificationKind
    {        
        noModification = 0,
        contentChanged =1,
        nameChanged = 2,
    }

    public class FindBelongingResult
    {     

        public SyntakTreeDecorator treeNode;
        public bool wasModified;

        public ModificationKind howModified;
    }


    public class TreeHelper
    {
        private static int nodeId = 0;
        static public SyntakTreeDecorator SyntaxTreeFromString(string content, Node fileNode, List<Node> allNodes, List<Edge> allLinks)
        {
            var foo = CSharpSyntaxTree.ParseText(content);

            var subEdges = new List<Edge>();
            var subNodes = new List<Node>();
            var childTrees = AddChildsToNodes(foo.GetRoot(), fileNode, subNodes, subEdges);

            var enrichedTree = new SyntakTreeDecorator();
            enrichedTree.node = foo.GetRoot();
            enrichedTree.equivilantGraphNode = fileNode;
            enrichedTree.childs.AddRange(childTrees);
            return enrichedTree;
        }

        static public List<SyntakTreeDecorator> AddChildsToNodes(SyntaxNode rootNode, Node fileNode, List<Node> allNodes, List<Edge> allLinks)
        {
            var kind = rootNode.Kind();
            // break early for stuff
            if (
                (kind == SyntaxKind.UsingDirective)
                || (kind == SyntaxKind.SimpleBaseType) || (kind == SyntaxKind.ParameterList) || (kind == SyntaxKind.ExpressionStatement) || (kind == SyntaxKind.LocalDeclarationStatement) || (kind == SyntaxKind.NotEqualsExpression)
                || (kind == SyntaxKind.EnumMemberDeclaration) || (kind == SyntaxKind.FieldDeclaration)
                || (kind == SyntaxKind.ClassDeclaration)
                )
            { return null; }

            var results = new List<SyntakTreeDecorator>();
            var childs = rootNode.ChildNodes();
            foreach (var c in childs)
            {
                var n = new Node(nodeId.ToString(), c.Kind().ToString(), c.Kind().ToString());
                var n2 = new SyntakTreeDecorator();
                n2.equivilantGraphNode = n;
                n2.node = c;

                n.FullContent = c.ToFullString().Trim();
                n.SyntaxNode = c;
                nodeId++;

                if (allNodes != null) { allNodes.Add(n); }
                if (fileNode != null) { allLinks.Add(new Edge(fileNode, n, Edge.LinkType.Generic)); }

                var childtrees = AddChildsToNodes(c, n, allNodes, allLinks);
                if (childtrees != null) { n2.childs.AddRange(childtrees); }

                results.Add(n2);
            }
            return results;
        }

        static public SyntakTreeDecorator ParseCsFileAndAddToGraph(List<Node> allNodes, List<Edge> allLinks, Commit commit, string path, Node fileNode)
        {
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
                allLinks.AddRange(subEdges);

                return enrichedTree;
            }
        }

    }


    public class TreeComparison
    {
        public static FindBelongingResult FindBelongingThing(SyntakTreeDecorator toFind, SyntakTreeDecorator possibleMatches)
        {
            var singleItemList = new List<SyntakTreeDecorator>();
            singleItemList.Add(possibleMatches);
            return FindBelongingThing(toFind, singleItemList);
        }

        public static FindBelongingResult FindBelongingThing(SyntakTreeDecorator toFind, List<SyntakTreeDecorator> possibleMatches)
        {
            {
                var equiv = possibleMatches.Find(n => n.node.IsEquivalentTo(toFind.node));
                if (equiv != null)
                {
                    return new FindBelongingResult() { treeNode = equiv, wasModified = false , howModified = ModificationKind.noModification};
                }
            }


            {
                var sameKind = possibleMatches.FindAll(n => n.node.Kind().Equals(toFind.node.Kind()));
                if (sameKind != null)
                {
                    foreach (var s in sameKind)
                    {
                        if (AreRoughlyEquivilant(toFind.node.Kind(), s.node, toFind.node))
                        {                                                       
                            return new FindBelongingResult() { treeNode = s, wasModified = s.node.ToFullString().Equals(toFind.node.ToFullString()) , howModified = ModificationKind.contentChanged };
                        }
                    }
                }

                if (sameKind.Count == 1)
                {
                    var s = sameKind[0];                    
                    bool sameContent = s.node.ToFullString().Equals(toFind.node.ToFullString());
                    ModificationKind how = ModificationKind.nameChanged;
                    if (!sameContent)
                    {
                        how = how | ModificationKind.contentChanged;
                    }
                            
                    return new FindBelongingResult() { treeNode =s, wasModified = true, howModified = how };
                }

            }




            return null;
        }

        private static bool AreRoughlyEquivilant(SyntaxKind syntaxKind, SyntaxNode node1, SyntaxNode node2)
        {
            switch (syntaxKind)
            {
                case SyntaxKind.NamespaceDeclaration:
                    string n1 = GetIdentifyer(node1);
                    string n2 = GetIdentifyer(node2);
                    return n1.Equals(n2);

                    

                default:
                    return false;

            }
            throw new NotImplementedException();
        }

        private static string GetIdentifyer(SyntaxNode node)
        {
            foreach (var c in node.ChildNodes())
            {
                if (c.IsKind(SyntaxKind.IdentifierName))
                {
                    return c.ToFullString().Trim();


                }
            }

            return null;
        }
    }
}

    