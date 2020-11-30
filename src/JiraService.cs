using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace JiraToDgmlDump
{
    public class JiraService : IJiraService
    {
        private readonly Task _initializeTask;
        private readonly IJiraRepository _repository;
        private readonly JiraResolver _resolver;

        public IReadOnlyCollection<JiraNamedObjectLight> Statuses { get; private set; }
        public IReadOnlyCollection<JiraNamedObjectLight> Types { get; private set; }

        public JiraService(IJiraRepository repository)
        {
            _repository = repository;
            _resolver = new JiraResolver();

            _initializeTask = Initialize();
        }

        public async Task<(IEnumerable<IssueLight>, IEnumerable<IssueLinkLight>)>
            GetIssuesWithConnections()
        {
            await _initializeTask.ConfigureAwait(false);

            var concurrentBag = new List<IssueLinkLight>();

            var rawIssues = await _repository.GetAllIssuesInProject().ConfigureAwait(false);

            Console.WriteLine($"Loaded issues (count): {rawIssues.Count}");

            var getLinks = new TransformManyBlock<IssueLight, IssueLinkLight>(
                async i => await _repository.GetLinks(i),
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }
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

        public async Task<IEnumerable<JiraUser>> GetUsers()
            => await _repository.GetAllUsersInProject().ConfigureAwait(false);

        private async Task Initialize()
        {
            var statuses = await _repository.GetStatuses().ConfigureAwait(false);
            var types = await _repository.GetTypes().ConfigureAwait(false);

            Statuses = new ReadOnlyCollection<JiraNamedObjectLight>(statuses.ToList());
            Types = new ReadOnlyCollection<JiraNamedObjectLight>(types.ToList());
        }
    }
}
