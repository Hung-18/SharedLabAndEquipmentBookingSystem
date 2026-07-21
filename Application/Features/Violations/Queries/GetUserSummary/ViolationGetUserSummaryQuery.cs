using Application.DTOs.Violations;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Violations.Queries.GetUserSummary;

public sealed record ViolationGetUserSummaryQuery(
    int UserId) : IRequest<UserViolationSummaryResponse>;

public sealed class ViolationGetUserSummaryQueryHandler : IRequestHandler<ViolationGetUserSummaryQuery, UserViolationSummaryResponse>
{
    private readonly IViolationService _service;

    public ViolationGetUserSummaryQueryHandler(IViolationService service)
    {
        _service = service;
    }

    public Task<UserViolationSummaryResponse> Handle(
        ViolationGetUserSummaryQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetUserSummaryAsync(request.UserId, cancellationToken);
    }
}
