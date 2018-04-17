using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TestGitClient
{
    static class NodeTypeHelpers
    {

        public static bool IsMethodDeclartionNode(this Node et)
        {
            return et.Type == Node.NodeType.Syntax && et.SyntaxNode != null && et.SyntaxNode.Kind() == SyntaxKind.MethodDeclaration;
        }
    }

    [DebuggerDisplay("{Type} : {Content} ({Id})")]
    public class Node
    {
        public enum NodeType
        {
            Commit,
            File,
            FileCS,
            Syntax,
            Person,
        }

        public NodeType Type { get; set; }
        public string Id { get; set; }
        public string SyntaxType { get; set; }
        public string Content { get; set; }
        public string FullContent { get; set; }
        public SyntaxNode SyntaxNode { get; internal set; }

        public Node(string nodeId, NodeType nodeType, string content) : this(nodeId, nodeType, content, "")
        {          
        }

        public Node(string nodeId, NodeType nodeType, string content, string fullContent)
        {
            this.Type = nodeType;
            this.Id = nodeId;
            this.Content = content;
            this.FullContent = fullContent;
        }

        public Node(string nodeId, string nodeType, string content)
        {
            this.Type = NodeType.Syntax;
            this.Id = nodeId;
            this.Content = content;
            this.SyntaxType = nodeType;            
        }

        internal string getSyntaxFullContent()
        {
            if (this.SyntaxNode == null)
            { return ""; }

            return this.SyntaxNode.ToFullString();            
        }
    }
}
