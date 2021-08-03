using System;
using System.Collections.Generic;
using System.Linq;
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

        public static IssueLight ToIssueLight(this Issue issue, string epicFieldId, string storyPointsFieldId)
        {
            return new IssueLight
            {
                Key = issue.Key.Value,
                Assignee = issue.Assignee,
                Reporter = issue.Reporter,
                Created = issue.Created.GetValueOrDefault(DateTime.MinValue),
                Summary = issue.Summary,
                Status = issue.Status
                    .ToNamedObjectLight(), // new JiraNamedObjectLight { Id = issue.Status?.Id, Name = issue.Status?.Name },
                Type = issue.Type
                    .ToNamedObjectLight(), //new JiraNamedObjectLight { Id = issue.Type?.Id, Name = issue.Type?.Name },
                EpicKey = GetCustomField<string>(issue, epicFieldId),
                Labels = issue.Labels.ToList(),
                StoryPoints = GetCustomField<int?>(issue, storyPointsFieldId)
            };
        }

        private static T GetCustomField<T>(Issue issue, string fieldId)
            => issue.AdditionalFields.TryGetValue(fieldId, out JToken fieldJToken) ? fieldJToken.Value<T>() : default;

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
    }
}
