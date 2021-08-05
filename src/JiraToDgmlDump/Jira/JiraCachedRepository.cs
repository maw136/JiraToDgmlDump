using System.Collections.Generic;
using System.Threading.Tasks;
using JiraToDgmlDump.Jira.Model;
using JiraToDgmlDump.Jira.PreviousModel;

namespace JiraToDgmlDump.Jira
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

        public Task<IReadOnlyCollection<IssueLight>> GetAllIssuesInProject(IReadOnlyCollection<JiraNamedObjectLight> customFields)
            => _diskCache.Wrap("GetIssues", () => _repository.GetAllIssuesInProject(customFields));

        public Task<IList<JiraUser>> GetAllUsersInProject()
            => _diskCache.Wrap("GetUsers", _repository.GetAllUsersInProject);

        public Task<IEnumerable<IssueLinkLight>> GetLinks(IssueLight rawIssue)
            => _diskCache.Wrap($"{rawIssue.Key}_links", () => _repository.GetLinks(rawIssue));

        public Task<IEnumerable<JiraNamedObjectLight>> GetLinkTypes()
            => _diskCache.Wrap("_linkTypes", _repository.GetLinkTypes);

        public Task<IEnumerable<JiraNamedObjectLight>> GetStatuses()
            => _diskCache.Wrap("_statuses", _repository.GetStatuses);

        public Task<IEnumerable<JiraNamedObjectLight>> GetTypes()
            => _diskCache.Wrap("_types", _repository.GetTypes);

        public Task<IEnumerable<JiraNamedObjectLight>> GetCustomFields()
            => _diskCache.Wrap("_customFields", _repository.GetCustomFields);
    }
}
