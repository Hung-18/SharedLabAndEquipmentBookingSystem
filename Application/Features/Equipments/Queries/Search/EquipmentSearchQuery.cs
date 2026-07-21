using Application.DTOs.Equipments;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Equipments.Queries.Search;

public sealed record EquipmentSearchQuery(
    EquipmentSearchRequest Request) : IRequest<PagedEquipmentResponse>;

public sealed class EquipmentSearchQueryHandler : IRequestHandler<EquipmentSearchQuery, PagedEquipmentResponse>
{
    private readonly IEquipmentService _service;

    public EquipmentSearchQueryHandler(IEquipmentService service)
    {
        _service = service;
    }

    public Task<PagedEquipmentResponse> Handle(
        EquipmentSearchQuery request,
        CancellationToken cancellationToken)
    {
        return _service.SearchAsync(request.Request, cancellationToken);
    }
}
