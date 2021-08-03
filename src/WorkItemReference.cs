using System;

namespace JiraToDgmlDump
{
    public class WorkItemReference
    {
        public string Id { get; }

        public string Url { get; }

        public WorkItemReference(string id, string url)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Value cannot be null or empty.", nameof(id));

            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Value cannot be null or empty.", nameof(url));

            Id = id;
            Url = url;
        }
    }
}