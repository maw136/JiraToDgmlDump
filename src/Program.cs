using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenSoftware.DgmlTools.Model;

namespace JiraToDgmlDump
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting...");

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            IJiraContext jiraContext = new JiraContext();
            configuration.GetSection("JiraContext").Bind(jiraContext);

            IJiraRepository jiraRepository;
            if (jiraContext.UseCachedRepo)
            {
                var file = new FileStream("cache.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite,
                    FileShare.Read, 16384);
                jiraRepository =
                   new JiraCachedRepository(new JiraRepository(jiraContext), new DiskCache(file, file, jiraContext.WaitForData));
            }
            else
            {
                jiraRepository = new JiraRepository(jiraContext);
            }

            var jiraService = new JiraService(jiraContext, jiraRepository);
            await jiraService.InitializeTask;
            var (issues, connections) = await jiraService.GetIssuesWithConnections().ConfigureAwait(false);

            Console.Write("[Graph] or [CSV] or [both], give the type: ");
            var choice = Console.ReadLine()?.ToLowerInvariant();

            switch (choice)
            {
                case "graph":
                    await BuildGraph(jiraContext, issues, connections);
                    break;
                case "csv":
                    await BuildCsv(jiraContext, issues);
                    break;
                case "both":
                    await Task.WhenAll(
                        BuildGraph(jiraContext, issues, connections),
                        BuildCsv(jiraContext, issues)
                        );
                    break;
            }
        }

        private static Task BuildCsv(IJiraContext jiraContext, IEnumerable<IssueLight> issues)
        {
            var workItems = ToWorkItems(jiraContext, issues);
            return SaveCsv(workItems);
        }

        private static IEnumerable<WorkItem> ToWorkItems(IJiraContext jiraContext, IEnumerable<IssueLight> issues)
        {
            foreach (IssueLight issue in issues)
            {
                var type = jiraContext.ToWorkItemType(issue.Type);
                var parent = issue.Type.Id == jiraContext.SubTaskTypeId
                    ? jiraContext.MakeJiraWorkItemReference(issue.ParentKey)
                    : null;
                if (string.IsNullOrWhiteSpace(issue.EpicKey))
                    continue;
                yield return new WorkItem(jiraContext.MakeJiraWorkItemReference(issue.Key), type, issue.Summary,
                    issue.EpicKey, issue.StoryPoints, issue.Sprint, issue.Assignee, parent);
            }
        }

        private static Task BuildGraph(IJiraContext jiraContext, IEnumerable<IssueLight> issues, IEnumerable<IssueLinkLight> connections)
        {
            return Task.Run(() =>
            {
                var graphBuilder = new JiraGraphBuilder(jiraContext);
                var graph = graphBuilder.BuildGraph(issues, connections);
                SaveDgml(graph);
            });
        }

        private static async Task SaveCsv(IEnumerable<WorkItem> workItems)
        {
            var name = "jira.csv";
            await workItems.SaveToCsv(name).ConfigureAwait(false);
            var path = Path.Combine(Environment.CurrentDirectory, name);
            Console.WriteLine($"CSV saved at: {path}");
        }

        private static void SaveDgml(DirectedGraph graph)
        {
            var name = "jira.dgml";
            graph.WriteToFile(name);
            var path = Path.Combine(Environment.CurrentDirectory, name);
            Console.WriteLine($"DGML graph saved at: {path}");
        }
    }
}
