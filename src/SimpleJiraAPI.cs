using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JiraToDgmlDump
{
    public static class JqlExtensions
    {
        public static string MakeIssueSerchJql(string project, string[] epics, string[] excludeStatuses)
        {
            var tmp = string.Join(',', epics.Select(e => "\"" + e + "\""));
            return
                $" Project=\"{project}\" AND Status NOT IN ({string.Join(',', excludeStatuses)}) AND (\"Epic Link\" IN ( {tmp} ) OR parent IN ({tmp}) OR Id IN ({tmp}) )";
        }

        public static string MakeSubtaskSearchSql(string project, string[] issues, string[] excludeStatuses)
        {
            var tmp = string.Join(',', issues.Select(e => "\"" + e + "\""));
            return
                $" Project=\"{project}\" AND Status NOT IN ({string.Join(',', excludeStatuses)}) AND parent IN ({tmp})";
        }
    }

    public sealed class SimpleJiraAPI : IDisposable
    {
        public const int DefaultPageSize = 100;

        private readonly JiraRestClient _restClient;
        private readonly HttpClient _httpClient;

        public SimpleJiraAPI(string baseUri, string username, string password)
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

    public class JiraRestClient
    {
        private readonly HttpClient _httpClient;

        public JiraRestClient([DisallowNull] HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));


        }

        public async Task<Page> IssuesSearch(SearchOptions searchOptions)
        {
            string uri = GetRequestUrl("search");
            using var response = await _httpClient.PostAsJsonAsync(uri, searchOptions);
            response.EnsureSuccessStatusCode();
            try
            {
                var stream = await response.Content.ReadAsStreamAsync();
                var json = await JsonDocument.ParseAsync(stream);
                return new Page(json);
            }
            catch // Could be ArgumentNullException or UnsupportedMediaTypeException
            {
                Console.WriteLine("HTTP Response was invalid or could not be deserialised.");
            }

            return null;
        }

        private string GetRequestUrl(string action, int version = 2)
        {
            return $"rest/api/{version}/{action}";
        }
    }

    public record SearchOptions
    {
        public string Jql { get; init; }
        public int PageSize { get; init; }
        public string[] Fields { get; init; }
        public int StartAt { get; init; }

        internal SearchOptions()
        {
        }

        public SearchOptions(string jql, int pageSize, string[] fields)
        {
            Jql = jql ?? throw new ArgumentNullException(nameof(jql));
            PageSize = pageSize;
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
            StartAt = 0;
        }
    }

    public class Pager : IAsyncEnumerable<Page>
    {
        private readonly JiraRestClient _restClient;
        private readonly SearchOptions _startSearchOptions;

        public Pager(JiraRestClient restClient, SearchOptions startSearchOptions)
        {
            _restClient = restClient ?? throw new ArgumentNullException(nameof(restClient));
            _startSearchOptions = startSearchOptions ?? throw new ArgumentNullException(nameof(startSearchOptions));
        }

        public async IAsyncEnumerator<Page> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            var searchOptions = _startSearchOptions;
            bool hasNextPage = true;
            while (cancellationToken.IsCancellationRequested && hasNextPage)
            {
                var page = await _restClient.IssuesSearch(searchOptions);
                searchOptions = NextPage(searchOptions);
                hasNextPage = searchOptions.StartAt < page.Total;
                yield return page;
            }

            throw new NotImplementedException();
        }

        private SearchOptions NextPage(SearchOptions searchOptions)
        {
            return new SearchOptions
            {
                Jql = searchOptions.Jql,
                PageSize = searchOptions.PageSize,
                Fields = searchOptions.Fields,
                StartAt = searchOptions.StartAt + searchOptions.PageSize
            };
        }
    }

    public sealed class Page : IDisposable
    {
        public JsonDocument RawPage { get; }

        public int StartAt { get; }
        public int MaxResults { get; }
        public int Total { get; }

        public Page([DisallowNull] JsonDocument rawPage)
        {
            RawPage = rawPage ?? throw new ArgumentNullException(nameof(rawPage));

            StartAt = rawPage.RootElement.GetProperty("startAt").GetInt32();
            MaxResults = rawPage.RootElement.GetProperty("maxResults").GetInt32();
            Total = rawPage.RootElement.GetProperty("total").GetInt32();
        }

        public void Dispose()
        {
            RawPage?.Dispose();
        }
    }
}
