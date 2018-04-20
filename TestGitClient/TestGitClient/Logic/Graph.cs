using System.Collections.Generic;
using FPar.GEXF;
using System.Xml;
using System.Xml.Serialization;
using System;
using System.Linq;
using LibGit2Sharp;

namespace TestGitClient
{
    public static class GraphTypeHelpers
    {
        public static List<Node> SinkVertexs(this Graph g)
        {
            HashSet<Node> result = new HashSet<Node>(g.Nodes);
            foreach (var e in g.Edges)
            {
                result.Remove(e.from);
            }
            return result.ToList();
        }

        private static void RemoveRange<T>(this List<T> removeFrom, List<T> toRemove)
        {
            foreach(T r in toRemove)
            {
                removeFrom.Remove(r);
            }
        }

        public static void TrimNoCodeChange(this Graph graph)
        {
            var syntaxNodes = graph.Nodes.Where(n => n.Type == Node.NodeType.Syntax).ToList();
            var noChangeEdges = syntaxNodes.AsParallel().SelectMany(sn => graph.Edges.Where(e => e.from == sn && e.type == Edge.EdgeType.NoCodeChange)).ToList();

            var maxLevel = syntaxNodes.Max(n => n.DistanceFromSyntaxRoot);

            int currentLevel = 0;
            do
            {
                Edge noChangeEdge;
                {
                    noChangeEdge = noChangeEdges.FirstOrDefault(e => e.from.DistanceFromSyntaxRoot == currentLevel);
                    if (noChangeEdge == null)
                    {
                        if ( !noChangeEdges.Any())
                        {   break;  }

                        if (currentLevel < maxLevel)
                        {
                            currentLevel++;
                            continue;
                        }
                        else
                        {
                            noChangeEdge = noChangeEdges.FirstOrDefault();
                            if (noChangeEdge == null)
                            { break; }
                        }
                    }
                }              

                var toRemove = noChangeEdge.to;
                var toRemoveOutGoing = graph.GetEdgesFrom(toRemove).Where(e => e.type == Edge.EdgeType.CodeChanged || e.type == Edge.EdgeType.NoCodeChange).ToList();

                var toRemoveIngoing = graph.Edges.Where(e => e.to == toRemove).ToList();

                // add a OriginalSub
                // SyntaxHierarchialAboveOrig

                // simply drop subtree ?  --> kind of want to 
                var subTree = graph.GetConnectedSubGraph(toRemove, new Edge.EdgeType[] { Edge.EdgeType.SyntaxHierarchialyAbove } , false, false);

                foreach (var ingoing in toRemoveIngoing)
                {
                    switch (ingoing.type)
                    {
                        case Edge.EdgeType.SyntaxHierarchialyAbove:
                            graph.Edges.Add(new Edge(ingoing.from ,noChangeEdge.from, Edge.EdgeType.SyntaxHierarchialyAboveOriginal));
                            break;
                        case Edge.EdgeType.InFile:
                        case Edge.EdgeType.NoCodeChange:
                            // do nothing
                            break;
                        default:
                            DebugHelper.BreakIntoDebug();
                            break;
                    }
                }

                foreach (var e in toRemoveOutGoing)
                {                  
                    graph.Edges.Add(new Edge(noChangeEdge.from, e.to, e.type));
                    graph.Edges.Remove(e);

                    if (e.type == Edge.EdgeType.NoCodeChange)
                    {
                        noChangeEdges.Add(new Edge(noChangeEdge.from, e.to, e.type));
                    }
                }                                              

                graph.Edges.RemoveAll(e => e.from == toRemove || e.to == toRemove);
                noChangeEdges.RemoveAll(e => e.from == toRemove || e.to == toRemove);

                graph.Nodes.RemoveRange(subTree.Nodes);
                syntaxNodes.RemoveRange(subTree.Nodes);
                graph.Nodes.Remove(toRemove);
                syntaxNodes.Remove(toRemove);
            } while (true);
        }
    }

    public class Graph
    {
        public List<Node> Nodes { get; } = new List<Node>();

        public List<Edge> Edges { get; } = new List<Edge>();


        public void Serialize2(string path)
        {
            string sep = "||";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(path))
            {
                foreach(var n in this.Nodes)
                {
                    file.WriteLine("n"+sep + n.Id + sep + n.LongId + sep + n.Type + sep);
                }

                foreach (var e in this.Edges)
                {
                    file.WriteLine("e" +sep + e.from.Id + sep + e.from.LongId + sep + e.to.Id + sep + e.to.LongId + sep + e.type );
                }
            }
        }

