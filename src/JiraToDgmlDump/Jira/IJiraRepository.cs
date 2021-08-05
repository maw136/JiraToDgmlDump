using System.Collections.Generic;
using System.Threading.Tasks;

namespace JiraToDgmlDump
{
    public interface IJiraRepository
    {
        Task<IReadOnlyCollection<IssueLight>> GetAllIssuesInProject(IReadOnlyCollection<JiraNamedObjectLight> customFields);
        Task<IList<JiraUser>> GetAllUsersInProject();
        Task<IEnumerable<IssueLinkLight>> GetLinks(IssueLight rawIssue);
        Task<IEnumerable<JiraNamedObjectLight>> GetLinkTypes();
        Task<IEnumerable<JiraNamedObjectLight>> GetStatuses();
        Task<IEnumerable<JiraNamedObjectLight>> GetTypes();
        Task<IEnumerable<JiraNamedObjectLight>> GetCustomFields();
    }
}
