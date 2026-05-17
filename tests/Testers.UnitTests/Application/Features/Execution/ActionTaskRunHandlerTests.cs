using Microsoft.EntityFrameworkCore;
using Testers.Application.Abstractions;
using Testers.Application.Features.Execution.ActionTaskRun;
using Testers.Domain.Execution;

namespace Testers.UnitTests.Application.Features.Execution;

/// <summary>
/// Tests the ActionTaskRun handler in isolation. Fakes the four interface dependencies
/// (ITestPlanDbContext, IAppDbContext, ICurrentUser, IClock) via NSubstitute. Verifies:
/// <list type="bullet">
///   <item>Handler maps the command into a correctly-constructed <c>TaskRun</c> aggregate</item>
///   <item>Both DbContexts have <c>SaveChangesAsync</c> called exactly once</item>
///   <item>The result reflects the run's actual id, outcome, and timestamp</item>
///   <item>Unknown outcomes throw (delegating to <c>TaskOutcome.FromName</c>)</item>
/// </list>
/// Integration concerns (interceptors firing, outbox row landing, transaction commit) are
/// covered separately by the integration test harness.
/// </summary>
public class ActionTaskRunHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 5, 17, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_constructs_TaskRun_with_command_values_and_currentUser_id()
    {
        // Arrange
        var testPlanDb = Substitute.For<ITestPlanDbContext>();
        var appDb = Substitute.For<IAppDbContext>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        var taskRunSet = Substitute.For<DbSet<TaskRun>>();
        testPlanDb.Set<TaskRun>().Returns(taskRunSet);
        currentUser.Id.Returns("okta:user-42");
        clock.UtcNow.Returns(FixedNow);

        var handler = new ActionTaskRunHandler(testPlanDb, appDb, currentUser, clock);
        var taskDefId = Guid.NewGuid();
        var buildId = Guid.NewGuid();
        var cmd = new ActionTaskRunCommand(taskDefId, buildId, "PASS", "all good");

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert: result
        result.TaskRunId.ShouldNotBe(Guid.Empty);
        result.Outcome.ShouldBe("PASS");
        result.ActionedAt.ShouldBe(FixedNow);

        // Assert: a single TaskRun added with the expected values
        taskRunSet.Received(1).Add(Arg.Is<TaskRun>(r =>
            r.TaskDefinitionId == taskDefId &&
            r.BuildId == buildId &&
            r.TesterId == "okta:user-42" &&
            r.Outcome == TaskOutcome.Pass &&
            r.Note == "all good" &&
            r.ActionedAt == FixedNow));

        // Assert: both contexts saved (the cross-DB write pattern)
        await testPlanDb.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await appDb.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_calls_TestPlan_save_before_App_save()
    {
        // The interceptor walks both contexts; either order is correct, but our convention
        // is business-write first. Pinning the order here so an accidental swap is caught.
        var testPlanDb = Substitute.For<ITestPlanDbContext>();
        var appDb = Substitute.For<IAppDbContext>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        testPlanDb.Set<TaskRun>().Returns(Substitute.For<DbSet<TaskRun>>());
        currentUser.Id.Returns("tester-1");
        clock.UtcNow.Returns(FixedNow);

        var saveOrder = new List<string>();
        testPlanDb.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => { saveOrder.Add("testPlanDb"); return Task.FromResult(1); });
        appDb.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => { saveOrder.Add("appDb"); return Task.FromResult(1); });

        var handler = new ActionTaskRunHandler(testPlanDb, appDb, currentUser, clock);
        var cmd = new ActionTaskRunCommand(Guid.NewGuid(), Guid.NewGuid(), "SKIP", null);

        await handler.Handle(cmd, CancellationToken.None);

        saveOrder.ShouldBe(new[] { "testPlanDb", "appDb" });
    }

    [Theory]
    [InlineData("PASS")]
    [InlineData("FAIL")]
    [InlineData("SKIP")]
    [InlineData("BLOCK")]
    public async Task Handle_accepts_all_known_outcomes(string outcome)
    {
        var (handler, _, _) = BuildHandler();
        var cmd = new ActionTaskRunCommand(Guid.NewGuid(), Guid.NewGuid(), outcome, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Outcome.ShouldBe(outcome);
    }

    [Fact]
    public async Task Handle_with_unknown_outcome_throws()
    {
        var (handler, _, _) = BuildHandler();
        var cmd = new ActionTaskRunCommand(Guid.NewGuid(), Guid.NewGuid(), "FLOOP", null);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => handler.Handle(cmd, CancellationToken.None));
    }

    private static (ActionTaskRunHandler Handler, ITestPlanDbContext TestPlanDb, IAppDbContext AppDb) BuildHandler()
    {
        var testPlanDb = Substitute.For<ITestPlanDbContext>();
        var appDb = Substitute.For<IAppDbContext>();
        var currentUser = Substitute.For<ICurrentUser>();
        var clock = Substitute.For<IClock>();

        testPlanDb.Set<TaskRun>().Returns(Substitute.For<DbSet<TaskRun>>());
        currentUser.Id.Returns("tester");
        clock.UtcNow.Returns(FixedNow);

        return (new ActionTaskRunHandler(testPlanDb, appDb, currentUser, clock), testPlanDb, appDb);
    }
}
