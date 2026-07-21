using Application.DTOs.Departments;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Departments.Commands.Update;

public sealed record DepartmentUpdateCommand(
    int DepartmentId,
    UpdateDepartmentRequest Request) : IRequest<DepartmentResponse>;

public sealed class DepartmentUpdateCommandHandler : IRequestHandler<DepartmentUpdateCommand, DepartmentResponse>
{
    private readonly IDepartmentService _service;

    public DepartmentUpdateCommandHandler(IDepartmentService service)
    {
        _service = service;
    }

    public Task<DepartmentResponse> Handle(
        DepartmentUpdateCommand request,
        CancellationToken cancellationToken)
    {
        return _service.UpdateAsync(request.DepartmentId, request.Request, cancellationToken);
    }
}
