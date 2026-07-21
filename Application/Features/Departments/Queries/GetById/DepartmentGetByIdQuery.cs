using Application.DTOs.Departments;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Departments.Queries.GetById;

public sealed record DepartmentGetByIdQuery(
    int DepartmentId) : IRequest<DepartmentResponse?>;

public sealed class DepartmentGetByIdQueryHandler : IRequestHandler<DepartmentGetByIdQuery, DepartmentResponse?>
{
    private readonly IDepartmentService _service;

    public DepartmentGetByIdQueryHandler(IDepartmentService service)
    {
        _service = service;
    }

    public Task<DepartmentResponse?> Handle(
        DepartmentGetByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByIdAsync(request.DepartmentId, cancellationToken);
    }
}
