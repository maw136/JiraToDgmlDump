using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlassian.Jira;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace JiraToDgmlDump
{
    public class JiraRepository : IJiraRepository
    {
        private const int MaxIssuesPerRequest = 100;

        private readonly Atlassian.Jira.Jira _jira;
        private readonly IJiraContext _jiraContext;

        public JiraRepository(IJiraContext jiraContext)
        {
            // RAII
            _jiraContext = jiraContext;

            _jira = Atlassian.Jira.Jira.CreateRestClient(
                jiraContext.Uri, jiraContext.Login, jiraContext.Password);

            _jira.Issues.MaxIssuesPerRequest = MaxIssuesPerRequest;
        }

        public async Task<IList<JiraUser>> GetAllUsersInProject()
        {
            var rawUsers = await _jira.GetAllUsers(_jiraContext);
            return rawUsers as IList<JiraUser> ?? rawUsers?.ToList() ?? new List<JiraUser>();
        }

        public async Task<IList<IssueLight>> GetAllIssuesInProject(string epicKey)
        {
            bool useStatus = false;
            bool useCreated = false;
            bool useEpic = !string.IsNullOrWhiteSpace(epicKey);

            var createdFilterPart =
                $@" AND Created > ""{DateTime.Today.AddDays(-_jiraContext.DaysBackToFetchIssues).Date:yyyy-MM-dd}"" ";
            var statusFilterPart = " AND Status NOT IN ( 11114, 6, 11110, 10904, 10108, 10109, 11115 ) ";
            var epicFilterPart = $@" AND (""Epic Link"" = ""{epicKey}"" OR parent in (""{epicKey}"") ";


            var searchOptions =
                new IssueSearchOptions(
                    $@" Project = ""{_jiraContext.Project}"" {(useStatus ? statusFilterPart:"")} {(useEpic?epicFilterPart:"")} ) {(useCreated ? createdFilterPart: "")} ")
                {
                    StartAt = 0,
                    FetchBasicFields = false,
                    AdditionalFields = new[]
                    {
                        "key",
                        "assignee",
                        "reporter",
                        "created",
                        "summary",
                        "status",
                        "issuetype",
                    },
                };

            var result = new List<IssueLight>();
            IPagedQueryResult<Issue> pages = null;
            do
            {
                pages = await _jira.Issues.GetIssuesFromJqlAsync(searchOptions);
                Debug.Assert(pages != null);
                result.AddRange(pages.Select(JiraExtensions.ToIssueLight));
                searchOptions.StartAt = Math.Min(searchOptions.StartAt + pages.ItemsPerPage, pages.TotalItems);

            } while (searchOptions.StartAt < pages.TotalItems);

            return result;
        }

        public async Task<IEnumerable<IssueLinkLight>> GetLinks(IssueLight rawIssue)
        {
            if (rawIssue == null)
                throw new ArgumentNullException(nameof(rawIssue));

            //async Task<IEnumerable<IssueLinkLight>> getIssueLinks(string issueKey, IEnumerable<string> linkTypeNames)
            //{
            //    var serializerSettings = _jira.RestClient.Settings.JsonSerializerSettings;
            //    var jtoken1 = (await _jira.RestClient.ExecuteRequestAsync(Method.GET, string.Format("rest/api/2/issue/{0}?fields=issuelinks,created", issueKey), null).ConfigureAwait(false))["fields"]["issuelinks"];
            //    if (jtoken1 == null)
            //        throw new InvalidOperationException("There is no 'issueLinks' field on the issue data, make sure issue linking is turned on in JIRA.");
            //    var source = jtoken1.Cast<JObject>();
            //    var filteredIssueLinks = source;
            //    if (linkTypeNames != null)
            //        filteredIssueLinks = source.Where(link => linkTypeNames.Contains(link["type"]["name"].ToString(), StringComparer.InvariantCultureIgnoreCase));
            //    //HashSet<string> issuesMap = await this._jira.Issues.GetIssuesAsync((IEnumerable<string>) filteredIssueLinks.Select<JObject, string>((Func<JObject, string>) (issueLink => (string) Extensions.Value<string>((IEnumerable<JToken>) (issueLink["outwardIssue"] ?? issueLink["inwardIssue"])[(object) "key"]))).ToList<string>).ConfigureAwait(false);
            //    //if (!issuesMap.Keys.Contains(issueKey))
            //    //    issuesMap.Add(issueKey, issue);
            //    return filteredIssueLinks.Select(issueLink =>
            //    {
            //        var linkType = JsonConvert.DeserializeObject<IssueLinkType>(issueLink["type"].ToString(), serializerSettings);
            //        var jtoken2 = issueLink["outwardIssue"];
            //        var jtoken3 = issueLink["inwardIssue"];
            //        var index1 = jtoken2 != null ? (string)jtoken2["key"] : null;
            //        var index2 = jtoken3 != null ? (string)jtoken3["key"] : null;
            //        var outwardIssue = index1 == null ? issueKey : index1;
            //        var inwardIssue = index2 == null ? issueKey : index2;
            //        return new IssueLinkLight { LinkType = linkType.ToNamedObjectLight(), OutwardIssueKey = outwardIssue, InwardIssueKey = inwardIssue };
            //    });

            //}

            //var linksRawer = await getIssueLinks(rawIssue.Key, null);


            var linksRaw = await _jira.Links.GetLinksForIssueAsync(rawIssue.Key);
            return linksRaw.Select(JiraExtensions.ToIssueLinkLight);
        }

        public async Task<IEnumerable<(string, IEnumerable<IssueLinkLight>)>> GetAllLinks(IList<IssueLight> rawIssues)
        {
            if (_jiraContext.LinkTypes == null)
                _jiraContext.LinkTypes = (await GetLinkTypes()).Select(l => l.Id).ToArray();


            var result = rawIssues.Select(rawIssue
                => GetLinks(rawIssue).ContinueWith(t
                    => (rawIssue.Key, t.Result.Where(link => _jiraContext.LinkTypes.ContainsById(link.LinkType))), TaskContinuationOptions.OnlyOnRanToCompletion));

            return await Task.WhenAll(result);
        }

        public async Task<IEnumerable<JiraNamedObjectLight>> GetLinkTypes()
        {
            var linkTypesRaw = await _jira.Links.GetLinkTypesAsync();
            return linkTypesRaw.Select(JiraExtensions.ToNamedObjectLight);
        }

        public async Task<IEnumerable<JiraNamedObjectLight>> GetStatuses()
        {
            var statuses = await _jira.Statuses.GetStatusesAsync();
            return statuses?.Select(JiraExtensions.ToNamedObjectLight) ?? Enumerable.Empty<JiraNamedObjectLight>();
        }

        public async Task<IEnumerable<JiraNamedObjectLight>> GetTypes()
        {
            var statuses = await _jira.IssueTypes.GetIssueTypesForProjectAsync(_jiraContext.Project);
            return statuses?.Select(JiraExtensions.ToNamedObjectLight) ?? Enumerable.Empty<JiraNamedObjectLight>();
        }


    }
}
