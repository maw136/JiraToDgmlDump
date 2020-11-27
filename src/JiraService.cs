using System;
using System.Collections.Concurrent;
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
            await _initializeTask.ConfigureAwait(false);


            var concurrentBag = new ConcurrentBag<IssueLinkLight>();

            var rawIssues = await _repository.GetAllIssuesInProject().ConfigureAwait(false);


            //var batching = new BatchBlock<IssueLight>(16);
            var getLinks = new TransformManyBlock<IssueLight, IssueLinkLight>(async i=> await _repository.GetLinks(i));


            var collect = new ActionBlock<IssueLinkLight>(i => concurrentBag.Add(i));

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            using var l1 = getLinks.LinkTo(collect, linkOptions);


            if (rawIssues.Any(x => !getLinks.Post(x)))
                throw new Exception($"Failure to queue data for processing. Block already has {getLinks.InputCount} items to be processed.");

            getLinks.Complete();
            await getLinks.Completion;

            await collect.Completion;
            // var users = await _repository.GetAllUsersInProject();
            //var links = await _repository.GetAllLinks(rawIssues).ConfigureAwait(false);

            //var linksLookup = links.ToDictionary(t => t.Item1, t => t.Item2);

            return (rawIssues, concurrentBag);

            //return await _resolver.Resolve(rawIssues.Select(i=>(i, linksLookup[i.Key])), users);
        }

        public async Task<IEnumerable<JiraUser>> GetUsers()
            => await _repository.GetAllUsersInProject().ConfigureAwait(false);

        private async Task Initialize()
        {
            var statuses = await _repository.GetStatuses().ConfigureAwait(false);
            var types = await _repository.GetTypes().ConfigureAwait(false);

            _statuses = new ReadOnlyCollection<JiraNamedObjectLight>(statuses.ToList());
            _types = new ReadOnlyCollection<JiraNamedObjectLight>(types.ToList());
        }
    }
}
