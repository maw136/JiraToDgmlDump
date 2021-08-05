namespace JiraToDgmlDump.Jira.Model
{
    public sealed record JiraUser
    {
        public string Key { get; init; }
        public string Name { get; init; }
        public string DisplayName { get; init; }
    }
}
