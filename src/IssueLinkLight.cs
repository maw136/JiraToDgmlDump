namespace JiraToDgmlDump
{
    public class IssueLinkLight
    {
        public JiraNamedObjectLight LinkType { get; set; }

        public string InwardIssueKey { get; set; }

        public string OutwardIssueKey { get; set; }
    }
}
