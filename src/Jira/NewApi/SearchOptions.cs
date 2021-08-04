using System;

namespace JiraToDgmlDump.Jira.NewApi
{
    public record SearchOptions
    {
        public string Jql { get; init; }
        public int PageSize { get; init; }
        public string[] Fields { get; init; }
        public int StartAt { get; init; }

        internal SearchOptions()
        {
        }

        public SearchOptions(string jql, int pageSize, string[] fields)
        {
            Jql = jql ?? throw new ArgumentNullException(nameof(jql));
            PageSize = pageSize;
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
            StartAt = 0;
        }
    }
}