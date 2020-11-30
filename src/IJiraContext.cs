namespace JiraToDgmlDump
{
    public interface IJiraContext
    {
        string Login { get; }
        string Password { get; }
        string Uri { get; }
        string Project { get; }
        int DaysBackToFetchIssues { get; }
        string[] Epics { get; }
        string[] LinkTypes { get; set; }
        string[] ExcludedStatuses { get; }
        bool UseCachedRepo { get; }
    }
}
