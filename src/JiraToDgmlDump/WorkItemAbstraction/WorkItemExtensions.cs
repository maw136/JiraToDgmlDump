using System;

namespace JiraToDgmlDump.WorkItemAbstraction
{
    public record Row
    {
        public string Id { get; init; }
        public Uri Url { get; init; }
        public WorkItemType Type { get; init; }
        public string Title { get; init; }
        public string EpicId { get; init; }
        public int? StoryPoints { get; init; }
        public string Sprint { get; init; }
        public string Assignee { get; init; }
        public string Status { get; init; }
        public string ParentId { get; init; }
        public Uri ParentUrl { get; init; }

    }

    public static class WorkItemExtensions
    {
        public static Row ToRow(this WorkItem workItem)
        {
            if (workItem == null)
                throw new ArgumentNullException(nameof(workItem));

            return new Row
            {
                Id = workItem.Reference.Id,
                Url = new Uri(workItem.Reference.Url),
                Type = workItem.Type,
                Title = workItem.Title,
                EpicId = workItem.EpicId,
                StoryPoints = workItem.StoryPoints,
                Sprint = workItem.Sprint,
                Assignee = workItem.Assignee,
                Status = workItem.Status,
                ParentId = workItem.ParentReference?.Id,
                ParentUrl = workItem.ParentReference != null ? new Uri(workItem.ParentReference?.Url) : null
            };
        }
    }
}