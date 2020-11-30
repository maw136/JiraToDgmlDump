using System;
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

            var jiraRepository = new JiraRepository(jiraContext);
            var jiraService = new JiraService(jiraContext, jiraRepository);
            await jiraService.InitializeTask;
            var (issues, connections) = await jiraService.GetIssuesWithConnections().ConfigureAwait(false);
            var epicTypeId = jiraService.EpicTypeId;
            var graphBuilder = new JiraGraphBuilder(jiraContext, epicTypeId);
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
