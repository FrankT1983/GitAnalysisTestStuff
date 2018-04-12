using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TestGitClient.Logic;
using System.Windows.Controls.Primitives;
using QuickGraph;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Media;
using GraphSharp.Controls;

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
            CreateGraphToVisualize();
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

        private void CreateGraphToVisualize()
        {
            var g = new BidirectionalGraph<object, IEdge<object>>();

            //add the vertices to the graph
            string[] vertices = new string[5];
            for (int i = 0; i < 5; i++)
            {
                vertices[i] = i.ToString();
                g.AddVertex(vertices[i]);
            }

            //add some edges to the graph
            g.AddEdge(new Edge<object>(vertices[0], vertices[1]));
            g.AddEdge(new Edge<object>(vertices[1], vertices[2]));
            g.AddEdge(new Edge<object>(vertices[2], vertices[3]));
            g.AddEdge(new Edge<object>(vertices[3], vertices[1])); 
            g.AddEdge(new Edge<object>(vertices[1], vertices[4]));
            
        }

        private void CommitButtonPressed(Node n)
        {
            this.GraphDisplay.Graph = AddNodeToDisplay(n);
            VieWCode(this.CodeDisplay, n);
        }

        private void VieWCode(VirtualizingStackPanel codeDisplay, Node n)
        {
            codeDisplay.Children.Clear();
            if (n.Type == Node.NodeType.Syntax)
            {
                var lines = n.FullContent.Split('\n');
                foreach(var l in lines)
                {
                    var lab = new TextBlock();
                    lab.Text = l;
                    codeDisplay.Children.Add(lab);
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
                VieWCode(this.CodeDisplay, node);
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
    }

    internal class DisplayNode : DependencyObject
    {


        public Node Node
        {
            get { return (Node)GetValue(NodeProperty); }
            set { SetValue(NodeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Node.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NodeProperty =
            DependencyProperty.Register("Node", typeof(Node), typeof(DisplayNode), new PropertyMetadata(null));                      
       
        public DisplayNode(Node n )
        {
            this.Node = n;
        }       

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(DisplayNode), new PropertyMetadata(false));



    }

    internal class DebugHelper
    {
        internal static void BreakIntoDebug()
        {
            int i = 0;
        }
    }
}
