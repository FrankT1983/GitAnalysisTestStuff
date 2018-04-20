using System.Diagnostics;

namespace TestGitClient
{
    static class EdgeTypeHelpers
    {

        public static bool IsCodeModificationEdge(this Edge.EdgeType et)
        {
            return et == Edge.EdgeType.CodeChanged || et == Edge.EdgeType.CodeChangedRename || et == Edge.EdgeType.NoCodeChange;
        }
    }

    [DebuggerDisplay("{type} : {from} -> {to}")]
    public class Edge
    {
        public enum EdgeType
        {
            Generic,
            HierarchialyAbove,
            NextCommit,
            Author,
            FileModification,
            NoCodeChange,
            CodeChanged,
            CodeChangedRename,
            InFile,
            SyntaxHierarchialyAbove,
            SyntaxHierarchialyAboveOriginal,
        }

     

        public Edge(Node f, Node t, EdgeType edgeType)
        {
            this.from = f;
            this.to = t;
            this.type = edgeType;
        }

        public Edge(Node f, Node t) : this(f, t, EdgeType.Generic)
        { }

        public Node from;
        public Node to;
        public EdgeType type;
    }
}
