using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace JiraToDgmlDump.Jira.NewApi
{
    public sealed class JiraApi : IDisposable
    {
        public const int DefaultPageSize = 100;

        private readonly JiraRestClient _restClient;
        private readonly HttpClient _httpClient;

        public JiraApi(string baseUri, string username, string password)
        {
            _httpClient = MakeHttpClient(baseUri, username, password);
            _restClient = new JiraRestClient(_httpClient);
        }

        public (IEnumerable<IssueLight>, IEnumerable<IssueLinkLight>) EnumerateIssuesWithLinks(string jql, string[] fields)
        {
            SearchOptions options = new (jql, DefaultPageSize, fields);
            var pager = new Pager(_restClient, options);

            return (null, null);
        }

        private HttpClient MakeHttpClient(string baseUri, string username, string password)
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri(baseUri),
            };
            var parameter = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", parameter);
            return client;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
