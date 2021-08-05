using System.Threading.Tasks;
using JiraToDgmlDump.Jira.NewApi;
using Xunit;

namespace JiraToDgmlDump.Test
{
    public class JiraRestClientTest
    {
        private readonly JiraRestClient _jiraRestClient;

        public JiraRestClientTest()
        {
            var http = JiraApi.MakeHttpClient(
                TestConfig.Instance.Uri,
                TestConfig.Instance.Login,
                TestConfig.Instance.Password
                );
            _jiraRestClient = new JiraRestClient(http);
        }

        [Fact]
        public async Task Test1()
        {
            var users = await _jiraRestClient.GetUsers(TestConfig.Instance.Project);


        }

        [Fact]
        public async Task Test2()
        {
            var issuesWithStatuses = await _jiraRestClient.GetIssuesWithStatuses(TestConfig.Instance.Project);


        }

        [Fact]
        public async Task Test3()
        {
            var linkTypes = await _jiraRestClient.GetLinkTypes();


        }

        [Fact]
        public async Task Test4()
        {
            var issues = await _jiraRestClient.IssuesByEpic(TestConfig.Instance.Epics[0], new SearchOptions(null, 50, null));


        }
    }
}
