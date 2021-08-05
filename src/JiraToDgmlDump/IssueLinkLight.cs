namespace JiraToDgmlDump
{
    public record IssueLinkLight
    {
        public JiraNamedObjectLight LinkType { get; init; }
        public string InwardIssueKey { get; init; }
        public string OutwardIssueKey { get; init; }
    }
}
