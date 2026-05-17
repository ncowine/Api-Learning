using Testers.Domain.Abstractions;

namespace Testers.Domain.Execution;

// Owned by TaskRun.
public sealed class Comment : Entity<Guid>
{
    public string Body { get; private set; } = string.Empty;
    public string AuthorId { get; private set; } = string.Empty;
    public DateTime AddedAt { get; private set; }

    private Comment() { }  // EF

    internal Comment(string body, string authorId, DateTime addedAt) : base(Guid.NewGuid())
    {
        Body = body;
        AuthorId = authorId;
        AddedAt = addedAt;
    }
}
