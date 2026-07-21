using Application.DTOs.Departments;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Departments.Queries.GetAll;

public sealed record DepartmentGetAllQuery(
    bool ActiveOnly) : IRequest<List<DepartmentResponse>>;

public sealed class DepartmentGetAllQueryHandler : IRequestHandler<DepartmentGetAllQuery, List<DepartmentResponse>>
{
    private readonly IDepartmentService _service;

    public DepartmentGetAllQueryHandler(IDepartmentService service)
    {
        _service = service;
    }

    public Task<List<DepartmentResponse>> Handle(
        DepartmentGetAllQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetAllAsync(request.ActiveOnly, cancellationToken);
    }
}
