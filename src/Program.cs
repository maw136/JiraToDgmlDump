using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

            await jiraService.InitializeTask;

            var (issues, connections) = await jiraService.GetIssuesWithConnections().ConfigureAwait(false);

            var epicTypeId = jiraService.Types.Single(t => t.Name == "Epic").Id;

            var graph = BuildGraph(
                issues,
                connections,
                jiraContext,
                epicTypeId);

            SaveDgml(graph);
        }

        private static DirectedGraph BuildGraph(IEnumerable<IssueLight> issues, IEnumerable<IssueLinkLight> connections, IJiraContext jiraContext, string epicTypeId)
        {

            var builder = new DgmlBuilder
            {
                NodeBuilders = new NodeBuilder[]
                {
                   MakeNodeBuilder(jiraContext, epicTypeId)
                },
                LinkBuilders = new LinkBuilder[]
                {
                   MakeLinkBuilder(),
                   MakeContainerBuilder()
                },
                //CategoryBuilders = new CategoryBuilder[]
                //{
                //    MakeCategoryBuilder(epics)
                //},
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

        private static NodeBuilder MakeNodeBuilder(IJiraContext jiraContext, string epicTypeId)
        {
            Node BuildNode(IssueLight issue)
            {
                Debug.Assert(issue.Key != null);

                return new Node
                {
                    Id = issue.Key,
                    Label = GetIssueLabel(issue, epicTypeId),
                    Description = issue.Summary,
                    Reference = $"{jiraContext.Uri}browse/{issue.Key}"
                };
            }

            return new NodeBuilder<IssueLight>(BuildNode);
        }

        private static string GetIssueLabel(IssueLight issue, string epicTypeId)
        {
            if (issue.Type.Id == epicTypeId)
            {
                return $"{issue.Key} {issue.Summary}";
            }
            else
            {
                IEnumerable<string> elements = new[]
                {
                    issue.Key,
                    $"{issue.Summary} {(issue.StoryPoints.HasValue ? $"{issue.StoryPoints.Value} SP" : null)}",
                    $"{issue.Status.Name} {String.Join(", ", issue.Labels.Select(label => $"#{label}"))}"
                }.Where(o => !String.IsNullOrEmpty(o));

                return String.Join(Environment.NewLine, elements);
            }
        }

        private static LinkBuilder MakeLinkBuilder()
        {
            Link BuildLink(IssueLinkLight link)
            {
                return new Link
                {
                    Source = link.OutwardIssueKey,
                    Target = link.InwardIssueKey,
                    Description = link.LinkType.Name,
                };
            }

            return new LinkBuilder<IssueLinkLight>(BuildLink);
        }

        private static LinkBuilder MakeContainerBuilder()
        {
            Link BuildLink(IssueLight issue)
            {
                return new Link()
                {
                    Source = issue.EpicKey,
                    Target = issue.Key,
                    IsContainment = true
                };
            }

            static bool Accept(IssueLight issue)
                => issue.EpicKey != null;

            return new LinkBuilder<IssueLight>(BuildLink, Accept);
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
                };
            }


            return new StyleBuilder<IssueLight>(BuildStyle);
        }
    }
}
