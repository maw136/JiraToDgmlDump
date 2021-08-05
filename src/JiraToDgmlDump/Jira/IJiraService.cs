using System.Collections.Generic;
using System.Threading.Tasks;
using JiraToDgmlDump.Jira.Model;
using JiraToDgmlDump.Jira.PreviousModel;

namespace JiraToDgmlDump.Jira
{
    public interface IJiraService
    {
        Task<(IReadOnlyCollection<IssueLight>, IReadOnlyCollection<IssueLinkLight>)> GetIssuesWithConnections();
        Task<IReadOnlyDictionary<string, JiraUser>> GetUsers();
        IReadOnlyCollection<JiraNamedObjectLight> Statuses { get; }
        IReadOnlyCollection<JiraNamedObjectLight> Types { get; }
        IReadOnlyCollection<JiraNamedObjectLight> CustomFields { get; }
    }
}
