using System;

namespace JiraToDgmlDump
{
    public sealed record JiraUser
    {
        public string Key { get; init; }
        public string DisplayName { get; init; }

        public JiraUser(string key, string displayName)
        {
            Key = key;
            DisplayName = displayName;
        }
    }
}
