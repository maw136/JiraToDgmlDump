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
        public bool WaitForData { get; set; }
        public Dictionary<string, StatusColorInfo> StatusColors { get; } = new();
        public string[] Epics { get; set; }
        public string[] ExcludedStatuses { get; set; }
        public string EpicTypeName { get; set; }
        public string StoryTypeName { get; set; }
        public string SubTaskTypeName { get; set; }
        public string SprintFieldName { get; set; }
        public string EpicLinkFieldName { get; set; }
        public string StoryPointsFieldName { get; set; }

        public string EpicTypeId { get; set; }
        public string StoryTypeId { get; set; }
        public string SubTaskTypeId { get; set; }
    }
}
