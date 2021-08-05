using System.IO;
using Microsoft.Extensions.Configuration;

namespace JiraToDgmlDump.Test
{
    public class TestConfig
    {
        public static readonly IJiraContext Instance;

        static TestConfig()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                 .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
                 .AddEnvironmentVariables()
                 .Build();

            IJiraContext jiraContext = new JiraContext();
            configuration.GetSection("JiraContext").Bind(jiraContext);
        }
    }
}
