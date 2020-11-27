using System.Collections.Generic;
using System.Threading.Tasks;

namespace JiraToDgmlDump
{
    public interface IJiraService
    {
        Task<(IEnumerable<IssueLight>, IEnumerable<IssueLinkLight>)> GetIssuesWithConnections(string epicKey);
        Task<IEnumerable<JiraUser>> GetUsers();
    }
}
