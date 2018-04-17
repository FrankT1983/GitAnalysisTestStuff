using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGitClient.Logic
{
    public class GraphFactory
    {
        static public  Graph GraphFromRepoFolder(Repository repo, int max = -1)
        {                       
            var graph = new Graph();
            graph.SetRepository(repo);

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
                System.Console.WriteLine(i++);
                if (max > 0)
                {
                    if (i > max) break;
                }
                    
                Node authorNode = null;
                if (!authorNodes.ContainsKey(commit.Author.Email))
                {
                    authorNode = new Node(commit.Author.Name, Node.NodeType.Person, commit.Author.Name);
                    allNodes.Add(authorNode);
                    authorNodes.Add(commit.Author.Email, authorNode);
                }
                    
                authorNode = authorNodes[commit.Author.Email];                

                var commitNode = new Node(commit.Id.Sha, Node.NodeType.Commit, commit.MessageShort.Trim(), commit.Message.Trim());
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

                        if (previousTrees.ContainsKey(ContructCommitTreeIdentifyer(commit.Sha, change.Path)))
                        {
                            // this can happen with multiple Parents in case of a merge ... think about how to handle this

                        }
                        else
                        {
                            previousTrees.Add(ContructCommitTreeIdentifyer(commit.Sha, change.Path), enrichedTree);
                        }                           
                            
                        SyntakTreeDecorator prefTree;
                        if (previousTrees.TryGetValue(ContructCommitTreeIdentifyer(parent.Sha, change.Path), out prefTree))
                        {
                            var transitionEdges = new List<Edge>();
                            CompareTressAndCreateEdges(prefTree, enrichedTree, transitionEdges);
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

            return graph;            
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

        static private string ContructCommitTreeIdentifyer(string comitSha, string filePath)
        {
            return comitSha + "|" + filePath;
        }

        private static void CompareTressAndCreateEdges(SyntakTreeDecorator fromTree, SyntakTreeDecorator tooTree, List<Edge> transitionEdges)
        {
            if (fromTree == null || fromTree.childs == null)
            {
                return;
            }


            var thisLevelsEdges = new List<Edge>();

            var tooList = new List<SyntakTreeDecorator>();
            var notFound = new List<SyntakTreeDecorator>();

            foreach (var from in fromTree.childs)
            {
                if (tooTree == null || tooTree.childs == null)
                {
                    notFound.Add(from);
                    continue;
                    // probally deleted
                }

                var to = TreeComparison.FindBelongingThing(from, tooTree.childs);
                if (to != null)
                {
                    if (tooList.Contains(to.treeNode))
                    {
                        // used twice ... potential Bug
                        DebugHelper.BreakIntoDebug();
                    }

                    tooList.Add(to.treeNode);
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
                    notFound.Add(from);
                }
            }

            // track which kinds where found ... rest was probably  created
            // todo: I should realy unit-test this.
        }

        private static void AddInitialCommitToGraph(Dictionary<string, Node> fileNodes, List<Node> allNodes, List<Edge> allEdges, Commit commit, Node commitNode, Dictionary<string, SyntakTreeDecorator> previousTrees)
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

    }
}
