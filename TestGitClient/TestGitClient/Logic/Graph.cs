﻿using System.Collections.Generic;
using FPar.GEXF;
using System.Xml;
using System.Xml.Serialization;
using System;
using System.Linq;

namespace TestGitClient
{
    public class Graph
    {    
        public List<Node> Nodes { get; } = new List<Node>();

        public List<Edge> Edges { get; } = new List<Edge>();
            
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
                                                    value = (nc.FullContent != null) ? nc.FullContent : "",
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

        internal Graph GetConnectedSubGraph(Node n, IEnumerable<Edge.EdgeType> allowedConnections, bool biDirectional)
        {
            var nodesToCheck = new HashSet<Node>(this.Nodes);
            var edgesToCheck = new HashSet<Edge>(this.Edges);

            var workingSetNodes = new HashSet<Node>();
            var workingSetEdges = new List<Edge>();
            workingSetNodes.Add(n);
            nodesToCheck.Remove(n);
                        

            bool change = true;
            while(change)
            {
                change = false;
                IEnumerable<Edge> nextEdges;

                if (biDirectional)
                { nextEdges = edgesToCheck.Where(e => workingSetNodes.Contains(e.from) || workingSetNodes.Contains(e.to)); }
                else
                { nextEdges = edgesToCheck.Where(e => workingSetNodes.Contains(e.from)); }

                if (allowedConnections!= null)
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

            var result  = new Graph();
            result.Edges.AddRange(workingSetEdges);
            result.Nodes.AddRange(workingSetNodes);
            return result;
        }

        internal IEnumerable<Node> GetNeighborsOf(Node n)
        {
            var edges = GetEdgesFrom(n,this.Edges);
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
            return this.Nodes.FindAll( n => n.Type == searchtype);
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
    }
}
