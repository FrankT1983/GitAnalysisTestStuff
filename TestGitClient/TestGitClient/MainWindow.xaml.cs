using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using FPar.GEXF;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace TestGitClient
{
    public class Graph
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

        private IList<Node> nodes = new List<Node>();
        private IList<Edge> edgedes = new List<Edge>();


        public void Serialize(string path)
        {
            var foo = new gexfcontent();
            foo.version = gexfcontentVersion.Item12;
            foo.graph = new graphcontent();

            string NodeTypeId = "NodeTypeAttribute";
            var attributes = new attributescontent
            {
                @class = classtype.node,
                attribute = new[]
                            {
                                new attributecontent
                                {
                                    id = NodeTypeId,
                                    title = "NodeType",
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
                                                }
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
                    convertedEdge.Add(blub);
                }
                edgesToSTore.edge = convertedEdge.ToArray();
                edgesToSTore.count = convertedEdge.Count.ToString();
            }

            foo.graph.Items = new object[3];
            foo.graph.Items[0] = attributes;
            foo.graph.Items[1] = nodesToStore;
            foo.graph.Items[2] = edgesToSTore;



            using (var file = XmlWriter.Create(path))
            {
                var serializer = new XmlSerializer(typeof(gexfcontent));
                serializer.Serialize(file, foo);
            }
        }

        internal void Add(List<Node> toAdd)
        {
            foreach (var n in toAdd)
            {
                this.nodes.Add(n);
            }
        }

        internal void Add(List<Edge> allLinks)
        {
            foreach (var n in allLinks)
            {
                this.edgedes.Add(n);
            }
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // todo: early test .... needs refactoring and seperation ov ViewModel and Model
        private void OnTry(object sender, RoutedEventArgs e)
        {
            var urlLink = this.UrlInput.Text;
            var lokalPath = this.LokalPath.Text;
            this.OutputBox.Children.Clear();


            bool usedLokal = false;
            try
            {
                usedLokal = OutputLokalRepo(lokalPath);
            }
            catch (Exception ex)
            {
                Output(ex.ToString());
            }


            if (!usedLokal)
            {
                // try opening
                try
                {
                    Output("Starting clone");
                    Repository.Clone(urlLink, lokalPath);
                    Output("Clone finished");
                    OutputLokalRepo(lokalPath);
                }
                catch (Exception ex)
                {
                    Output(ex.ToString());
                }
            }

            this.Close();
        }

        private bool OutputLokalRepo(string lokalPath)
        {
            bool usedLokal;
            using (var repo = new Repository(lokalPath))
            {                
                Output("found Repo:" + repo.Info.Path);
                usedLokal = true;
                //foreach (var c in repo.Commits)
                //{                    
                //    Output(c.MessageShort + " " + c.Tree.Count);
                //    foreach (var v in c.Tree)
                //    {
                //        Output("\t" + v.Name + " " + v.Target);

                //    }
                //}


                //repo.Diff.Compare()

                var graph = new Graph();


                var authorNodes = new Dictionary<string,Graph.Node>();
                var fileNodes = new Dictionary<string, Graph.Node>();
                var allNodes = new List<Graph.Node>();
                var allLinks = new List<Graph.Edge>();
                int i = 0;
                Graph.Node lastCommit = null;
                var commits = new List<Commit>();
                foreach (Commit commit in repo.Commits)
                {   commits.Add(commit);    }
                commits.Sort((a, b) => a.Author.When.CompareTo(b.Author.When));
                
                foreach (Commit commit in commits)
                {
                    Graph.Node authorNode = null;
                    if (!authorNodes.ContainsKey(commit.Author.Email))
                    {
                        authorNode = new Graph.Node(commit.Author.Name, Graph.Node.NodeType.Person, commit.Author.Name);
                        allNodes.Add(authorNode);
                        authorNodes.Add(commit.Author.Email, authorNode);
                    }
                    authorNode = authorNodes[commit.Author.Email];
                    
                    i++;
                    if (i > 5) break;

                    var commitNode = new Graph.Node(commit.Id.Sha, Graph.Node.NodeType.Commit, commit.Message);
                    allNodes.Add(commitNode);
                    allLinks.Add(new Graph.Edge(authorNode, commitNode, Graph.Edge.LinkType.Author));

                    if (lastCommit != null)
                    {
                        allLinks.Add(new Graph.Edge(lastCommit, commitNode, Graph.Edge.LinkType.NextCommit));
                    }
                    lastCommit = commitNode;

                    bool hasParrents = false;
                    foreach (var parent in commit.Parents)
                    {
                        hasParrents = true;
                        var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);
                        //Output(String.Format("{0} | {1}  ({2})", commit.Sha, commit.MessageShort , changes.Count));
                        foreach (TreeEntryChanges change in changes)
                        {
                            //Output(String.Format("{0} : {1}", change.Status, change.Path));

                            var fileNode = new Graph.Node(change.Path + commit.Id, Graph.Node.NodeType.File, change.Path);
                            if (!fileNodes.ContainsKey(change.Path))
                            {
                                fileNodes.Add(change.Path,fileNode);
                            }
                            else
                            {
                                var oldFileNode = fileNodes[change.Path];
                                allLinks.Add(new Graph.Edge(oldFileNode, fileNode,Graph.Edge.LinkType.FileModification));
                                fileNodes[change.Path] = fileNode;
                            }

                            allNodes.Add(fileNode);
                            allLinks.Add(new Graph.Edge(commitNode, fileNode, Graph.Edge.LinkType.HierarchialyAbove));

                            if (!change.Path.EndsWith(".cs")) { continue; }

                            fileNode.Type = Graph.Node.NodeType.FileCS;
                            ParseCsFileAndAddToGraph(allNodes, allLinks, commit, change.Path, fileNode);
                        }
                    }

                    if (!hasParrents)
                    {
                        var tree = commit.Tree;
                        foreach (var element in tree)
                        {
                            var fileNode = new Graph.Node(element.Path + commit.Id, Graph.Node.NodeType.File, element.Path);
                            fileNodes.Add(element.Path, fileNode);
                            allNodes.Add(fileNode);
                            allLinks.Add(new Graph.Edge(commitNode, fileNode, Graph.Edge.LinkType.HierarchialyAbove));

                            if (!element.Path.EndsWith(".cs")) { continue; }
                            fileNode.Type = Graph.Node.NodeType.FileCS;
                            ParseCsFileAndAddToGraph(allNodes, allLinks, commit, element.Path, fileNode);
                        }
                    }
                }

                graph.Add(allNodes);
                graph.Add(allLinks);

                graph.Serialize("Test.GEXF");

                Output("Finished");
            }

            return usedLokal;
        }

        private void ParseCsFileAndAddToGraph(List<Graph.Node> allNodes, List<Graph.Edge> allLinks, Commit commit, string path, Graph.Node fileNode)
        {
            var blob = commit.Tree[path].Target as Blob;
            if (blob != null)
            {
                var contentStream = blob.GetContentStream();
                using (var tr = new StreamReader(contentStream, Encoding.UTF8))
                {
                    string content = tr.ReadToEnd();
                    var foo = CSharpSyntaxTree.ParseText(content);

                    AddChildsToNodes(foo.GetRoot(), fileNode, allNodes, allLinks);
                }
            }
        }

        int nodeId = 0;

        private void AddChildsToNodes(SyntaxNode rootNode, Graph.Node fileNode, List<Graph.Node> allNodes, List<Graph.Edge> allLinks)
        {
            var kind = rootNode.Kind();
            // break early for stuff
            if (
                (kind == SyntaxKind.UsingDirective) 
                || (kind == SyntaxKind.SimpleBaseType) || (kind == SyntaxKind.ParameterList) || (kind == SyntaxKind.ExpressionStatement) || (kind == SyntaxKind.LocalDeclarationStatement) || (kind == SyntaxKind.NotEqualsExpression)
                || (kind == SyntaxKind.EnumMemberDeclaration) || (kind == SyntaxKind.FieldDeclaration)
                || (kind == SyntaxKind.ClassDeclaration)
                )
            {   return; }
            
            var childs = rootNode.ChildNodes();
            foreach(var c in childs)
            {                
                var n = new Graph.Node(nodeId.ToString(),c.Kind().ToString(),"test");
                n.FullContent = c.ToFullString().Trim();
                nodeId++;

                allNodes.Add(n);
                allLinks.Add(new Graph.Edge(fileNode, n, Graph.Edge.LinkType.Generic));

                AddChildsToNodes(c, n, allNodes, allLinks);
            }           
        }
             

        private void Output(string ex)
        {
            System.Console.WriteLine(ex);
            var lab = new TextBlock();
            lab.Text = ex;
            this.OutputBox.Children.Add(lab);
        }
    }
}
