using System;
using System.Collections.Generic;

namespace JiraToDgmlDump
{
    public class IssueLight
    {
        public string Key { get; set; }
        public string Assignee { get; set; }
        public string Reporter { get; set; }
        public DateTime Created { get; set; }
        public string Summary { set; get; }
        public JiraNamedObjectLight Status { get; set; }
        public JiraNamedObjectLight Type { get; set; }
        public string EpicKey { get; set; }
        public List<string> Labels { get; set; }
        public int? StoryPoints { get; set; }
        public string ParentKey { get; set; }
        public string Sprint { get; set; }
    }
}
