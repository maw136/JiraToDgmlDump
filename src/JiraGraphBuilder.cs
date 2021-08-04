using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using OpenSoftware.DgmlTools;
using OpenSoftware.DgmlTools.Analyses;
using OpenSoftware.DgmlTools.Builders;
using OpenSoftware.DgmlTools.Model;

namespace JiraToDgmlDump
{
    public class JiraGraphBuilder
    {
        private readonly DgmlBuilder _builder;

        public JiraGraphBuilder(IJiraContext jiraContext)
        {
            var statusColors = jiraContext.StatusColors;
            (string, Color)[] map = BuildStatusColorMapForCategoryColorAnalysis(statusColors);

            _builder = new DgmlBuilder(
                new CategoryColorAnalysis(map),
                new HubNodeAnalysis())
            {
                NodeBuilders = new []
                {
                    MakeNodeBuilder(jiraContext)
                },
                LinkBuilders = new []
                {
                    MakeLinkBuilder(),
                    MakeContainerBuilder()
                },
                CategoryBuilders = new []
                {
                    MakeCategoryBuilder()
                },
                //      StyleBuilders = new StyleBuilder[]
                //      {
                //         // <your style builders>
                //      }
            };
        }

        private (string, Color)[] BuildStatusColorMapForCategoryColorAnalysis(Dictionary<string, StatusColorInfo> statusColors)
            => (from statusColorInfo in statusColors
                from valueStatusId in statusColorInfo.Value.StatusIds
                select (valueStatusId, Color.FromName(statusColorInfo.Value.Color))).ToArray();

        public DirectedGraph BuildGraph(IEnumerable<IssueLight> issues, IEnumerable<IssueLinkLight> connections)
        {
            return _builder.Build(issues, connections);
        }

        private NodeBuilder MakeNodeBuilder(IJiraContext jiraContext)
        {
            Node BuildNode(IssueLight issue)
            {
                Debug.Assert(issue.Key != null);

                return new Node
                {
                    Id = issue.Key,
                    Label = GetIssueLabel(issue, jiraContext.EpicTypeId),
                    Description = issue.Summary,
                    Reference = jiraContext.MakeJiraReference(issue.Key),
                    CategoryRefs = { new CategoryRef { Ref = StatusRefFromStatus(issue.Status) } }
                };
            }

            return new NodeBuilder<IssueLight>(BuildNode);
        }

        private static string GetIssueLabel(IssueLight issue, string epicTypeId)
        {
            if (issue.Type.Id == epicTypeId)
            {
                return $"{issue.Key} {issue.Summary}";
            }
            else
            {
                IEnumerable<string> elements = new[]
                {
                    issue.Key,
                    $"{issue.Summary}",
                    $"{(issue.StoryPoints.HasValue ? $"{issue.StoryPoints.Value} SP" : null)}",
                    $"{issue.Status.Name}",
                    $"{string.Join(", ", issue.Labels.Select(label => $"#{label}"))}"
                }.Where(o => !string.IsNullOrEmpty(o));

                return string.Join(Environment.NewLine, elements);
            }
        }

        private static LinkBuilder MakeLinkBuilder()
        {
            Link BuildLink(IssueLinkLight link)
            {
                return new Link
                {
                    Source = link.OutwardIssueKey,
                    Target = link.InwardIssueKey,
                    Description = link.LinkType.Name,
                };
            }

            return new LinkBuilder<IssueLinkLight>(BuildLink);
        }

        private static LinkBuilder MakeContainerBuilder()
        {
            Link BuildLink(IssueLight issue)
            {
                return new Link()
                {
                    Source = issue.EpicKey,
                    Target = issue.Key,
                    IsContainment = true
                };
            }

            static bool Accept(IssueLight issue)
                => issue.EpicKey != null;

            return new LinkBuilder<IssueLight>(BuildLink, Accept);
        }

        private static CategoryBuilder MakeCategoryBuilder()
        {
            IEnumerable<Category> BuildCategory(IssueLight issue)
            {
                yield return new Category
                {
                    Id = StatusRefFromStatus(issue.Status),
                    Label = issue.Status.Name,
                };
            }

            return new CategoriesBuilder<IssueLight>(BuildCategory);
        }

        private static StyleBuilder MekeStyleBuilder(IReadOnlySet<string> epics)
        {
            Style BuildStyle(IssueLight issue)
            {
                return new Style()
                {
                };
            }

            return new StyleBuilder<IssueLight>(BuildStyle);
        }

        private static string StatusRefFromStatus(JiraNamedObjectLight status)
            => "s:" + status.Id;
    }
}
