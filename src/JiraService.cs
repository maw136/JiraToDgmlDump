using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Atlassian.Jira;

namespace JiraToDgmlDump
{
    public class JiraService : IJiraService
    {
        private readonly Task _initializeTask;
        private readonly IJiraRepository _repository;
        private readonly JiraResolver _resolver;

        private IReadOnlyCollection<JiraNamedObjectLight> _statuses;
        private IReadOnlyCollection<JiraNamedObjectLight> _types;

        public JiraService(IJiraRepository repository)
        {
            _repository = repository;
            _resolver = new JiraResolver();

            _initializeTask = Initialize();
        }

        public async Task<(IEnumerable<IssueLight>, IEnumerable<IssueLinkLight>)>
            GetIssuesWithConnections()
        {
            var rawIssues = await _repository.GetAllIssuesInProject();
            // var users = await _repository.GetAllUsersInProject();
            var links = await _repository.GetAllLinks(rawIssues);

            //var linksLookup = links.ToDictionary(t => t.Item1, t => t.Item2);

            return (rawIssues, links.SelectMany(l => l.Item2));

            //return await _resolver.Resolve(rawIssues.Select(i=>(i, linksLookup[i.Key])), users);
        }

        public async Task<IEnumerable<JiraUser>> GetUsers()
            => await _repository.GetAllUsersInProject();

        private async Task Initialize()
        {
            var statuses = await _repository.GetStatuses();
            var types = await _repository.GetTypes();

            _statuses = new ReadOnlyCollection<JiraNamedObjectLight>(statuses.ToList());
            _types = new ReadOnlyCollection<JiraNamedObjectLight>(types.ToList());
        }
    }
}
