using Application.DTOs.AuditLogs;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using AutoMapper;

namespace Application.Services
{
    public class AuditLogService : IAuditLogService
    {
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        private readonly IAuditLogRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        private readonly IMapper _mapper;

        public AuditLogService(
            IMapper mapper,
            IAuditLogRepository repository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _mapper = mapper;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<PagedAuditLogResponse> SearchAsync(
            AuditLogQueryRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            await ValidateCurrentAdminAsync(cancellationToken);
            ValidateFilters(request);

            int pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
            int pageSize = request.PageSize <= 0
                ? DefaultPageSize
                : Math.Min(request.PageSize, MaxPageSize);

            var result = await _repository.SearchAsync(
                request.UserId,
                request.ActionType,
                request.EntityName,
                request.EntityId,
                request.From,
                request.To,
                pageNumber,
                pageSize,
                cancellationToken);

            return new PagedAuditLogResponse
            {
                TotalCount = result.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = result.TotalCount == 0
                    ? 0
                    : (int)Math.Ceiling(result.TotalCount / (double)pageSize),
                Items = _mapper.Map<List<AuditLogResponse>>(result.Items)
            };
        }

        public async Task<AuditLogResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
                throw new ArgumentException("AuditLogId phải lớn hơn 0.");

            await ValidateCurrentAdminAsync(cancellationToken);
            var auditLog = await _repository.GetByIdAsync(id, cancellationToken);
            return auditLog is null
                ? null
                : _mapper.Map<AuditLogResponse>(auditLog);
        }

        private async Task ValidateCurrentAdminAsync(
            CancellationToken cancellationToken)
        {
            int actorUserId = _currentUserService.GetRequiredUserId();
            var actor = await _unitOfWork.Users.GetUserByIdAsync(
                actorUserId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy người thực hiện có ID {actorUserId}.");

            if (actor.Status != UserStatus.Active)
                throw new InvalidOperationException(
                    "Tài khoản người thực hiện không ở trạng thái Active.");

            if (actor.Role?.RoleName != RoleName.Admin)
                throw new UnauthorizedAccessException(
                    "Chỉ Admin được xem nhật ký hệ thống.");
        }

        private static void ValidateFilters(AuditLogQueryRequest request)
        {
            if (request.UserId is <= 0)
                throw new ArgumentException("UserId phải lớn hơn 0.");
            if (request.EntityId is <= 0)
                throw new ArgumentException("EntityId phải lớn hơn 0.");
            if (request.ActionType.HasValue
                && !Enum.IsDefined(request.ActionType.Value))
                throw new ArgumentException("ActionType không hợp lệ.");
            if (request.From.HasValue && request.To.HasValue
                && request.From.Value > request.To.Value)
                throw new ArgumentException(
                    "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            if (request.EntityName?.Trim().Length > 100)
                throw new ArgumentException(
                    "EntityName không được vượt quá 100 ký tự.");
        }

    }
}
