using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using JiraToDgmlDump.Jira.Model;
using JiraToDgmlDump.Jira.PreviousModel;

namespace JiraToDgmlDump.Jira
{
    public class JiraService : IJiraService
    {
        private readonly IJiraContext _jiraContext;
        private readonly IJiraRepository _repository;

        public IReadOnlyCollection<JiraNamedObjectLight> Statuses { get; private set; }
        public IReadOnlyCollection<JiraNamedObjectLight> Types { get; private set; }
        public IReadOnlyCollection<JiraNamedObjectLight> CustomFields { get; private set; }
        public IReadOnlyCollection<JiraNamedObjectLight> LinkTypes { get; private set; }
        public Task InitializeTask { get; }

        public JiraService(IJiraContext jiraContext, IJiraRepository repository)
        {
            _jiraContext = jiraContext ?? throw new ArgumentNullException(nameof(jiraContext));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));

            InitializeTask = Initialize();
        }

        public async Task<(IReadOnlyCollection<IssueLight>, IReadOnlyCollection<IssueLinkLight>)>
            GetIssuesWithConnections()
        {
            await InitializeTask.ConfigureAwait(false);
            var concurrentBag = new List<IssueLinkLight>();
            var rawIssues = await _repository.GetAllIssuesInProject(CustomFields).ConfigureAwait(false);

            Console.WriteLine($"Loaded issues (count): {rawIssues.Count}");

            var getLinks = new TransformManyBlock<IssueLight, IssueLinkLight>(
                i => _repository.GetLinks(i),
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount/4 }
                );
            var collect = new ActionBlock<IssueLinkLight>(
                i => concurrentBag.Add(i),
                new ExecutionDataflowBlockOptions{MaxDegreeOfParallelism = 1}
                );

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            using var l1 = getLinks.LinkTo(collect, linkOptions);

            if (rawIssues.Any(x => !getLinks.Post(x)))
                throw new Exception($"Failure to queue data for processing. Block already has {getLinks.InputCount} items to be processed.");

            getLinks.Complete();
            await getLinks.Completion;
            await collect.Completion;

            Console.WriteLine($"Loaded links: (count): {concurrentBag.Count}");

            return (rawIssues, concurrentBag);
        }

        public async Task<IReadOnlyDictionary<string, JiraUser>> GetUsers()
        {
            var  usersRaw = await _repository.GetAllUsersInProject().ConfigureAwait(false);
            return usersRaw.Distinct(JiraUserDedupComparer.Instance).ToDictionary(u => u.Key);
        }

        private class JiraUserDedupComparer : IEqualityComparer<JiraUser>
        {
            internal static readonly JiraUserDedupComparer Instance = new();

            public bool Equals(JiraUser x, JiraUser y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (ReferenceEquals(x, null))
                    return false;
                if (ReferenceEquals(y, null))
                    return false;
                return x.Key == y.Key;
            }

            public int GetHashCode(JiraUser obj)
            {
                return obj.Key != null ? obj.Key.GetHashCode() : 0;
            }
        }

        private async Task Initialize()
        {
            IEnumerable<JiraNamedObjectLight>[] tuple = await Task.WhenAll(
                _repository.GetStatuses(),
                _repository.GetTypes(),
                _repository.GetCustomFields(),
                _repository.GetLinkTypes()
                );

            Statuses = new ReadOnlyCollection<JiraNamedObjectLight>(tuple[0].ToList());
            Types = new ReadOnlyCollection<JiraNamedObjectLight>(tuple[1].ToList());
            CustomFields = new ReadOnlyCollection<JiraNamedObjectLight>(tuple[2].ToList());
            LinkTypes = new ReadOnlyCollection<JiraNamedObjectLight>(tuple[3].ToList());

            _jiraContext.EpicTypeId = Types.Single(t => t.Name == _jiraContext.EpicTypeName).Id;
            _jiraContext.StoryTypeId = Types.Single(t => t.Name == _jiraContext.StoryTypeName).Id;
            _jiraContext.SubTaskTypeId = Types.Single(t => t.Name == _jiraContext.SubTaskTypeName).Id;
        }
    }
}
