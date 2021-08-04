using System;

namespace JiraToDgmlDump
{
    public class WorkItem
    {
        public WorkItemReference Reference { get; }

        public WorkItemType Type {get;}

        public string Title { get; }

        public string EpicId { get; }

        public decimal? StoryPoints { get; }

        public string Sprint { get; }

        public string Assignee { get; }

        public string Status { get; }

        public WorkItemReference ParentReference { get; }

        public WorkItem(
            WorkItemReference reference,
            WorkItemType type,
            string title,
            string epicId,
            decimal? storyPoints = null,
            string sprint = null,
            string assignee = null,
            string status = null,
            WorkItemReference parentReference = null)
        {
            if (string.IsNullOrEmpty(title))
                throw new ArgumentException("Value cannot be null or empty.", nameof(title));

            if (string.IsNullOrEmpty(epicId))
                throw new ArgumentException("Value cannot be null or empty.", nameof(epicId));

            Reference = reference ?? throw new ArgumentNullException(nameof(reference));
            Type = type;
            Title = title;
            StoryPoints = storyPoints;
            Sprint = sprint;
            EpicId = epicId;
            Assignee = assignee;
            Status = status;
            ParentReference = parentReference;
        }
    }
}