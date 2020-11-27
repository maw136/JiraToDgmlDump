using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JiraToDgmlDump
{
    public class JiraCachedRepository : IJiraRepository
    {
        private readonly IJiraRepository _repository;
        private readonly DiskCache _diskCache;

        public JiraCachedRepository(IJiraRepository repository, DiskCache diskCache)
        {
            _repository = repository;
            _diskCache = diskCache;
        }

        public async Task<IList<IssueLight>> GetAllIssuesInProject()
            => await _diskCache.Wrap("GetIssues", _repository.GetAllIssuesInProject);

        public async Task<IList<JiraUser>> GetAllUsersInProject()
            => await _diskCache.Wrap("GetUsers", _repository.GetAllUsersInProject);

        public async Task<IEnumerable<IssueLinkLight>> GetLinks(IssueLight rawIssue)
            => await _diskCache.Wrap($"{ rawIssue.Key}_links", () => _repository.GetLinks(rawIssue));

        public async Task<IEnumerable<(string, IEnumerable<IssueLinkLight>)>> GetAllLinks(IList<IssueLight> rawIssues)
            => await Task.WhenAll(rawIssues/*.AsParallel()*/.Select(ProcessLinksInParallel));

        public async Task<IEnumerable<JiraNamedObjectLight>> GetLinkTypes()
            => await _diskCache.Wrap("_linkTypes", _repository.GetLinkTypes);

        public async Task<IEnumerable<JiraNamedObjectLight>> GetStatuses()
            => await _diskCache.Wrap("_statuses", GetStatuses);

        public async Task<IEnumerable<JiraNamedObjectLight>> GetTypes()
            => await _diskCache.Wrap("_types", GetTypes);

        private async Task<(string, IEnumerable<IssueLinkLight>)> ProcessLinksInParallel(IssueLight rawIssue)
        {
            var links = await GetLinks(rawIssue);
            return (rawIssue.Key, links);
        }
    }
}
