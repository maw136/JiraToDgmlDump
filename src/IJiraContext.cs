using System.Collections.Generic;

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
        string EpicTypeName { get; }
        string EpicLinkName { get; }
        string StoryPointsName { get; }
        bool UseCachedRepo { get; }
        Dictionary<string, StatusColorInfo> StatusColors { get; }

        public string EpicTypeId { get; set; }
    }

    public class StatusColorInfo
    {
        public string Color { get; set; }
        public string[] StatusIds { get; set; }
    }
}
