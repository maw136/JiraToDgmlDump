using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Jira;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace JiraToDgmlDump
{
    internal static class JiraExtensions
    {
        private const int MaxUsersPerRequest = 50;

        public static JiraUser ToJiraUser(this Atlassian.Jira.JiraUser user)
            => new(user.Key, user.DisplayName);

        public static IssueLight ToIssueLight(this Issue issue, string epicFieldId, string storyPointsFieldId, string sprintFieldId)
        {
            return new IssueLight
            {
                Key = issue.Key.Value,
                Assignee = issue.Assignee,
                Reporter = issue.Reporter,
                Created = issue.Created.GetValueOrDefault(DateTime.MinValue),
                Summary = issue.Summary,
                Status = issue.Status
                    .ToNamedObjectLight(),
                Type = issue.Type
                    .ToNamedObjectLight(),
                EpicKey = GetCustomField<string>(issue, epicFieldId),
                Labels = issue.Labels.ToList(),
                StoryPoints = GetCustomField<int?>(issue, storyPointsFieldId),
                ParentKey = issue.ParentIssueKey,
                Sprint = ParseSprint(GetCustomFieldArray<string>(issue, sprintFieldId)),
            };
        }

        private static readonly Regex SprintObjectRegex = new(".+@.+\\[.+,name=(?<sprint>[^,]+),.+]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static string ParseSprint(IEnumerable<string> sprintField)
        {
            return SprintObjectRegex.Match(sprintField.FirstOrDefault() ?? string.Empty).Groups["sprint"].Value;
        }

        public static IssueLight ToIssueLight(this Issue issue, IReadOnlyDictionary<string, IssueLight> parents)
        {
            return new IssueLight
            {
                Key = issue.Key.Value,
                Assignee = issue.Assignee,
                Reporter = issue.Reporter,
                Created = issue.Created.GetValueOrDefault(DateTime.MinValue),
                Summary = issue.Summary,
                Status = issue.Status
                    .ToNamedObjectLight(),
                Type = issue.Type
                    .ToNamedObjectLight(),
                Labels = issue.Labels.ToList(),
                StoryPoints = ParseStoryPoints(issue.Summary),
                ParentKey = issue.ParentIssueKey,
                EpicKey = parents[issue.ParentIssueKey].EpicKey,
                Sprint = parents[issue.ParentIssueKey].Sprint,
            };
        }

        private static readonly Regex StoryPointsRegex = new("[([]\\s?(?<storyPoints>\\d+)\\s?SP\\s?[)\\]]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static int? ParseStoryPoints(string issueSummary)
        {
            return int.TryParse(
                StoryPointsRegex.Match(issueSummary)
                    .Groups["storyPoints"].Value, out int result)
                ? result
                : null;
        }


        private static T GetCustomField<T>(Issue issue, string fieldId)
            => issue.AdditionalFields.TryGetValue(fieldId, out JToken fieldJToken) ? fieldJToken.Value<T>() : default;

        private static IEnumerable<T> GetCustomFieldArray<T>(Issue issue, string fieldId)
            => issue.AdditionalFields.TryGetValue(fieldId, out JToken fieldJToken) ? fieldJToken.ToObject<T[]>() ?? Array.Empty<T>() : Array.Empty<T>();

        public static JiraNamedObjectLight ToNamedObjectLight(this JiraNamedEntity entity)
            => new() { Id = entity?.Id, Name = entity?.Name };

        public static JiraNamedObjectLight ToNamedObjectLight(this JiraNamedResource resource)
            => new() { Id = resource?.Id, Name = resource?.Name };

        public static bool ContainsById(this string[] array, JiraNamedObjectLight value)
            => Array.IndexOf(array, value.Id) >= 0;

        public static async Task<IEnumerable<JiraUser>> GetAllUsers(this Atlassian.Jira.Jira jira,
            IJiraContext jiraContext,
            CancellationToken token = default)
        {
            var url = "rest/api/2/user/assignable/multiProjectSearch";
            int startAt = 0;
            int loaded = 0;
            var fullResult = new List<JiraUser>();
            bool hasMoreData = false;

            do
            {
                var page = await jira.RestClient.ExecuteRequestAsync<IEnumerable<Atlassian.Jira.JiraUser>>(
                    Method.GET,
                    $"{url}?projectKeys={jiraContext.Project}&startAt={startAt}&maxResults={MaxUsersPerRequest}",
                    null,
                    token).ConfigureAwait(false);

                if (page == null)
                    break;

                var previousCount = fullResult.Count;
                fullResult.AddRange(page.Select(ToJiraUser));
                loaded = fullResult.Count - previousCount;

                startAt += loaded;

            } while (loaded >= MaxUsersPerRequest);

            return fullResult;
        }

        public static IssueLinkLight ToIssueLinkLight(IssueLink issueLink)
            => new()
            {
                LinkType = issueLink.LinkType.ToNamedObjectLight(),
                InwardIssueKey = issueLink.InwardIssue.Key.Value,
                OutwardIssueKey = issueLink.OutwardIssue.Key.Value
            };

        public static string MakeJiraReference(this IJiraContext jiraContext, string key)
        {
            return $"{jiraContext.Uri}browse/{key}";
        }

        public static WorkItemReference MakeJiraWorkItemReference(this IJiraContext jiraContext, string key)
        {
            return new WorkItemReference(key, MakeJiraReference(jiraContext, key));
        }

        public static WorkItemType ToWorkItemType(this IJiraContext jiraContext, JiraNamedObjectLight type)
        {
            if (type.Id == jiraContext.SubTaskTypeId)
                return WorkItemType.SubTask;
            if (type.Id == jiraContext.StoryTypeId)
                return WorkItemType.UserStory;
            return WorkItemType.Other;
        }
    }
}
