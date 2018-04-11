﻿using LibGit2Sharp;
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
            var url = this.UrlInput.Text;
            var lokalPath = this.LokalPath.Text;
            this.OutputBox.Children.Clear();

            bool usedLokal = false;
            try
            {   usedLokal = OutputLokalRepo(lokalPath); }
            catch (Exception ex)
            {   Output(ex.ToString());  }


            if (!usedLokal)
            {
                // try opening
                try
                {
                    Output("Starting clone");
                    Repository.Clone(url, lokalPath);
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

                var authorNodes = new Dictionary<string, Node>();
                var fileNodes = new Dictionary<string, Node>();
                var previousTrees = new Dictionary<string, SyntakTreeDecorator>();  // todo: this is inefficent, could drop commits, that are note useded anymore
                var allNodes = new List<Node>();
                var allEdges = new List<Edge>();
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

                    var commitNode = new Node(commit.Id.Sha, Node.NodeType.Commit, commit.Message.Trim());
                    allNodes.Add(commitNode);
                    allEdges.Add(new Edge(authorNode, commitNode, Edge.EdgeType.Author));

                    if (lastCommit != null)
                    {
                        allEdges.Add(new Edge(lastCommit, commitNode, Edge.EdgeType.NextCommit));
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
                            Node oldFileNode = GetOldVersionOfFile(fileNodes, allEdges, change.Path, fileNode);

                            allNodes.Add(fileNode);
                            allEdges.Add(new Edge(commitNode, fileNode, Edge.EdgeType.HierarchialyAbove));

                            if (!change.Path.EndsWith(".cs")) { continue; }

                            fileNode.Type = Node.NodeType.FileCS;

                            var subEdges = new List<Edge>();
                            var subNodes = new List<Node>();
                            var enrichedTree = TreeHelper.ParseCsFileAndAddToGraph(subNodes, subEdges, commit, change.Path, fileNode);
                            allEdges.AddRange(subEdges);
                            allNodes.AddRange(subNodes);

                            previousTrees.Add(ContructCommitTreeIdentifyer(commit.Sha,change.Path), enrichedTree);

                            SyntakTreeDecorator prefTree;
                            if (previousTrees.TryGetValue(ContructCommitTreeIdentifyer(parent.Sha, change.Path), out prefTree))
                            {
                                var transitionEdges = new List<Edge>();
                                CompareTressAndCreateEdges( prefTree, enrichedTree, transitionEdges);
                                allEdges.AddRange(transitionEdges);
                            }
                        }
                    }

                    if (!hasParrents)
                    {
                        AddInitialCommitToGraph(fileNodes, allNodes, allEdges, commit, commitNode, previousTrees);
                    }
                }

                graph.Add(allNodes);
                graph.Add(allEdges);

                graph.Serialize("Test.GEXF");

                Output("Finished");
            }

            return usedLokal;
        }

        static private string ContructCommitTreeIdentifyer(string comitSha, string filePath)
        {
            return comitSha + "|" + filePath;
        }

        private static void CompareTressAndCreateEdges(SyntakTreeDecorator fromTree, SyntakTreeDecorator tooTree, List<Edge> transitionEdges)
        {
            var thisLevelsEdges = new List<Edge>();

            foreach(var from in fromTree.childs)
            {
                var to = TreeComparison.FindBelongingThing(from, tooTree.childs);

                if (to != null)
                {
                    if (!to.wasModified)
                    {
                        transitionEdges.Add(new Edge(from.equivilantGraphNode, to.treeNode.equivilantGraphNode, Edge.EdgeType.NoCodeChange));
                        continue;
                    }
                    else
                    {
                        switch (to.howModified)
                        {
                            case ModificationKind.nameChanged:
                                transitionEdges.Add(new Edge(from.equivilantGraphNode, to.treeNode.equivilantGraphNode, Edge.EdgeType.CodeChangedRename));
                                break;
                            default:
                                transitionEdges.Add(new Edge(from.equivilantGraphNode, to.treeNode.equivilantGraphNode, Edge.EdgeType.CodeChanged));
                                break;
                        }                       
                    }

                    CompareTressAndCreateEdges(from, to.treeNode, transitionEdges);
                }
                else
                {
                    // probally deleted
                }
            }

            // track which kinds where found ... rest was probably  created
            // todo: I should realy unit-test this.
        }       

        private void AddInitialCommitToGraph(Dictionary<string, Node> fileNodes, List<Node> allNodes, List<Edge> allEdges, Commit commit, Node commitNode, Dictionary<string, SyntakTreeDecorator> previousTrees)
        {
            var tree = commit.Tree;
            foreach (var element in tree)
            {
                var fileNode = new Node(element.Path + commit.Id, Node.NodeType.File, element.Path);
                fileNodes.Add(element.Path, fileNode);
                allNodes.Add(fileNode);
                allEdges.Add(new Edge(commitNode, fileNode, Edge.EdgeType.HierarchialyAbove));

                if (!element.Path.EndsWith(".cs")) { continue; }
                fileNode.Type = Node.NodeType.FileCS;
                var enrichedTree = TreeHelper.ParseCsFileAndAddToGraph(allNodes, allEdges, commit, element.Path, fileNode);

                previousTrees.Add(ContructCommitTreeIdentifyer(commit.Sha, element.Path), enrichedTree);
            }
        }

        private static Node GetOldVersionOfFile(Dictionary<string, Node> fileNodes, List<Edge> allEdges, string path, Node fileNode)
        {
            Node result = null;
            if (!fileNodes.ContainsKey(path))
            {
                fileNodes.Add(path, fileNode);
            }
            else
            {
                result = fileNodes[path];
                allEdges.Add(new Edge(result, fileNode, Edge.EdgeType.FileModification));
                fileNodes[path] = fileNode;
            }
            return result;
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
