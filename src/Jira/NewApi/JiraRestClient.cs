using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
}