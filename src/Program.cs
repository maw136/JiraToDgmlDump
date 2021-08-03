#define CACHED

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

            var jiraContext = new JiraContext();
            configuration.GetSection("JiraContext").Bind(jiraContext);

#if CACHED
            await using var file = new FileStream("cache.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.Read, 16384);
            var jiraRepository =
                new JiraCachedRepository(new JiraRepository(jiraContext), new DiskCache(file, file, true));
#else
            var jiraRepository = new JiraRepository(jiraContext);
#endif

            var jiraService = new JiraService(jiraContext, jiraRepository);
            await jiraService.InitializeTask;
            var (issues, connections) = await jiraService.GetIssuesWithConnections().ConfigureAwait(false);

            Console.WriteLine("Graph or CSV or both");
            var choice = Console.ReadLine()?.ToLowerInvariant();

            switch (choice)
            {
                case "graph":
                    await BuildGraph(jiraContext, issues, connections);
                    break;
                case "csv":
                    await BuildCsv(jiraContext, issues, connections);
                    break;
                case "both":
                    await Task.WhenAll(
                        BuildGraph(jiraContext, issues, connections),
                        BuildCsv(jiraContext, issues, connections)
                        );
                    break;
            }
        }

        private static async Task BuildCsv(IJiraContext jiraContext, IEnumerable<IssueLight> issues, IEnumerable<IssueLinkLight> connections)
        {
            throw new NotImplementedException();
        }

        private static async Task BuildGraph(IJiraContext jiraContext, IEnumerable<IssueLight> issues, IEnumerable<IssueLinkLight> connections)
        {
            var graphBuilder = new JiraGraphBuilder(jiraContext);
            var graph = graphBuilder.BuildGraph(issues, connections);
            SaveDgml(graph);
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
