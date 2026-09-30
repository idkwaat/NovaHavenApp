using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Api.Infrastructure;

namespace NovaHaven.Api.Filters;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PersistenceConflictExceptionFilter : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var (statusCode, title) = context.Exception switch
        {
            DbUpdateConcurrencyException => (StatusCodes.Status412PreconditionFailed,
                "Resource changed; reload before editing."),
            DbUpdateException databaseException when PostgreSqlConflict.IsUniqueViolation(databaseException) =>
                (StatusCodes.Status409Conflict, "A record with the same unique value already exists."),
            DbUpdateException databaseException when PostgreSqlConflict.IsForeignKeyViolation(databaseException) =>
                (StatusCodes.Status409Conflict, "The record is still referenced and cannot be deleted."),
            var exception when PostgreSqlConflict.IsDeadlock(exception)
                || PostgreSqlConflict.IsSerializationFailure(exception) =>
                (StatusCodes.Status409Conflict, "Concurrent update conflict. Reload and retry."),
            _ => (0, string.Empty)
        };

        if (statusCode == 0) return;

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = title
        })
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
        context.ExceptionHandled = true;
    }
}
