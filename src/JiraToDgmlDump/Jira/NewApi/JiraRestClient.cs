using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace JiraToDgmlDump.Jira.NewApi
{
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
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(uri, searchOptions).ConfigureAwait(false);
            return await ExecutePage(response).ConfigureAwait(false);
        }

        public async Task<Page> IssuesByEpic(string epic, SearchOptions searchOptions)
        {
            string uri = $"rest/agile/1.0/epic/{epic}/issue";
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(uri, searchOptions).ConfigureAwait(false);
            return await ExecutePage(response).ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<IssueType>> GetIssuesWithStatuses(string project)
        {
            string uri = GetRequestUrl("project/" + project + "/statuses");
            await using Stream stream = await _httpClient.GetStreamAsync(uri).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<List<IssueType>>(stream).ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<LinkType>> GetLinkTypes()
        {
            string uri = GetRequestUrl("issueLinkType");
            await using Stream stream = await _httpClient.GetStreamAsync(uri).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<List<LinkType>>(stream).ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<JiraUser>> GetUsers(string project)
        {
            string uri = GetRequestUrl("user/assignable/search?maxResults=100000&project=") + project;
            await using Stream stream = await _httpClient.GetStreamAsync(uri).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<List<JiraUser>>(stream).ConfigureAwait(false);
        }

        private async Task<Page> ExecutePage(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            try
            {
                Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                JsonObject json = JsonNode.Parse(stream) as JsonObject;
                return new Page(json);
            }
            catch // Could be ArgumentNullException or UnsupportedMediaTypeException
            {
                Console.WriteLine("HTTP Response was invalid or could not be deserialised.");
            }

            return null;
        }

        private static string GetRequestUrl(string action, int version = 2)
        {
            return $"rest/api/{version}/{action}";
        }
    }

    public record JiraObject
    {
        public string Id { get; init; }
        public string Name { get; init; }
    }

    public record IssueType : JiraObject
    {
        public IssueStatus[] Statuses { get; init; }
    }

    public record IssueStatus : JiraObject
    {
        public string Description { get; init; }
    }

    public record LinkType : JiraObject
    {
        public string Inward { get; init; }
        public string Outward { get; init; }
    }
}