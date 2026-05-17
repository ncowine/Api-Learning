namespace Testers.SharedKernel.Exceptions;

// Api maps to 403 - caller authenticated but not authorised for this specific action.
public sealed class ForbiddenException(string message) : Exception(message);
