﻿using System.Diagnostics;

namespace TestGitClient
{

    [DebuggerDisplay("{Type} : {Content}")]
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

        public Node(string nodeId, NodeType nodeType, string content)
        {
            this.Type = nodeType;
            this.Id = nodeId;
            this.Content = content;
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
