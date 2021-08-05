namespace JiraToDgmlDump.Jira.ModelRaw
{
    public record OutwardLinkRaw
    {
        public NamedObject Type { get; init; }
        public OutwardIssueRaw OutwardIssue { get; init; }
    }
}