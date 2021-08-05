using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using JiraToDgmlDump.Jira.PreviousModel;

namespace JiraToDgmlDump.Jira.NewApi
{
    public sealed class JiraApi : IDisposable
    {
        public const int DefaultPageSize = 100;

        public static HttpClient MakeHttpClient(string baseUri, string username, string password)
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri(baseUri),
            };
            var parameter = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", parameter);
            return client;
        }

        private readonly JiraRestClient _restClient;
        private readonly HttpClient _httpClient;

        public JiraApi(string baseUri, string username, string password)
        {
            _httpClient = MakeHttpClient(baseUri, username, password);
            _restClient = new JiraRestClient(_httpClient);
        }

        public async Task<(IEnumerable<IssueLight>, IEnumerable<IssueLinkLight>)> EnumerateIssuesWithLinks(string jql, string[] fields)
        {
            SearchOptions options = new (jql, DefaultPageSize, fields);
            var pager = new Pager(_restClient, options);

            List<IssueLight> issues = new();
            List<IssueLinkLight> links = new();

            await foreach (var page in pager)
            {

            }

            return (null, null);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
