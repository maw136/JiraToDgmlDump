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

            try
            {
                _jira = Atlassian.Jira.Jira.CreateRestClient(
                    jiraContext.Uri, jiraContext.Login, jiraContext.Password);
            }
            finally
            {

            }

            _jira.Issues.MaxIssuesPerRequest = MaxIssuesPerRequest;
        }

        public async Task<IList<JiraUser>> GetAllUsersInProject()
        {
            var rawUsers = await _jira.GetAllUsers(_jiraContext).ConfigureAwait(false);
            return rawUsers as IList<JiraUser> ?? rawUsers?.ToList() ?? new List<JiraUser>();
        }

        public async Task<IList<IssueLight>> GetAllIssuesInProject(IReadOnlyCollection<JiraNamedObjectLight> customFields)
        {
            if (customFields == null)
                throw new ArgumentNullException(nameof(customFields));

            var sprintField = customFields.First(cf => cf.Name == _jiraContext.SprintFieldName);
            var epicField = customFields.First(cf => cf.Name == _jiraContext.EpicLinkFieldName);
            var storyPointsField = customFields.First(cf => cf.Name == _jiraContext.StoryPointsFieldName);

            bool useStatus = _jiraContext.ExcludedStatuses?.LongLength > 0;
            bool useCreated = false;
            bool useEpics = _jiraContext.Epics?.Any() ?? false;
            string epics = string.Join(',', _jiraContext.Epics?.Select(i => $"\"{i}\"") ?? Enumerable.Empty<string>());
            string excludedStatuses = string.Join(',',
                _jiraContext.ExcludedStatuses ?? Enumerable.Empty<string>());

            var createdFilterPart =
                $@" AND Created > ""{DateTime.Today.AddDays(-_jiraContext.DaysBackToFetchIssues).Date:yyyy-MM-dd}"" ";
            var statusFilterPart = $" AND Status NOT IN ( {excludedStatuses} ) ";
            var epicFilterPart = $@" AND (""{epicField.Name}"" in ({epics}) OR parent in ({epics}) OR id in ({epics}) ) ";


            var searchOptions =
                new IssueSearchOptions(
                    $@" Project = ""{_jiraContext.Project}""{(useStatus ? statusFilterPart : "")}{(useEpics ? epicFilterPart : "")}{(useCreated ? createdFilterPart : "")} ")
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
                        sprintField.Id,
                        epicField.Id,
                        storyPointsField.Id,
                        "parent",
                        "labels"
                    }
                };

            Console.WriteLine($"JQL: {searchOptions.Jql}");

            var result = new List<IssueLight>();
            IPagedQueryResult<Issue> pages = null;
            do
            {
                pages = await _jira.Issues.GetIssuesFromJqlAsync(searchOptions).ConfigureAwait(false);
                Debug.Assert(pages != null);
                var issuesLight = pages.Select(i => i.ToIssueLight(epicField.Id, storyPointsField.Id, sprintField.Id)).ToList();
                var subTasksForPage = await GetSubTasksAsync(issuesLight);

                result.AddRange(issuesLight);
                result.AddRange(subTasksForPage);

                searchOptions.StartAt = Math.Min(searchOptions.StartAt + pages.ItemsPerPage, pages.TotalItems);

            } while (searchOptions.StartAt < pages.TotalItems);

            return result;
        }

        private async Task<IEnumerable<IssueLight>> GetSubTasksAsync(IReadOnlyCollection<IssueLight> issuesLight)
        {
            var result = new List<IssueLight>();
            var issuesLightDict = issuesLight.ToDictionary(i => i.Key);
            var parents = string.Join(',', issuesLight?.Select(i => $"\"{i.Key}\"") ?? Enumerable.Empty<string>());
            var searchOptions =
                 new IssueSearchOptions($@" Project = ""{_jiraContext.Project}"" AND parent in ({parents})")
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
                         "parent",
                         "labels"
                     }
                 };

            IPagedQueryResult<Issue> pages = null;
            do
            {
                pages = await _jira.Issues.GetIssuesFromJqlAsync(searchOptions).ConfigureAwait(false);
                result.AddRange(pages.Select(subTask => subTask.ToIssueLight(issuesLightDict)));
                searchOptions.StartAt = Math.Min(searchOptions.StartAt + pages.ItemsPerPage, pages.TotalItems);
            } while (searchOptions.StartAt < pages.TotalItems);

            return result;
        }

        public async Task<IEnumerable<IssueLinkLight>> GetLinks(IssueLight rawIssue)
        {
            if (rawIssue == null)
                throw new ArgumentNullException(nameof(rawIssue));

            var linksRaw = await _jira.Links.GetLinksForIssueAsync(rawIssue.Key).ConfigureAwait(false);
            var result = linksRaw.Select(JiraExtensions.ToIssueLinkLight);
            if (!string.IsNullOrWhiteSpace(rawIssue.ParentKey))
                result = result.Prepend(new IssueLinkLight
                {
                    InwardIssueKey = rawIssue.ParentKey, OutwardIssueKey = rawIssue.Key,
                    LinkType = new JiraNamedObjectLight{Id = "parent", Name = "Parent"}
                });
            return result;
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

        public async Task<IEnumerable<JiraNamedObjectLight>> GetCustomFields()
        {
            var customFields = await _jira.Fields.GetCustomFieldsAsync(new CustomFieldFetchOptions
            { ProjectKeys = { _jiraContext.Project } }).ConfigureAwait(false);
            return customFields?.Select(JiraExtensions.ToNamedObjectLight) ?? Enumerable.Empty<JiraNamedObjectLight>();
        }
    }
}
