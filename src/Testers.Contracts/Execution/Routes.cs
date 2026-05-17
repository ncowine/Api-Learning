namespace Testers.Contracts.Execution;

// Route templates (server) + URL builders (client).
//
// Endpoint scanner prefixes templates with /api/v1; builders include it so clients can
// POST/GET directly.
public static class Routes
{
    public const string ApiBase = "/api/v1";

    public static class Tasks
    {
        // Templates: used by server endpoints. Relative to /api/v1.
        public const string RunsTemplate = "/tasks/{taskDefinitionId:guid}/runs";
        public const string RunDetailTemplate = "/tasks/{taskDefinitionId:guid}/runs/{taskRunId:guid}";

        // Builders: used by clients (and by Results.Created on the server).
        public static string Runs(Guid taskDefinitionId) =>
            $"{ApiBase}/tasks/{taskDefinitionId}/runs";

        public static string RunDetail(Guid taskDefinitionId, Guid taskRunId) =>
            $"{ApiBase}/tasks/{taskDefinitionId}/runs/{taskRunId}";
    }
}
