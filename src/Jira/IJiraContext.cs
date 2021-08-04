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
        string[] ExcludedStatuses { get; }
        string EpicTypeName { get; }
        string StoryTypeName { get; }
        string SubTaskTypeName { get; }
        string SprintFieldName { get; }
        string EpicLinkFieldName { get; }
        string StoryPointsFieldName { get; }
        bool UseCachedRepo { get; }
        bool WaitForData { get; }
        Dictionary<string, StatusColorInfo> StatusColors { get; }

        public string EpicTypeId { get; set; }
        public string StoryTypeId { get; set; }
        public string SubTaskTypeId { get; set; }
    }

    public class StatusColorInfo
    {
        public string Color { get; set; }
        public string[] StatusIds { get; set; }
    }
}
