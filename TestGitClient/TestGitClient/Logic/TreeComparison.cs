using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            var toFindContentString = ContentString(toFind.node);
            var toFindFullString = toFind.node.ToFullString().Trim();
            {
                var identical = possibleMatches.Find(n => n.node.ToFullString().Trim().Equals(toFindFullString));
                if (identical != null)
                {
                    return new FindBelongingResult() { treeNode = identical, wasModified = false, howModified = ModificationKind.noModification };
                }

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
                        bool? haveSameName = HaveSameName(toFind.node.Kind(), s.node, toFind.node);
                        if (haveSameName == true || haveSameName == null)   // same name, or no name
                        {                                                       
                            return new FindBelongingResult() { treeNode = s, wasModified = !ContentString(s.node).Equals(toFindContentString), howModified = ModificationKind.contentChanged };
                        }
                    }
                }

                if (sameKind.Count == 1)
                {
                    var s = sameKind[0];                    
                    bool sameContent = ContentString(s.node).Equals(toFindContentString);
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

        private static string ContentString(SyntaxNode node)
        {
            if (node is MethodDeclarationSyntax)
            {
                var body = ((MethodDeclarationSyntax)node).Body;
                if (body != null)
                {
                    return body.ToFullString();
                }
                else
                {
                    return "";
                }
            }

            var b = new StringBuilder();            
            foreach(var c in node.ChildNodes())
            {
                if (c.IsKind(SyntaxKind.IdentifierName))
                { continue; }

                b.Append(c.ToFullString());
            }
            return b.ToString();
        }

       


        private static bool? HaveSameName(SyntaxKind syntaxKind, SyntaxNode node1, SyntaxNode node2)
        {            

            switch (syntaxKind)
            {
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ClassDeclaration:
                    {
                        var name1 = TreeHelper.GetName(node1);
                        var name2 = TreeHelper.GetName(node2);

                        if (name1 != null && name2 != null)
                        {
                            return name2.Equals(name1);
                        }
                    }
                    goto default;               

                case SyntaxKind.NamespaceDeclaration:
                default:
                    string n1 = GetIdentifyer(node1);
                    string n2 = GetIdentifyer(node2);

                    if (n1 == null || n2 == null) return null;
                    return n1.Equals(n2);                                    
            }            
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

    