using Application.DTOs.Departments;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Departments.Commands.Create;

public sealed record DepartmentCreateCommand(
    CreateDepartmentRequest Request) : IRequest<DepartmentResponse>;

public sealed class DepartmentCreateCommandHandler : IRequestHandler<DepartmentCreateCommand, DepartmentResponse>
{
    private readonly IDepartmentService _service;

    public DepartmentCreateCommandHandler(IDepartmentService service)
    {
        _service = service;
    }

    public Task<DepartmentResponse> Handle(
        DepartmentCreateCommand request,
        CancellationToken cancellationToken)
    {
        return _service.CreateAsync(request.Request, cancellationToken);
    }
}
