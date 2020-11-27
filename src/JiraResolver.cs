using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JiraToDgmlDump
{
    public sealed class JiraResolver
    {
        //    private const double EpsilonTimeForGroupingChangeLogsInMinutes = 1;

        //    public async Task<(IEnumerable<IssuePlanItem>, IEnumerable<IssuePlanItemConnection>)> Resolve(
        //        IEnumerable<(IssueLight issue, IEnumerable<IssueChangeLog> changeLog, IEnumerable<IssueLinkLight> links)> issuesWithChangeLogs,
        //        IList<Atlassian.Jira.JiraUser> users)
        //    {
        //        if (issuesWithChangeLogs == null)
        //            throw new ArgumentNullException(nameof(issuesWithChangeLogs));

        //        if (users == null)
        //            throw new ArgumentNullException(nameof(users));

        //        var planItemsRaw = issuesWithChangeLogs
        //           // .AsParallel()
        //            .Select(i => ResolveSingleIssueChangeLog(i.issue, i.changeLog, users)).ToList();


        //        var planItems = planItemsRaw.SelectMany(kv => kv.planItems);
        //        //var allPlanItemsWithIssues = planItems.SelectMany(t => t);
        //        var connections = ResolveConnections(planItemsRaw);


        //        return (planItems, connections);
        //    }

        //    private IEnumerable<IssuePlanItemConnection> ResolveConnections(IList<(IssueLight issue, IList<IssuePlanItem> planItems)> planItemsLookup)
        //    {

        //        var connections = new List<IssuePlanItemConnection>();

        //        // trivial connections
        //        connections.AddRange(
        //            planItemsLookup.SelectMany(i
        //                => i.planItems.Zip(i.planItems.Skip(1),
        //                    (previous, next)
        //                        => new IssuePlanItemConnection(previous, next))));


        //        return connections;

        //    }

        //    private (IssueLight issue, IList<IssuePlanItem> planItems) ResolveSingleIssueChangeLog(
        //        IssueLight rawIssue,
        //        IEnumerable<IssueChangeLog> changelog,
        //        IList<Atlassian.Jira.JiraUser> users)
        //    {
        //        if (rawIssue == null)
        //            throw new ArgumentNullException(nameof(rawIssue));

        //        if (changelog == null)
        //            throw new ArgumentNullException(nameof(changelog));

        //        if (users == null)
        //            throw new ArgumentNullException(nameof(users));

        //        // The only attributes we care about are Asignee and Status
        //        var changesWeCareAbout =
        //            changelog.Where(FilterIssueChangeLog);//.OrderBy(i => i.CreatedDate);

        //        var changesWeCareAboutFlat = changesWeCareAbout.SelectMany(icl
        //             => icl.Items.Select(icli => (icl.CreatedDate, icli)));

        //        // all changes happening within a x minutes time would be regarded as the one instance
        //        var groupsByMoment =
        //            changesWeCareAboutFlat.GroupBy(c
        //                => c.CreatedDate.Round(TimeSpan.FromMinutes(EpsilonTimeForGroupingChangeLogsInMinutes)));

        //        var assigneeStatusPairs = GenerateAssigneeStatusPairs(groupsByMoment).ToList();

        //        // replaying history

        //        // if less than 2 status changes then history is useless
        //        if (assigneeStatusPairs.Count(t => t.lastStatus != null) < 2)
        //            return (rawIssue,new List<IssuePlanItem>());

        //        // same with no assignee change
        //        //if (assigneeStatusPairs.Count(t => t.lastAssignee != null) < 2)
        //        //    return (rawIssue, new List<IssuePlanItem>());

        //        // assigneeStatusPairs are int he right order
        //        Debug.Assert(assigneeStatusPairs.Zip(assigneeStatusPairs.Skip(1),
        //                (a, b) => new { a, b }).All(p => p.a.moment < p.b.moment));

        //        //Debug.Assert(rawIssue.Created.HasValue, "At the JQL stage they are filtered out, no issue without Created date should be there");

        //        var planItems = new List<IssuePlanItem>();

        //        string previousAssignee =
        //            assigneeStatusPairs.FirstOrDefault(i => i.lastAssignee != null).lastAssignee ?? rawIssue.Assignee;

        //        if (previousAssignee == null)
        //            return (rawIssue, new List<IssuePlanItem>());

        //        var firstTuple = assigneeStatusPairs.FirstOrDefault();
        //        string previousStatus = firstTuple.lastStatus;
        //        DateTime previousMoment = firstTuple.moment;


        //        foreach (var (moment, assignee, status) in assigneeStatusPairs.Skip(1))
        //        {
        //            if (previousAssignee == null || previousStatus == null ||
        //                (previousAssignee == assignee && previousStatus == status))
        //            {
        //                previousAssignee = assignee ?? previousAssignee;
        //                previousStatus = status ?? previousStatus;
        //                continue;
        //            }

        //            var user = GetUserForPlanItem(previousAssignee, users);
        //            var newIssuePlanItem = new IssuePlanItem(rawIssue.Key, rawIssue.Summary, previousMoment, moment, previousStatus, user);
        //            planItems.Add(newIssuePlanItem);

        //            previousMoment = moment;
        //            previousAssignee = assignee ?? previousAssignee;
        //            previousStatus = status ?? previousStatus;
        //        }

        //        return (rawIssue, planItems);
        //    }

        //    private IEnumerable<(DateTime moment, string lastAssignee, string lastStatus)> GenerateAssigneeStatusPairs(
        //        IEnumerable<IGrouping<DateTime, (DateTime created, IssueChangeLogItem changeLogItem)>> groupsByMoment)
        //        => groupsByMoment.OrderBy(i => i.Key).Select(current =>
        //        {
        //            var allInTheGroupOrdered = current.OrderBy(i => i.created).Select(p => p.changeLogItem).ToList();

        //            var lastStatus = allInTheGroupOrdered.LastOrDefault(FilterIssueChangeLogItemsWithStatus);
        //            var lastAssignee = allInTheGroupOrdered.LastOrDefault(FilterIssueChangeLogItemsWithAssignee);

        //            Debug.Assert(lastStatus != null || lastAssignee != null,
        //                "Both conditions should never be false at the same time");

        //            return (current.Key, lastAssignee?.ToId, lastStatus?.ToValue);
        //        });

        //    private Atlassian.Jira.JiraUser GetUserForPlanItem(string id, IList<Atlassian.Jira.JiraUser> users)
        //        => users.SingleOrDefault(u => u.Key == id) ?? new Atlassian.Jira.JiraUser(id, id);


        public async Task<(IEnumerable<IssueLight>, IEnumerable<IssueLinkLight>)>
            Resolve(IEnumerable<(IssueLight issues, IEnumerable<IssueLinkLight>)> connections, IList<JiraUser> users)
        {

            throw new NotImplementedException();
        }
    }
}