        public void Serialize(string path)
        {
            var foo = new gexfcontent();
            foo.version = gexfcontentVersion.Item12;
            foo.graph = new graphcontent();

            string NodeTypeId = "NodeTypeAttribute";
            string NodeTypeFullContent = "NodeFullContent";
            var nodeAttributes = new attributescontent
            {
                @class = classtype.node,
                attribute = new[]
                            {
                                new attributecontent
                                {
                                    id = NodeTypeId,
                                    title = "NodeType",
                                    type = attrtypetype.@string
                                },
                                  new attributecontent
                                {
                                    id = NodeTypeFullContent,
                                    title = "FullContent",
                                    type = attrtypetype.@string
                                },
                            }
            };

            string EdgeTypeId = "EdgeTypeAttribute";
            var edgeAttributes = new attributescontent
            {
                @class = classtype.edge,
                attribute = new[]
                            {
                                new attributecontent
                                {
                                    id = EdgeTypeId,
                                    title = "EdgeType",
                                    type = attrtypetype.@string
                                }
                            }
            };

            var nodesToStore = new nodescontent();

            {
                List<nodecontent> convertedNodes = new List<nodecontent>();
                foreach (var nc in this.Nodes)
                {
                    var node = new nodecontent();
                    node.id = nc.Id;
                    node.label = nc.Type + " : " + nc.Id;
                    if (nc.Type == Node.NodeType.Syntax)
                    {
                        node.label = nc.Type + " : " + nc.Content;
                    }

                    node.Items = new object[]
                                    {
                                        new attvaluescontent
                                        {
                                            attvalue = new[]
                                            {
                                                new attvalue
                                                {
                                                    @for = NodeTypeId,
                                                    value = (nc.Type != Node.NodeType.Syntax ) ? nc.Type.ToString() : nc.Type.ToString() + " " + nc.SyntaxType
                                                },
                                                new attvalue
                                                {
                                                    @for = NodeTypeFullContent,
                                                    value =(nc.Type != Node.NodeType.Syntax ) ? nc.FullContent : nc.getSyntaxFullContent(),
                                                },
                                            }
                                        }
                                    };

                    convertedNodes.Add(node);
                }
                nodesToStore.node = convertedNodes.ToArray();
                nodesToStore.count = convertedNodes.Count.ToString();
            }

            int i = 0;
            var edgesToSTore = new edgescontent();
            {
                List<edgecontent> convertedEdge = new List<edgecontent>();
                foreach (var e in this.Edges)
                {
                    var blub = new edgecontent();
                    blub.id = i.ToString(); i++;
                    blub.source = e.from.Id;
                    blub.target = e.to.Id;
                    blub.Items = new object[]
                                   {
                                        new attvaluescontent
                                        {
                                            attvalue = new[]
                                            {
                                                new attvalue
                                                {
                                                    @for = EdgeTypeId,
                                                    value = e.type.ToString()
                                                }
                                            }
                                        }
                                   };
                    convertedEdge.Add(blub);
                }
                edgesToSTore.edge = convertedEdge.ToArray();
                edgesToSTore.count = convertedEdge.Count.ToString();
            }

            foo.graph.Items = new object[4];
            foo.graph.Items[0] = nodeAttributes;
            foo.graph.Items[1] = edgeAttributes;
            foo.graph.Items[2] = nodesToStore;
            foo.graph.Items[3] = edgesToSTore;

            using (var file = XmlWriter.Create(path))
            {
                var serializer = new XmlSerializer(typeof(gexfcontent));
                serializer.Serialize(file, foo);
            }
        }

        internal IEnumerable<Graph> GetConnectedSubGraphs( bool biDirectional, bool inverseOder)
        {
            var remainingNodes = new HashSet<Node>(this.Nodes);
            var remainingEdges = new HashSet<Edge>(this.Edges);

            while(remainingNodes.Any())
            {
                var n = remainingNodes.FirstOrDefault();

                var ng = GetConnectedSubGraph(remainingNodes, remainingEdges, n, null, biDirectional, inverseOder);
                yield return ng;           
            }
        }

