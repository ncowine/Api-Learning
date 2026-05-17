namespace Testers.Contracts.Execution;

// Route templates (server) + URL builders (client).
//
// Endpoint scanner prefixes templates with /api/v1; builders include it so clients can
// PUT/POST/GET directly without composing the prefix themselves.
public static class Routes
{
    public const string ApiBase = "/api/v1";

    public static class Tasks
    {
        public const string RunsTemplate = "/tasks/{taskDefinitionId:guid}/runs";

        public static string Runs(Guid taskDefinitionId) =>
            $"{ApiBase}/tasks/{taskDefinitionId}/runs";
    }

    public static class Builds
    {
        public const string RunsTemplate = "/builds/{buildId:guid}/runs";

        public static string Runs(Guid buildId) =>
            $"{ApiBase}/builds/{buildId}/runs";
    }

    public static class Runs
    {
        public const string DetailTemplate = "/runs/{taskRunId:guid}";
        public const string CommentsTemplate = "/runs/{taskRunId:guid}/comments";
        public const string BugLinksTemplate = "/runs/{taskRunId:guid}/bug-links";

        public static string Detail(Guid taskRunId) =>
            $"{ApiBase}/runs/{taskRunId}";

        public static string Comments(Guid taskRunId) =>
            $"{ApiBase}/runs/{taskRunId}/comments";

        public static string BugLinks(Guid taskRunId) =>
            $"{ApiBase}/runs/{taskRunId}/bug-links";
    }
}
