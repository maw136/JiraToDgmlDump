using System.Linq;

namespace JiraToDgmlDump.Jira.NewApi
{
    public static class JqlExtensions
    {
        public static string MakeIssueSerchJql(string project, string[] epics, string[] excludeStatuses)
        {
            var tmp = string.Join(',', epics.Select(e => "\"" + e + "\""));
            return
                $" Project=\"{project}\" AND Status NOT IN ({string.Join(',', excludeStatuses)}) AND (\"Epic Link\" IN ( {tmp} ) OR parent IN ({tmp}) OR Id IN ({tmp}) )";
        }

        public static string MakeSubtaskSearchSql(string project, string[] issues, string[] excludeStatuses)
        {
            var tmp = string.Join(',', issues.Select(e => "\"" + e + "\""));
            return
                $" Project=\"{project}\" AND Status NOT IN ({string.Join(',', excludeStatuses)}) AND parent IN ({tmp})";
        }
    }
}