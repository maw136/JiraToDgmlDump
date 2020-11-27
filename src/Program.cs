using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using OpenSoftware.DgmlTools;
using OpenSoftware.DgmlTools.Builders;
using OpenSoftware.DgmlTools.Model;

namespace JiraToDgmlDump
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();

            string epicKey = null;
            do
            {
                Console.Write("Give Epic key: ");
                epicKey = Console.ReadLine()?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(epicKey))
                    Console.WriteLine("Epic key empty, retrying....");

            } while (string.IsNullOrWhiteSpace(epicKey));

            var jiraContext = new JiraContext();
            configuration.GetSection("JiraContext").Bind(jiraContext);

            var jiraRepository = new JiraRepository(jiraContext);
            var jiraService = new JiraService(jiraRepository);

            var (issues, connections) = await jiraService.GetIssuesWithConnections(epicKey);

            var graph = BuildGraph(issues, connections);

            SaveDgml(graph);

            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
        }

        private static DirectedGraph BuildGraph(IEnumerable<IssueLight> issues, IEnumerable<IssueLinkLight> connections)
        {

            var builder = new DgmlBuilder
            {
                NodeBuilders = new NodeBuilder[]
                {
                   MakeNodeBuilder()
                },
                LinkBuilders = new LinkBuilder[]
                {
                   MakeLinkBuilder()
                },
          //      CategoryBuilders = new CategoryBuilder[]
          //      {
          //         // <your category builders>
          //      },
          //      StyleBuilders = new StyleBuilder[]
          //      {
          //         // <your style builders>
          //      }
            };

            return builder.Build(issues, connections);
        }

        private static void SaveDgml(DirectedGraph graph)
        {
            var name = "jira.dgml";

            using var writer = new StreamWriter(name);
            var serializer = new XmlSerializer(typeof(DirectedGraph));
            serializer.Serialize(writer, graph);

            var path = Path.Combine(Environment.CurrentDirectory, name);
            Console.WriteLine($"DGML graph saved at: {path}");
        }

        private static NodeBuilder MakeNodeBuilder()
        {
            static Node BuildNode(IssueLight issue)
            {
                return new Node()
                {
                    Id = issue.Key,
                    Label = issue.Key,
                    Description = issue.Summary,
                    //Category = issue.Type.Name
                };
            }

            return new NodeBuilder<IssueLight>(BuildNode);
        }

        private static LinkBuilder MakeLinkBuilder()
        {
            static Link BuildLink(IssueLinkLight link)
            {
                return new Link()
                {
                    Source = link.InwardIssueKey,
                    Target = link.OutwardIssueKey,
                    Label = link.LinkType.Name,
                    Description = link.LinkType.Name
                };
            }

            return new LinkBuilder<IssueLinkLight>(BuildLink);
        }
    }
}
