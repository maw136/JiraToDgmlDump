namespace JiraToDgmlDump.Jira.ModelRaw
{
    public record NamedObjectWithDescription
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
    }
}