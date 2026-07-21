using Application.DTOs.AuditLogs;
using Application.Interfaces;
using MediatR;

namespace Application.Features.AuditLogs.Queries.Search;

public sealed record AuditLogSearchQuery(
    AuditLogQueryRequest Request) : IRequest<PagedAuditLogResponse>;

public sealed class AuditLogSearchQueryHandler : IRequestHandler<AuditLogSearchQuery, PagedAuditLogResponse>
{
    private readonly IAuditLogService _service;

    public AuditLogSearchQueryHandler(IAuditLogService service)
    {
        _service = service;
    }

    public Task<PagedAuditLogResponse> Handle(
        AuditLogSearchQuery request,
        CancellationToken cancellationToken)
    {
        return _service.SearchAsync(request.Request, cancellationToken);
    }
}
