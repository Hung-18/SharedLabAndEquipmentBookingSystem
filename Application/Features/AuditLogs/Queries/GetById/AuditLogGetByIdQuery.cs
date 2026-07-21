using Application.DTOs.AuditLogs;
using Application.Interfaces;
using MediatR;

namespace Application.Features.AuditLogs.Queries.GetById;

public sealed record AuditLogGetByIdQuery(
    int Id) : IRequest<AuditLogResponse?>;

public sealed class AuditLogGetByIdQueryHandler : IRequestHandler<AuditLogGetByIdQuery, AuditLogResponse?>
{
    private readonly IAuditLogService _service;

    public AuditLogGetByIdQueryHandler(IAuditLogService service)
    {
        _service = service;
    }

    public Task<AuditLogResponse?> Handle(
        AuditLogGetByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByIdAsync(request.Id, cancellationToken);
    }
}
