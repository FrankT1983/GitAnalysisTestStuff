using LibGit2Sharp;
using System;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Controls;

using TestGitClient.Logic;
using System.Windows.Controls.Primitives;
using QuickGraph;

using System.ComponentModel;
using System.Windows.Media;
using GraphSharp.Controls;
using System.Linq;
using System.Windows.Input;
using TestGitClient.View;
using Microsoft.CodeAnalysis.CSharp;

namespace TestGitClient
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Graph currentGraph;
        Dictionary<Node, DisplayNode> DisplayedNodes = new Dictionary<Node, DisplayNode>();
        private Node nodeInEditor = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        // todo: early test .... needs refactoring and seperation ov ViewModel and Model
        // todo: this should happen asyncronously
        private void OnTry(object sender, RoutedEventArgs e)
        {
            var url = this.UrlInput.Text;
            var lokalPath = this.LokalPath.Text;
            this.OutputBox.Children.Clear();

            Graph g = null;
            try
            { g = GraphFactory.GraphFromRepoFolder(lokalPath); }
            catch (Exception ex)
            { Output(ex.ToString()); }

            if (g == null)
            {
                // try opening
                try
                {
                    Output("Starting clone");
                    Repository.Clone(url, lokalPath);
                    Output("Clone finished");
                    g = GraphFactory.GraphFromRepoFolder(lokalPath);
                }
                catch (Exception ex)
                {
                    Output(ex.ToString());
                }
            }

            if (g != null)
            {
                Output("Generated graph with " + g.Nodes.Count + " Nodes and " + g.Edges.Count + " Edges");                     
                g.Serialize("Test.GEXF");
                g.Serialize2("Test.txt");
                currentGraph = g;
                var commitNodes = g.GetNodesOfType(Node.NodeType.Commit);
                this.CommitPane.Children.Clear();
                foreach (var n in commitNodes)
                {
                    var button = new ToggleButton();
                    button.Content = n.Content;
                    button.Click += (o, i) => this.CommitButtonPressed(o, n);
                    this.CommitPane.Children.Add(button);
                }
            }
        }

        private void CommitButtonPressed(object o, Node n)
        {
            HighlightSingleToggleButton(o);

            this.GraphDisplay.Graph = AddNodeToDisplay(n);
            ViewCode(this.CodeDisplay, n);

            CommitDescription.Text = "";
            if (n.Type == Node.NodeType.Commit)
            {
                BuildFileTreeForNode(n);

                var changesInThisCommit = currentGraph.GetConnectedSubGraph(n, new Edge.EdgeType[] { Edge.EdgeType.SyntaxHierarchialyAbove, Edge.EdgeType.HierarchialyAbove, Edge.EdgeType.InFile }, false);

                foreach (var node in changesInThisCommit.Nodes)
                {
                    switch (node.Type)
                    {
                        case Node.NodeType.Commit:
                            CommitDescription.Text += " Summery for commit " + node.Content + "\n";
                            break;

                        case Node.NodeType.Syntax:

                            {
                                var toNode = currentGraph.Edges.Where(e => e.to == node && e.type.IsCodeModificationEdge());

                                if (toNode.Any())
                                {
                                    CommitDescription.Text += " Changed " + NodeDescriber.Decribe(node)+ "\n";
                                }
                                else
                                {
                                    CommitDescription.Text += " Created " + NodeDescriber.Decribe(node) + "\n";
                                }
                            }
                            break;
                        default: break;
                    }                    
                }
            }
        }

        private void HighlightSingleToggleButton(object o)
        {
            if (o is ToggleButton)
            {
                foreach (var k in this.CommitPane.Children)
                {
                    if (k != o && k is ToggleButton)
                    {
                        ((ToggleButton)k).IsChecked = false;
                    }
                }
                 ((ToggleButton)o).IsChecked = true;
            }
        }

        private void BuildFileTreeForNode(Node n)
        {
            var filenodes = currentGraph.GetNeighborsOf(n, Node.NodeType.FileCS);

            FileTree.Items.Clear();
            var commitItem = new TreeViewItem() { Header = n.Id };
            FileTree.Items.Add(commitItem);

            foreach (var f in filenodes)
            {
                var item = new TreeViewItem();
                item.Header = f.Content;
                commitItem.Items.Add(item);
            }
            commitItem.ExpandSubtree();
        }

        private void ViewCode(VirtualizingStackPanel codeDisplay, Node node)
        {
            codeDisplay.Children.Clear();
            if (node.Type == Node.NodeType.Syntax)
            {

                var parents = this.currentGraph.GetConnectedSubGraph(node, new Edge.EdgeType[] { Edge.EdgeType.SyntaxHierarchialyAbove }, true).Nodes;
                parents.Sort((a, b) => a.SyntaxNode.SpanStart.CompareTo(b.SyntaxNode.SpanStart));

                int lineOffset = node.SyntaxNode.SpanStart;

                var lines = node.FullContent.Split('\n');
                int lineNumber = 0;
                foreach (var l in lines)
                {
                    var aboveThisLine = parents.Where(n => lineOffset >= n.SyntaxNode.SpanStart && lineOffset < n.SyntaxNode.Span.End).ToList();



                    var pane = new StackPanel() { Orientation = Orientation.Horizontal };
                    var lab = new TextBlock();
                    lab.Text = l;


                    for (int i = 0; i < aboveThisLine.Count(); i++)
                    {
                        var btn = new Button();
                        btn.Width = 5;
                        btn.ToolTip = aboveThisLine[i].Content;

                        pane.Children.Add(btn);
                    }

                    var lineNumberLabel = new TextBlock();
                    lineNumberLabel.Text = lineNumber.ToString();
                    lineNumberLabel.Margin = new Thickness(5, 0, 5, 0);
                    pane.Children.Add(lineNumberLabel);
                    pane.Children.Add(lab);

                    codeDisplay.Children.Add(pane);

                    pane.MouseEnter += (s, e) => this.ModifiyGraphForCodeLineHovering(aboveThisLine, true);
                    pane.MouseLeave += (s, e) => this.ModifiyGraphForCodeLineHovering(aboveThisLine, false);

                    lineNumber++;
                    lineOffset += l.Length + 1; // + linebreak?
                }
            }
        }

        private void ModifiyGraphForCodeLineHovering(List<Node> aboveList, bool enter)
        {
            //HighlightNode(aboveList.Last(), enter);
            foreach (var toModify in aboveList)
            {
                HighlightNode(toModify, enter);
            }
        }

        private void HighlightNode(Node node, bool setHighlight)
        {
            if (node == null) return;
            var display = this.DisplayedNodes[node];
            if (display == null) return;

            display.Highlight = setHighlight;
        }

        private BidirectionalGraph<object, IEdge<object>> AddNodeToDisplay(Node n)
        {
            var g = new BidirectionalGraph<object, IEdge<object>>();
            var dn = new DisplayNode(n);
            g.AddVertex(dn);

            DisplayedNodes.Clear();

            DisplayedNodes.Add(n, dn);
            var edges = currentGraph.GetEdgesFrom(n, Edge.EdgeType.HierarchialyAbove);
            while (edges.Count > 0)
            {
                var nextNodes = new List<Node>();
                foreach (var e in edges)
                {
                    if (!DisplayedNodes.ContainsKey(e.to))
                    {
                        var de = new DisplayNode(e.to);
                        g.AddVertex(de);
                        DisplayedNodes.Add(e.to, de);
                        nextNodes.Add(e.to);
                    }
                    g.AddEdge(new Edge<object>(DisplayedNodes[e.from], DisplayedNodes[e.to]));
                }

                edges.Clear();

                foreach (var next in nextNodes)
                {
                    var nextEdge = currentGraph.GetEdgesFrom(next);
                    edges.AddRange(nextEdge);
                }
            }

            return g;
        }

        private void Output(string ex)
        {
            System.Console.WriteLine(ex);
            var lab = new TextBlock();
            lab.Text = ex;
            this.OutputBox.Children.Add(lab);
        }


        private void OnClickedNode(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var v = FindParent<VertexControl>(sender as DependencyObject);
            if (v == null) { return; }
            var dn = v.Vertex as DisplayNode;
            if (dn == null) { return; }
            var node = dn.Node;
            if (node == null) { return; }

            if (this.nodeInEditor != node)
            {
                ViewCode(this.CodeDisplay, node);
                this.nodeInEditor = node;
            }
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null)
            { return null; }

            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.OnTry(sender, e);
        }

        private void OnTrim(object sender, RoutedEventArgs ev)
        {

            // find all nodes where Node is Syntax  +  Edge is NoCodeChange
            var graph = this.currentGraph;
            int origNodeCount = graph.Nodes.Count;
            int origEdgeCount = graph.Edges.Count;            
            graph.TrimNoCodeChange();

            graph.Serialize("TestTrimed.GEXF");
            graph.Serialize2("TestTrimed.Txt");
            Output("Trimed " + (origNodeCount - graph.Nodes.Count) + " Nodes and " + (origEdgeCount - graph.Edges.Count) + " Edges ");
        }

        

        private void OnStats(object sender, RoutedEventArgs ev)
        {
            // method change paths
            CommitDescription.Text = "";
            var graph = this.currentGraph;

            var subGraph = new Graph();
            subGraph.Nodes.AddRange(graph.Nodes.Where(n => n.IsMethodDeclartionNode()));
            subGraph.Edges.AddRange(graph.Edges.Where(e => e.from.IsMethodDeclartionNode() && e.to.IsMethodDeclartionNode()));
                    

            var sinks = subGraph.SinkVertexs();
            List<int> changes = new List<int>();
            foreach(var s in sinks)
            {
                var methodHistory = subGraph.GetConnectedSubGraph(s, null, true);
                methodHistory.TrimNoCodeChange();

                if (methodHistory.Edges.Count == methodHistory.Nodes.Count - 1)
                {
                    CommitDescription.Text += NodeDescriber.Decribe(s) + " had " + methodHistory.Edges.Count + " changes \n " ;
                    changes.Add(methodHistory.Edges.Count);
                }                
            }

            CommitDescription.Text += "Overall " + changes.Count + " Methods changd " + changes.Min() + " - " + changes.Max() + " avrg. " + changes.Average() + "\n";
        }
    }
}

