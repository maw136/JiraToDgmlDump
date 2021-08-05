using System;
using System.Collections.Generic;

namespace JiraToDgmlDump
{
    public record IssueLight
    {
        public string Key { get; init; }
        public string Assignee { get; init; }
        public string Reporter { get; init; }
        public DateTime Created { get; init; }
        public string Summary { init; get; }
        public JiraNamedObjectLight Status { get; init; }
        public JiraNamedObjectLight Type { get; init; }
        public string EpicKey { get; init; }
        public List<string> Labels { get; init; }
        public int? StoryPoints { get; init; }
        public string ParentKey { get; init; }
        public string Sprint { get; init; }
    }
}
