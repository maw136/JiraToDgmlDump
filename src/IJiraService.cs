using System.Collections.Generic;
using System.Threading.Tasks;

namespace JiraToDgmlDump
{
    public interface IJiraService
    {
        Task<(IEnumerable<IssueLight>, IEnumerable<IssueLinkLight>)> GetIssuesWithConnections();
        Task<IEnumerable<JiraUser>> GetUsers();

        IReadOnlyCollection<JiraNamedObjectLight> Statuses { get; }
        IReadOnlyCollection<JiraNamedObjectLight> Types { get; }
    }
}
