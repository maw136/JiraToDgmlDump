using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlassian.Jira;

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
            var rawUsers = await _jira.GetAllUsers(_jiraContext).ConfigureAwait(false);
            return rawUsers as IList<JiraUser> ?? rawUsers?.ToList() ?? new List<JiraUser>();
        }

        public async Task<IList<IssueLight>> GetAllIssuesInProject()
        {
            var customFields = await _jira.Fields.GetCustomFieldsAsync(new CustomFieldFetchOptions
                { ProjectKeys = {_jiraContext.Project}});
            var epicField = customFields.First(cf => cf.Name == "Epic Link");

            bool useStatus = false;
            bool useCreated = false;
            bool useEpics = _jiraContext.Epics?.Any() ?? false;
            string epics = string.Join(',', _jiraContext.Epics?.Select(i => $"\"{i}\"") ?? Enumerable.Empty<string>());

            var createdFilterPart =
                $@" AND Created > ""{DateTime.Today.AddDays(-_jiraContext.DaysBackToFetchIssues).Date:yyyy-MM-dd}"" ";
            var statusFilterPart = " AND Status NOT IN ( 11114, 6, 11110, 10904, 10108, 10109, 11115 ) ";
            var epicFilterPart = $@" AND (""{epicField.Name}"" in ({epics}) OR parent in ({epics}) OR id in ({epics}) ) ";


            var searchOptions =
                new IssueSearchOptions(
                    $@" Project = ""{_jiraContext.Project}""{(useStatus ? statusFilterPart:"")}{(useEpics ? epicFilterPart:"")}{(useCreated ? createdFilterPart: "")} ")
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
                        epicField.Id,
                        "parent",
                        "labels",
                        "storypoints"
                    }
                };

            Console.WriteLine($"JQL: {searchOptions.Jql}");

            var result = new List<IssueLight>();
            IPagedQueryResult<Issue> pages = null;
            do
            {
                pages = await _jira.Issues.GetIssuesFromJqlAsync(searchOptions).ConfigureAwait(false);
                Debug.Assert(pages != null);
                result.AddRange(pages.Select(i=>i.ToIssueLight(epicField.Id)));
                searchOptions.StartAt = Math.Min(searchOptions.StartAt + pages.ItemsPerPage, pages.TotalItems);

            } while (searchOptions.StartAt < pages.TotalItems);

            return result;
        }

        public async Task<IEnumerable<IssueLinkLight>> GetLinks(IssueLight rawIssue)
        {
            if (rawIssue == null)
                throw new ArgumentNullException(nameof(rawIssue));

            var linksRaw = await _jira.Links.GetLinksForIssueAsync(rawIssue.Key).ConfigureAwait(false);
            return linksRaw.Select(JiraExtensions.ToIssueLinkLight);
        }
        public async Task<IEnumerable<JiraNamedObjectLight>> GetLinkTypes()
        {
            var linkTypesRaw = await _jira.Links.GetLinkTypesAsync().ConfigureAwait(false);
            return linkTypesRaw.Select(JiraExtensions.ToNamedObjectLight);
        }

        public async Task<IEnumerable<JiraNamedObjectLight>> GetStatuses()
        {
            var statuses = await _jira.Statuses.GetStatusesAsync().ConfigureAwait(false);
            return statuses?.Select(JiraExtensions.ToNamedObjectLight) ?? Enumerable.Empty<JiraNamedObjectLight>();
        }

        public async Task<IEnumerable<JiraNamedObjectLight>> GetTypes()
        {
            var statuses = await _jira.IssueTypes.GetIssueTypesForProjectAsync(_jiraContext.Project).ConfigureAwait(false);
            return statuses?.Select(JiraExtensions.ToNamedObjectLight) ?? Enumerable.Empty<JiraNamedObjectLight>();
        }
    }
}
