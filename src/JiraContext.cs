using System.Collections.Generic;

namespace JiraToDgmlDump
{
    public class JiraContext : IJiraContext
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string Uri { get; set; }
        public string Project { get; set; }
        public int DaysBackToFetchIssues { get; set; }
        public bool UseCachedRepo { get; set; }
        public Dictionary<string, StatusColorInfo> StatusColors { get; } = new();
        public string[] Epics { get; set; }
        public string[] ExcludedStatuses { get; set; }
        public string EpicTypeName { get; set; }
        public string EpicLinkName { get; set; }
        public string StoryPointsName { get; set; }

        public string[] LinkTypes { get; set; }
    }
}
