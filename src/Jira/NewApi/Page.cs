using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace JiraToDgmlDump.Jira.NewApi
{
    public sealed class Page : IDisposable
    {
        public JsonDocument RawPage { get; }

        public int StartAt { get; }
        public int MaxResults { get; }
        public int Total { get; }

        public Page([DisallowNull] JsonDocument rawPage)
        {
            RawPage = rawPage ?? throw new ArgumentNullException(nameof(rawPage));

            StartAt = rawPage.RootElement.GetProperty("startAt").GetInt32();
            MaxResults = rawPage.RootElement.GetProperty("maxResults").GetInt32();
            Total = rawPage.RootElement.GetProperty("total").GetInt32();
        }

        public void Dispose()
        {
            RawPage?.Dispose();
        }
    }
}