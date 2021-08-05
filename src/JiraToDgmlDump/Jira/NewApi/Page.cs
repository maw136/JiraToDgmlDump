using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace JiraToDgmlDump.Jira.NewApi
{
    public sealed class Page
    {
        public JsonObject RawPage { get; }

        public int StartAt { get; }
        public int MaxResults { get; }
        public int Total { get; }

        public Page([DisallowNull] JsonObject rawPage)
        {
            RawPage = rawPage ?? throw new ArgumentNullException(nameof(rawPage));

            StartAt = (int)rawPage["startAt"];
            MaxResults = (int)rawPage["maxResults"];
            Total = (int)rawPage["total"];
        }
    }
}