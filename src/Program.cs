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
            Console.WriteLine("Starting...");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();

            var jiraContext = new JiraContext();
            configuration.GetSection("JiraContext").Bind(jiraContext);

            var jiraRepository = new JiraRepository(jiraContext);
            var jiraService = new JiraService(jiraRepository);

            var (issues, connections) = await jiraService.GetIssuesWithConnections().ConfigureAwait(false);

            var graph = BuildGraph(issues, connections, new HashSet<string>(jiraContext.Epics));

            SaveDgml(graph);

            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
        }

        private static DirectedGraph BuildGraph(IEnumerable<IssueLight> issues, IEnumerable<IssueLinkLight> connections, IReadOnlySet<string> epics)
        {

            var builder = new DgmlBuilder
            {
                NodeBuilders = new NodeBuilder[]
                {
                   MakeNodeBuilder(epics)
                },
                LinkBuilders = new LinkBuilder[]
                {
                   MakeLinkBuilder(epics)
                },
                CategoryBuilders = new CategoryBuilder[]
                {
                    MakeCategoryBuilder(epics)
                },
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

        private static NodeBuilder MakeNodeBuilder(IReadOnlySet<string> epics)
        {
            static Node BuildNode(IssueLight issue)
            {
                return new Node()
                {
                    Id = issue.Key,
                    Label = issue.Key,
                    Description = issue.Summary,
                };
            }

            bool Accept(IssueLight issue)
            {
                return epics.Contains(issue.Key);
            }

            return new NodeBuilder<IssueLight>(BuildNode, Accept);
        }

        private static LinkBuilder MakeLinkBuilder(IReadOnlySet<string> epics)
        {
            Link BuildLink(IssueLinkLight link)
            {
                return new Link()
                {
                    Source = link.InwardIssueKey,
                    Target = link.OutwardIssueKey,
                    Label = link.LinkType.Name,
                    Description = link.LinkType.Name,
                    IsContainment = epics.Contains(link.InwardIssueKey)
                };
            }

            return new LinkBuilder<IssueLinkLight>(BuildLink);
        }

        private static CategoryBuilder MakeCategoryBuilder(IReadOnlySet<string> epics)
        {
            IEnumerable<Category> BuildCategory(IssueLight issue)
            {
                if (epics.Contains(issue.Key))
                    yield return new Category
                    {
                        Id = issue.Key,
                        Label = issue.Key,
                        Background = issue.Key,
                    };
            }

            return new CategoriesBuilder<IssueLight>(BuildCategory);
        }

        private static StyleBuilder MekeStyleBuilder(IReadOnlySet<string> epics)
        {
            Style BuildStyle(IssueLight issue)
            {
                return new Style()
                {
                    //Condition = new List<Condition> { new Condition { } }

                };
            }


            return new StyleBuilder<IssueLight>(BuildStyle);
        }
    }
}
