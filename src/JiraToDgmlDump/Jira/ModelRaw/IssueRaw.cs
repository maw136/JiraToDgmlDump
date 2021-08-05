using System;
using JiraToDgmlDump.Jira.Model;

namespace JiraToDgmlDump.Jira.ModelRaw
{
    public record IssueRaw : IssueRawHeader
    {
        public record IssueRawFields
        {
            public JiraUser Assignee { get; init; }
            public JiraUser Reporter { get; init; }
            public DateTime Created { get; init; }
            public string Summary { get; init; }
            public NamedObjectWithDescription Status { get; init; }
            public NamedObjectWithDescription IssueType { get; init; }
            public string[] Labels { get; init; }
            public IssueRawHeader Parent { get; init; }
            public OutwardLinkRaw IssueLinks { get; init; }
        }

        public IssueRawFields Fields { get; init; }
    }

    public record IssueRawHeader
    {
        public string Key { get; init; }
    }
}
