namespace Testers.SharedKernel.Exceptions;

// Api maps to 409 - use for domain-invariant / business-rule violations.
public sealed class ConflictException(string message) : Exception(message);
