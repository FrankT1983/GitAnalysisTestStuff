using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace TestGitClient
{
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
    }
}
