namespace Testers.SharedKernel.Abstractions;

// void-equivalent for IRequest<Unit> so generic handlers stay uniform.
public readonly record struct Unit
{
    public static readonly Unit Value = default;
    public static Task<Unit> Task => System.Threading.Tasks.Task.FromResult(Value);
}
