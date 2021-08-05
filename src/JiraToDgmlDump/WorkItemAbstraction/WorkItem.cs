using System;

namespace JiraToDgmlDump.WorkItemAbstraction
{
    public record WorkItem
    {
        public WorkItemReference Reference { get; init; }

        public WorkItemType Type {get; init; }

        public string Title { get; init; }

        public string EpicId { get; init; }

        public int? StoryPoints { get; init; }

        public string Sprint { get; init; }

        public string Assignee { get; init; }

        public string Status { get; init; }

        public string ParentTitle { get; init; }

        public WorkItemReference ParentReference { get; init; }

        public WorkItem(
            WorkItemReference reference,
            WorkItemType type,
            string title,
            string epicId,
            int? storyPoints = null,
            string sprint = null,
            string assignee = null,
            string status = null,
            string parentTitle = null,
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
            ParentTitle = parentTitle;
            ParentReference = parentReference;
        }
    }
}