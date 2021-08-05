using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenSoftware.DgmlTools.Model;

namespace JiraToDgmlDump
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
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
                (IReadOnlyCollection<IssueLight> issues, IReadOnlyCollection<IssueLinkLight> connections) = await jiraService.GetIssuesWithConnections().ConfigureAwait(false);
                IReadOnlyDictionary<string, JiraUser> users = await jiraService.GetUsers().ConfigureAwait(false);

                Console.Write("[Graph] or [CSV] or [Excel] or [all], give the type: ");
                string choice = Console.ReadLine()?.ToLowerInvariant();

                switch (choice)
                {
                    case "graph":
                        await BuildGraph(jiraContext, issues, connections, users).ConfigureAwait(false);
                        break;
                    case "csv":
                        await BuildCsv(jiraContext, issues, users).ConfigureAwait(false);
                        break;
                    case "excel":
                        await BuildExcel(jiraContext, issues, users).ConfigureAwait(false);
                        break;
                    case "both":
                        await Task.WhenAll(
                            BuildGraph(jiraContext, issues, connections, users),
                            BuildCsv(jiraContext, issues, users),
                            BuildExcel(jiraContext, issues, users)
                            );
                        break;
                }
            }
            finally
            {
                Console.WriteLine("Finished");
            }
        }

        private static Task BuildCsv(IJiraContext jiraContext, IReadOnlyCollection<IssueLight> issues, IReadOnlyDictionary<string, JiraUser> usersLookup)
        {
            IEnumerable<WorkItem> workItems = ToWorkItems(jiraContext, issues, usersLookup);
            return SaveCsv(workItems);
        }

        private static Task BuildExcel(IJiraContext jiraContext, IReadOnlyCollection<IssueLight> issues, IReadOnlyDictionary<string, JiraUser> usersLookup)
        {
            IEnumerable<WorkItem> workItems = ToWorkItems(jiraContext, issues, usersLookup);
            return SaveExcel(workItems);
        }

        private static IEnumerable<WorkItem> ToWorkItems(IJiraContext jiraContext, IReadOnlyCollection<IssueLight> issues, IReadOnlyDictionary<string, JiraUser> usersLookup)
        {
            Dictionary<string, IssueLight> issuesLookup = issues.ToDictionary(i => i.Key);
            Dictionary<string, int> adjacency = issues
                .Where(i => i.Type.Id == jiraContext.SubTaskTypeId && !string.IsNullOrWhiteSpace(i.ParentKey))
                .GroupBy(sub => sub.ParentKey).ToDictionary(g => g.Key, g => g.Count());

            IEnumerable<IssueLight> issuesWithoutStoriesWithSubtasks =
                issues.Where(i => !adjacency.TryGetValue(i.Key, out int count) || count == 0);
            foreach (IssueLight issue in issuesWithoutStoriesWithSubtasks)
            {
                var type = jiraContext.ToWorkItemType(issue.Type);
                WorkItemReference parent = issue.Type.Id == jiraContext.SubTaskTypeId
                    ? jiraContext.MakeJiraWorkItemReference(issue.ParentKey)
                    : null;
                if (string.IsNullOrWhiteSpace(issue.EpicKey))
                    continue;
                string user = usersLookup[issue.Assignee].DisplayName;
                string parentTitle = parent != null ? issuesLookup[parent.Id].Summary : null;
                yield return new WorkItem(jiraContext.MakeJiraWorkItemReference(issue.Key), type, issue.Summary,
                    issue.EpicKey, issue.StoryPoints, issue.Sprint, user, issue.Status.Name, parentTitle, parent);
            }
        }

        private static Task BuildGraph(IJiraContext jiraContext, IEnumerable<IssueLight> issues, IEnumerable<IssueLinkLight> connections, IReadOnlyDictionary<string, JiraUser> usersLookup)
        {
            return Task.Run(() =>
            {
                var graphBuilder = new JiraGraphBuilder(jiraContext);
                DirectedGraph graph = graphBuilder.BuildGraph(issues, connections, usersLookup);
                SaveDgml(graph);
            });
        }

        private static async Task SaveCsv(IEnumerable<WorkItem> workItems)
        {
            var name = "jira.csv";
            await workItems.SaveToCsv(name).ConfigureAwait(false);
            string path = Path.Combine(Environment.CurrentDirectory, name);
            Console.WriteLine($"CSV saved at: {path}");
        }

        private static async Task SaveExcel(IEnumerable<WorkItem> workItems)
        {
            var name = "jira.xlsx";
            await workItems.SaveToExcel(name).ConfigureAwait(false);
            string path = Path.Combine(Environment.CurrentDirectory, name);
            Console.WriteLine($"Excel saved at: {path}");
        }

        private static void SaveDgml(DirectedGraph graph)
        {
            var name = "jira.dgml";
            graph.WriteToFile(name);
            string path = Path.Combine(Environment.CurrentDirectory, name);
            Console.WriteLine($"DGML graph saved at: {path}");
        }
    }
}
