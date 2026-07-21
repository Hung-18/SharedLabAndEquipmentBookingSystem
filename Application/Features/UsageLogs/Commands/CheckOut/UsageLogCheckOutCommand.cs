using Application.DTOs.UsageLogs;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.UsageLogs.Commands.CheckOut;

public sealed record UsageLogCheckOutCommand(
    int LogId,
    CheckOutUsageRequest Request) : IRequest<UsageLogResponse>;

public sealed class UsageLogCheckOutCommandHandler : IRequestHandler<UsageLogCheckOutCommand, UsageLogResponse>
{
    private readonly IUsageLogService _service;

    public UsageLogCheckOutCommandHandler(IUsageLogService service)
    {
        _service = service;
    }

    public Task<UsageLogResponse> Handle(
        UsageLogCheckOutCommand request,
        CancellationToken cancellationToken)
    {
        return _service.CheckOutAsync(request.LogId, request.Request, cancellationToken);
    }
}
