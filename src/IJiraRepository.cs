using System.Collections.Generic;
using System.Threading.Tasks;
using Atlassian.Jira;

namespace JiraToDgmlDump
{
    public interface IJiraRepository
    {
        Task<IList<IssueLight>> GetAllIssuesInProject(string epicKey);
        Task<IList<JiraUser>> GetAllUsersInProject();
        Task<IEnumerable<IssueLinkLight>> GetLinks(IssueLight rawIssue);
        Task<IEnumerable<(string, IEnumerable<IssueLinkLight>)>> GetAllLinks(IList<IssueLight> rawIssues);
        Task<IEnumerable<JiraNamedObjectLight>> GetLinkTypes();
        Task<IEnumerable<JiraNamedObjectLight>> GetStatuses();
        Task<IEnumerable<JiraNamedObjectLight>> GetTypes();
    }
}
