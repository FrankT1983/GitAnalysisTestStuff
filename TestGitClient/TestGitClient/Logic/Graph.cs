using System.Collections.Generic;
using FPar.GEXF;
using System.Xml;
using System.Xml.Serialization;
using System;

namespace TestGitClient
{
    public class Graph
    {

        private List<Node> nodes = new List<Node>();
        private List<Edge> edgedes = new List<Edge>();


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
                foreach (var nc in this.nodes)
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
                foreach (var e in this.edgedes)
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

        internal List<Edge> GetEdgesFrom(Node n)
        {
            return this.edgedes.FindAll(e => e.from.Equals(n));
        }

        internal List<Edge> GetEdgesFrom(Node n, Edge.EdgeType edgeType)
        {
            return this.edgedes.FindAll(e => e.type.Equals(edgeType) && e.from.Equals(n));
        }

        internal List<Node> GetNodesOfType(Node.NodeType searchtype)
        {                                 
            return this.nodes.FindAll( n => n.Type == searchtype);
        }

        internal void Add(List<Node> toAdd)
        {
            foreach (var n in toAdd)
            {
                this.nodes.Add(n);
            }
        }

        internal void Add(List<Edge> allEdges)
        {
            foreach (var n in allEdges)
            {
                this.edgedes.Add(n);
            }
        }
    }
}
