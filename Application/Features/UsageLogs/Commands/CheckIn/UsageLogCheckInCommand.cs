using Application.DTOs.UsageLogs;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.UsageLogs.Commands.CheckIn;

public sealed record UsageLogCheckInCommand(
    CheckInUsageRequest Request) : IRequest<UsageLogResponse>;

public sealed class UsageLogCheckInCommandHandler : IRequestHandler<UsageLogCheckInCommand, UsageLogResponse>
{
    private readonly IUsageLogService _service;

    public UsageLogCheckInCommandHandler(IUsageLogService service)
    {
        _service = service;
    }

    public Task<UsageLogResponse> Handle(
        UsageLogCheckInCommand request,
        CancellationToken cancellationToken)
    {
        return _service.CheckInAsync(request.Request, cancellationToken);
    }
}
