using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.V1;
using Neo4jClient;

namespace TestGitClient.Tools
{
    internal class GraphToNeo4j
    {       
        public class DbNode
        {
            public long LongId { get; set; }
            public string Id { get; set; }
            public string Content { get; set; }
            public string Type { get; set; }

            public string ToCreateString()
            {
                //var strID = Id.Replace(" ", "") + intId.ToString();
                // without "_" it tries to index by id
                var strID = "node_"+ LongId.ToString();
                return "("+ strID + ":" + Type + "{Content:\'" + Content + "\', Id:\'"+ Id+ "\', LongId:\'" + LongId + "\''})";
            }
        }

        public class DbEdge
        {
            public long FromLongId { get; set; }
            public long ToLongId { get; set; }            
            
            public string Type { get; set; }

            public string ToCreateString()
            {
                //var strID = Id.Replace(" ", "") + intId.ToString();
                var fromID = "node_" + FromLongId.ToString();
                var toId = "node_" + ToLongId.ToString();
                return "(" +fromID + ")-[:" + Type + "]->(" + toId + ")";
            }

        }

        public class Person
        {
            //all public fields need to be properties with getters and setters
            public int born { get; set; }

            public string name { get; set; }
        }      
   

        static public void PersistToVm(Graph graph)
        {
            // hardcode credentials for local, non-internet connected vm
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"), "neo4j", "password");
            client.Connect();


            ClearDb(client);

            System.Console.WriteLine("Start Adding");
            /*
            foreach(var n in graph.Nodes)
            {
                var toDb = new DbNode()
                {
                    LongId = n.LongId,
                    Id = n.Id,
                    Content = n.Content,
                    Type = n.Type.ToString()
                };


           
                //System.Console.WriteLine(toDb.ToCreateString());
                //client.Cypher.Create( toDb.ToCreateString()).ExecuteWithoutResults();
                var c =client.Cypher.Create("(obj:" + n.Type + "{newObj})").
                    WithParam("newObj", toDb);

                //System.Console.WriteLine(c.Write.Query.DebugQueryText);
                c.ExecuteWithoutResults();
            }
            System.Console.WriteLine("Finished Adding Nodes");
            */
            
            foreach (var e in graph.Edges)
            {
                var toDb = new DbEdge()
                {
                    FromLongId = e.from.LongId,
                    ToLongId = e.to.LongId,
                    Type = e.type.ToString(),

                };

                var c =client.Cypher.Match("(obj1:" + e.from.Type + ")", "(obj2:" + e.to.Type + ")")
                    .Where((DbNode obj1) => obj1.LongId == e.from.LongId)
                    .AndWhere((DbNode obj2) => obj2.LongId == e.to.LongId)
                    .Create("(obj1)-[:" + e.type.ToString() + "]->(obj2)");
                //System.Console.WriteLine(c.Write.Query.DebugQueryText);
                c.ExecuteWithoutResultsAsync();


                //client.Cypher.Create(toDb.ToCreateString()).ExecuteWithoutResults();
                //System.Console.WriteLine(toDb.ToCreateString());
            }
            

            System.Console.WriteLine("Finished Adding Edges");            
            
        }

        private static void ClearDb(GraphClient client)
        {
            //      "MATCH (n) OPTIONAL MATCH(n)-[r]-() DELETE n,r"
            client.Cypher.Match("(n)").OptionalMatch("(n)-[r]-()").Delete("n,r");
        }
    }
}
