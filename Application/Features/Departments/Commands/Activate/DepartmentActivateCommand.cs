using Application.DTOs.Departments;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Departments.Commands.Activate;

public sealed record DepartmentActivateCommand(
    int DepartmentId) : IRequest<DepartmentResponse>;

public sealed class DepartmentActivateCommandHandler : IRequestHandler<DepartmentActivateCommand, DepartmentResponse>
{
    private readonly IDepartmentService _service;

    public DepartmentActivateCommandHandler(IDepartmentService service)
    {
        _service = service;
    }

    public Task<DepartmentResponse> Handle(
        DepartmentActivateCommand request,
        CancellationToken cancellationToken)
    {
        return _service.ActivateAsync(request.DepartmentId, cancellationToken);
    }
}
