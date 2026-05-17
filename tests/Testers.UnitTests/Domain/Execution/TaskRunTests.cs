using Testers.Domain.Execution;
using Testers.Domain.Execution.Events;

namespace Testers.UnitTests.Domain.Execution;

public class TaskRunTests
{
    private static readonly Guid SampleTaskDefId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SampleBuildId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string SampleTesterId = "okta:user-42";
    private static readonly DateTime SampleActionedAt = new(2026, 5, 17, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_assigns_supplied_values()
    {
        var run = new TaskRun(SampleTaskDefId, SampleBuildId, SampleTesterId, TaskOutcome.Pass, "looks good", SampleActionedAt);

        run.Id.ShouldNotBe(Guid.Empty);
        run.TaskDefinitionId.ShouldBe(SampleTaskDefId);
        run.BuildId.ShouldBe(SampleBuildId);
        run.TesterId.ShouldBe(SampleTesterId);
        run.Outcome.ShouldBe(TaskOutcome.Pass);
        run.Note.ShouldBe("looks good");
        run.ActionedAt.ShouldBe(SampleActionedAt);
        run.Comments.ShouldBeEmpty();
        run.BugLinks.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_raises_a_single_TaskRunRecorded_event()
    {
        var run = new TaskRun(SampleTaskDefId, SampleBuildId, SampleTesterId, TaskOutcome.Fail, "broken", SampleActionedAt);

        var evt = run.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TaskRunRecorded>();
        evt.TaskRunId.ShouldBe(run.Id);
        evt.TaskDefinitionId.ShouldBe(SampleTaskDefId);
        evt.BuildId.ShouldBe(SampleBuildId);
        evt.TesterId.ShouldBe(SampleTesterId);
        evt.Outcome.ShouldBe("FAIL");
        evt.Note.ShouldBe("broken");
        evt.OccurredAt.ShouldBe(SampleActionedAt);
        evt.EventId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Constructor_with_empty_TaskDefinitionId_throws()
    {
        var act = () => new TaskRun(Guid.Empty, SampleBuildId, SampleTesterId, TaskOutcome.Skip, null, SampleActionedAt);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("taskDefinitionId");
    }

    [Fact]
    public void Constructor_with_empty_BuildId_throws()
    {
        var act = () => new TaskRun(SampleTaskDefId, Guid.Empty, SampleTesterId, TaskOutcome.Skip, null, SampleActionedAt);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("buildId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_with_missing_TesterId_throws(string? testerId)
    {
        var act = () => new TaskRun(SampleTaskDefId, SampleBuildId, testerId!, TaskOutcome.Skip, null, SampleActionedAt);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("testerId");
    }

    [Fact]
    public void Constructor_with_null_Outcome_throws()
    {
        var act = () => new TaskRun(SampleTaskDefId, SampleBuildId, SampleTesterId, null!, null, SampleActionedAt);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("outcome");
    }

    [Fact]
    public void AddComment_appends_to_collection()
    {
        var run = new TaskRun(SampleTaskDefId, SampleBuildId, SampleTesterId, TaskOutcome.Pass, null, SampleActionedAt);

        var comment = run.AddComment("ack from reviewer", "reviewer-1", SampleActionedAt.AddMinutes(5));

        run.Comments.ShouldHaveSingleItem().ShouldBe(comment);
        comment.Body.ShouldBe("ack from reviewer");
        comment.AuthorId.ShouldBe("reviewer-1");
        comment.AddedAt.ShouldBe(SampleActionedAt.AddMinutes(5));
    }

    [Fact]
    public void LinkBug_appends_to_collection()
    {
        var run = new TaskRun(SampleTaskDefId, SampleBuildId, SampleTesterId, TaskOutcome.Fail, null, SampleActionedAt);

        var link = run.LinkBug("PROJ-1234", "https://jira/PROJ-1234", SampleActionedAt.AddMinutes(2));

        run.BugLinks.ShouldHaveSingleItem().ShouldBe(link);
        link.ExternalBugId.ShouldBe("PROJ-1234");
        link.BugTrackerUrl.ShouldBe("https://jira/PROJ-1234");
    }

    [Fact]
    public void ClearDomainEvents_empties_the_queue()
    {
        var run = new TaskRun(SampleTaskDefId, SampleBuildId, SampleTesterId, TaskOutcome.Skip, null, SampleActionedAt);
        run.DomainEvents.Count.ShouldBe(1);

        run.ClearDomainEvents();

        run.DomainEvents.ShouldBeEmpty();
    }
}
