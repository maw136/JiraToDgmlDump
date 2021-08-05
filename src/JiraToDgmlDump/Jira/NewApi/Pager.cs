using System;
using System.Collections.Generic;
using System.Threading;

namespace JiraToDgmlDump.Jira.NewApi
{
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
}