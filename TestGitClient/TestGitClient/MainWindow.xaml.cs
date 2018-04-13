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

namespace TestGitClient
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Graph currentGraph;
        private BackgroundWorker worker = new BackgroundWorker();        

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
            {   g = GraphFactory.GraphFromRepoFolder(lokalPath); }
            catch (Exception ex)
            {   Output(ex.ToString());  }


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
            
            if (g!= null)
            {
                g.Serialize("Test.GEXF");

                currentGraph = g;
                var commitNodes = g.GetNodesOfType( Node.NodeType.Commit);
                this.CommitPane.Children.Clear();
                foreach (var n in commitNodes)
                {
                    var button = new ToggleButton();
                    button.Content = n.Content;
                    button.Click+= (o,i) => this.CommitButtonPressed( n );
                    this.CommitPane.Children.Add(button);
                }
            }
        }

        private void CommitButtonPressed(Node n)
        {
            this.GraphDisplay.Graph = AddNodeToDisplay(n);
            ViewCode(this.CodeDisplay, n);

            if (n.Type == Node.NodeType.Commit)
            {
                var filenodes = currentGraph.GetNeighborsOf(n, Node.NodeType.FileCS);

                FileTree.Items.Clear();
                var commitItem = new TreeViewItem() {Header = n.Id };                
                FileTree.Items.Add(commitItem);
                                
                foreach(var f in filenodes)
                {
                    var item = new TreeViewItem();
                    item.Header = f.Content;
                    commitItem.Items.Add(item);
                }
                commitItem.ExpandSubtree();
            }
        }

        Random rnd = new Random();
        private void ViewCode(VirtualizingStackPanel codeDisplay, Node node)
        {
            codeDisplay.Children.Clear();
            if (node.Type == Node.NodeType.Syntax)
            {

                var parents = this.currentGraph.GetConnectedSubGraph(node, new Edge.EdgeType[] { Edge.EdgeType.SyntaxHierarchialyAbove }, true).Nodes;
                parents.Sort((a, b) => a.SyntaxNode.SpanStart.CompareTo(b.SyntaxNode.SpanStart) );

                int lineOffset = node.SyntaxNode.SpanStart;

                var lines = node.FullContent.Split('\n');
                int lineNumber = 0;
                foreach(var l in lines)
                {
                    var aboveThisLine = parents.Where(n => lineOffset >= n.SyntaxNode.SpanStart && lineOffset < n.SyntaxNode.Span.End).ToList();

                    

                    var pane = new StackPanel() { Orientation=Orientation.Horizontal };
                    var lab = new TextBlock();
                    lab.Text = l;


                    for(int i=0; i < aboveThisLine.Count(); i++)
                    {
                        var btn = new Button();
                        btn.Width = 5;
                        btn.ToolTip = aboveThisLine[i].Content;

                        pane.Children.Add(btn);
                    }

                    var lineNumberLabel = new TextBlock();
                    lineNumberLabel.Text = lineNumber.ToString();
                    lineNumberLabel.Margin = new Thickness(5,0,5,0);                    
                    pane.Children.Add(lineNumberLabel);
                    pane.Children.Add(lab);

               


                    codeDisplay.Children.Add(pane);
                    lineNumber++;
                    lineOffset+=l.Length + 1; // + linebreak?
                }
            }
        }

        private BidirectionalGraph<object, IEdge<object>> AddNodeToDisplay(Node n)
        {          
            var g = new BidirectionalGraph<object, IEdge<object>>();
            var dn = new DisplayNode(n);
            g.AddVertex(dn);
            var handledVertexes = new Dictionary<Node,DisplayNode>();

            handledVertexes.Add(n ,dn);
            var edges = currentGraph.GetEdgesFrom(n, Edge.EdgeType.HierarchialyAbove);
            while (edges.Count > 0)
            {
                var nextNodes = new List<Node>();
                foreach (var e in edges)
                {
                    if (!handledVertexes.ContainsKey(e.to))
                    {
                        var de = new DisplayNode(e.to);
                        g.AddVertex(de);
                        handledVertexes.Add(e.to, de);
                        nextNodes.Add(e.to);
                    }
                    g.AddEdge(new Edge<object>(handledVertexes[e.from], handledVertexes[e.to]));
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
        
        private Node nodeInEditor = null;
        private void OnClickedNode(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var v = FindParent<VertexControl>(sender as DependencyObject);
            if (v == null) { return; }
            var dn = v.Vertex as DisplayNode;
            if (dn == null) { return; }
            var node = dn.Node ;
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
    }
}
