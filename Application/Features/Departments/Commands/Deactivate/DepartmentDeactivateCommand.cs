using Application.DTOs.Departments;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Departments.Commands.Deactivate;

public sealed record DepartmentDeactivateCommand(
    int DepartmentId) : IRequest<bool>;

public sealed class DepartmentDeactivateCommandHandler : IRequestHandler<DepartmentDeactivateCommand, bool>
{
    private readonly IDepartmentService _service;

    public DepartmentDeactivateCommandHandler(IDepartmentService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        DepartmentDeactivateCommand request,
        CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(request.DepartmentId, cancellationToken);
        return true;
    }
}