        internal Graph GetConnectedSubGraph(Node n, IEnumerable<Edge.EdgeType> allowedConnections, bool biDirectional, bool inverseOder)
        {
            var nodesToCheck = new HashSet<Node>(this.Nodes);
            var edgesToCheck = new HashSet<Edge>(this.Edges);

            return GetConnectedSubGraph(nodesToCheck, edgesToCheck, n, allowedConnections, biDirectional, inverseOder);

        }

        internal Graph GetConnectedSubGraph(HashSet<Node>  nodes, HashSet<Edge> edges, Node n, IEnumerable<Edge.EdgeType> allowedConnections, bool biDirectional, bool inverseOder)
        {
            var nodesToCheck = nodes;
            var edgesToCheck = edges;

            var workingSetNodes = new HashSet<Node>();
            var workingSetEdges = new List<Edge>();
            workingSetNodes.Add(n);
            nodesToCheck.Remove(n);

            bool change = true;
            while (change)
            {
                change = false;
                IEnumerable<Edge> nextEdges;

                if (biDirectional)
                { nextEdges = edgesToCheck.Where(e => workingSetNodes.Contains(e.from) || workingSetNodes.Contains(e.to)); }
                else
                {
                    if (!inverseOder)
                    {
                        nextEdges = edgesToCheck.Where(e => workingSetNodes.Contains(e.from));
                    }
                    else
                    {
                        nextEdges = edgesToCheck.Where(e => workingSetNodes.Contains(e.to));
                    }
                }

                if (allowedConnections != null)
                {
                    IEnumerable<Edge> filterd = nextEdges.Where(e => allowedConnections.Contains(e.type));
                    nextEdges = filterd;
                }

                nextEdges = nextEdges.ToList(); // collaps link enumerable
                foreach (var foo in nextEdges) { edgesToCheck.Remove(foo); }
                workingSetEdges.AddRange(nextEdges);

                foreach (var foo in nextEdges)
                {
                    if (!workingSetNodes.Contains(foo.from))
                    {
                        workingSetNodes.Add(foo.from);
                        nodesToCheck.Remove(foo.from);
                        change = true;
                    }

                    if (!workingSetNodes.Contains(foo.to))
                    {
                        workingSetNodes.Add(foo.to);
                        nodesToCheck.Remove(foo.to);
                        change = true;
                    }
                }
            }

            var result = new Graph();
            result.Edges.AddRange(workingSetEdges);
            result.Nodes.AddRange(workingSetNodes);
            return result;
        }

        internal IEnumerable<Node> GetNeighborsOf(Node n)
        {
            var edges = GetEdgesFrom(n, this.Edges);
            return GetDestinationNodes(edges);
        }

        internal IEnumerable<Node> GetNeighborsOf(Node n, Node.NodeType type)
        {
            var edges = GetEdgesFrom(n, this.Edges);
            return GetDestinationNodes(edges, type);
        }

        private IEnumerable<Node> GetDestinationNodes(IEnumerable<Edge> list)
        {
            return list.Select(e => e.to);
        }

        private IEnumerable<Node> GetDestinationNodes(IEnumerable<Edge> list, Node.NodeType type)
        {
            foreach (var e in list)
            {
                if (e.to.Type == type)
                {
                    yield return e.to;
                }
            }
        }


        internal IEnumerable<Edge> GetEdgesFrom(Node n)
        {
            return GetEdgesFrom(n, this.Edges);
        }

        static internal IEnumerable<Edge> GetEdgesFrom(Node n, List<Edge> edges)
        {
            return edges.Where(e => e.from.Equals(n));
        }

        static internal List<Edge> GetEdgesTo(Node n, List<Edge> edges)
        {
            return edges.FindAll(e => e.to.Equals(n));
        }

        internal List<Edge> GetEdgesFrom(Node n, Edge.EdgeType edgeType)
        {
            return this.Edges.FindAll(e => e.type.Equals(edgeType) && e.from.Equals(n));
        }

        internal List<Node> GetNodesOfType(Node.NodeType searchtype)
        {
            return this.Nodes.FindAll(n => n.Type == searchtype);
        }

        internal void Add(List<Node> toAdd)
        {
            foreach (var n in toAdd)
            {
                this.Nodes.Add(n);
            }
        }

        internal void Add(List<Edge> allEdges)
        {
            foreach (var n in allEdges)
            {
                this.Edges.Add(n);
            }
        }

        private Repository repository;
        internal void SetRepository(Repository repo)
        {
            this.repository = repo;
        }
    }
}
