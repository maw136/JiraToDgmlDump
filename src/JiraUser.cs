using System;

namespace JiraToDgmlDump
{
    public sealed class JiraUser : IEquatable<JiraUser>
    {
        public string Key { get; }
        public string DisplayName { get; }

        public JiraUser(string key, string displayName)
        {
            Key = key;
            DisplayName = displayName;
        }

        public bool Equals(JiraUser other)
        {
            if (other is null)
                return false;
            return ReferenceEquals(this, other) || Key == other.Key;
        }

        public override bool Equals(object obj)
            => ReferenceEquals(this, obj) || (obj is JiraUser other && Equals(other));

        public override int GetHashCode()
            => Key != null ? Key.GetHashCode() : 0;

        public override string ToString()
            => String.IsNullOrWhiteSpace(DisplayName) ? Key : DisplayName;
    }
}
