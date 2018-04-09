using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TestGitClient
{

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
               

                var graph = new Graph();

                var authorNodes = new Dictionary<string,Node>();
                var fileNodes = new Dictionary<string, Node>();
                var allNodes = new List<Node>();
                var allLinks = new List<Edge>();
                int i = 0;
                Node lastCommit = null;

                var commits = new List<Commit>(repo.Commits);                               
                commits.Sort((a, b) => a.Author.When.CompareTo(b.Author.When));
                
                foreach (Commit commit in commits)
                {
                    Node authorNode = null;
                    if (!authorNodes.ContainsKey(commit.Author.Email))
                    {
                        authorNode = new Node(commit.Author.Name, Node.NodeType.Person, commit.Author.Name);
                        allNodes.Add(authorNode);
                        authorNodes.Add(commit.Author.Email, authorNode);
                    }
                    authorNode = authorNodes[commit.Author.Email];
                    
                    i++;
                    if (i > 5) break;

                    var commitNode = new Node(commit.Id.Sha, Node.NodeType.Commit, commit.Message);
                    allNodes.Add(commitNode);
                    allLinks.Add(new Edge(authorNode, commitNode, Edge.LinkType.Author));

                    if (lastCommit != null)
                    {
                        allLinks.Add(new Edge(lastCommit, commitNode, Edge.LinkType.NextCommit));
                    }
                    lastCommit = commitNode;

                    bool hasParrents = false;
                    foreach (var parent in commit.Parents)
                    {
                        hasParrents = true;
                        var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);                        
                        foreach (TreeEntryChanges change in changes)
                        {
                            //Output(String.Format("{0} : {1}", change.Status, change.Path));

                            var fileNode = new Node(change.Path + commit.Id, Node.NodeType.File, change.Path);
                            Node oldFileNode = GetOldVersionOfFile(fileNodes, allLinks, change.Path, fileNode);

                            allNodes.Add(fileNode);
                            allLinks.Add(new Edge(commitNode, fileNode, Edge.LinkType.HierarchialyAbove));

                            if (!change.Path.EndsWith(".cs")) { continue; }

                            fileNode.Type = Node.NodeType.FileCS;
                            ParseCsFileAndAddToGraph(allNodes, allLinks, commit, change.Path, fileNode);
                        }
                    }

                    if (!hasParrents)
                    {
                        AddInitialCommitToGraph(fileNodes, allNodes, allLinks, commit, commitNode);
                    }
                }

                graph.Add(allNodes);
                graph.Add(allLinks);

                graph.Serialize("Test.GEXF");

                Output("Finished");
            }

            return usedLokal;
        }

        private void AddInitialCommitToGraph(Dictionary<string, Node> fileNodes, List<Node> allNodes, List<Edge> allLinks, Commit commit, Node commitNode)
        {
            var tree = commit.Tree;
            foreach (var element in tree)
            {
                var fileNode = new Node(element.Path + commit.Id, Node.NodeType.File, element.Path);
                fileNodes.Add(element.Path, fileNode);
                allNodes.Add(fileNode);
                allLinks.Add(new Edge(commitNode, fileNode, Edge.LinkType.HierarchialyAbove));

                if (!element.Path.EndsWith(".cs")) { continue; }
                fileNode.Type = Node.NodeType.FileCS;
                ParseCsFileAndAddToGraph(allNodes, allLinks, commit, element.Path, fileNode);
            }
        }

        private static Node GetOldVersionOfFile(Dictionary<string, Node> fileNodes, List<Edge> allLinks, string path, Node fileNode)
        {
            Node result = null;
            if (!fileNodes.ContainsKey(path))
            {
                fileNodes.Add(path, fileNode);
            }
            else
            {
                result = fileNodes[path];
                allLinks.Add(new Edge(result, fileNode, Edge.LinkType.FileModification));
                fileNodes[path] = fileNode;
            }
            return result;
        }

        private void ParseCsFileAndAddToGraph(List<Node> allNodes, List<Edge> allLinks, Commit commit, string path, Node fileNode)
        {
            var blob = commit.Tree[path].Target as Blob;
            if (blob != null)
            {
                var contentStream = blob.GetContentStream();
                using (var tr = new StreamReader(contentStream, Encoding.UTF8))
                {
                    string content = tr.ReadToEnd();
                    var foo = CSharpSyntaxTree.ParseText(content);

                    var subEdges = new List<Edge>();
                    var subNodes = new List<Node>();                    
                    AddChildsToNodes(foo.GetRoot(), fileNode, allNodes, allLinks);

                    allLinks.AddRange(subEdges);
                    allNodes.AddRange(subNodes);
                }
            }
        }

        int nodeId = 0;

        private void AddChildsToNodes(SyntaxNode rootNode, Node fileNode, List<Node> allNodes, List<Edge> allLinks)
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
                var n = new Node(nodeId.ToString(),c.Kind().ToString(),"test");
                n.FullContent = c.ToFullString().Trim();
                n.SyntaxNode = c;
                nodeId++;

                allNodes.Add(n);
                allLinks.Add(new Edge(fileNode, n, Edge.LinkType.Generic));

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
