using Testers.Domain.Abstractions;

namespace Testers.Domain.Execution;

/// <summary>
/// A comment a tester adds to a TaskRun (after-the-fact context, screenshots-as-text,
/// follow-up notes). Owned by <see cref="TaskRun"/>; created/persisted/deleted with its parent.
/// </summary>
public sealed class Comment : Entity<Guid>
{
    public string Body { get; private set; } = string.Empty;

    public string AuthorId { get; private set; } = string.Empty;

    public DateTime AddedAt { get; private set; }

    private Comment()
    {
        // EF Core materialisation only.
    }

    internal Comment(string body, string authorId, DateTime addedAt) : base(Guid.NewGuid())
    {
        Body = body;
        AuthorId = authorId;
        AddedAt = addedAt;
    }
}
