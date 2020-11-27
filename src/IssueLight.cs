using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlassian.Jira;

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

        //public async Task<IEnumerable<IssueChangeLog>> GetChangeLogsAsync(Atlassian.Jira.Jira jira)
        //    => await jira.Issues.GetChangeLogsAsync(Key);
    }
}
