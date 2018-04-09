using System.Diagnostics;

namespace TestGitClient
{
    [DebuggerDisplay("{type} : {from} -> {to}")]
    public class Edge
    {
        public enum LinkType
        {
            Generic,
            HierarchialyAbove,
            NextCommit,
            Author,
            FileModification,
        }

        public Edge(Node f, Node t, LinkType linkType)
        {
            this.from = f;
            this.to = t;
            this.type = linkType;
        }

        public Edge(Node f, Node t) : this(f, t, LinkType.Generic)
        { }

        public Node from;
        public Node to;
        public LinkType type;
    }
}
