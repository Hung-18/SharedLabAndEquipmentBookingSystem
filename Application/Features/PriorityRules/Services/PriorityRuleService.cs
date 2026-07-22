using Application.DTOs.PriorityRules;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using AutoMapper;

namespace Application.Services
{
    public class PriorityRuleService : IPriorityRuleService
    {
        private readonly IPriorityRuleRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogWriter _auditLogWriter;
        private readonly ICurrentUserService _currentUserService;

        private readonly IMapper _mapper;

        public PriorityRuleService(
            IMapper mapper,
            IPriorityRuleRepository repository,
            IUnitOfWork unitOfWork,
            IAuditLogWriter auditLogWriter,
            ICurrentUserService currentUserService)
        {
            _mapper = mapper;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditLogWriter = auditLogWriter;
            _currentUserService = currentUserService;
        }

        public async Task<List<PriorityRuleResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            await GetAuthenticatedUserAsync(cancellationToken);
            var rules = await _repository.GetAllAsync(cancellationToken);
            return rules.OrderBy(x => x.PriorityLevel)
                .ThenBy(x => x.PurposeType)
                .Select(rule => _mapper.Map<PriorityRuleResponse>(rule))
                .ToList();
        }

        public async Task<List<PriorityRuleResponse>> GetActiveAsync(
            CancellationToken cancellationToken)
        {
            await GetAuthenticatedUserAsync(cancellationToken);
            return (await _repository.GetActiveRulesAsync(cancellationToken))
                .Select(rule => _mapper.Map<PriorityRuleResponse>(rule))
                .ToList();
        }

        public async Task<PriorityRuleResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            await GetAuthenticatedUserAsync(cancellationToken);
            var rule = await _repository.GetByIdAsync(id, cancellationToken);
            return rule is null
                ? null
                : _mapper.Map<PriorityRuleResponse>(rule);
        }

        public async Task<PriorityRuleResponse?> GetByPurposeTypeAsync(
            BookingPurposeType purposeType,
            CancellationToken cancellationToken)
        {
            await GetAuthenticatedUserAsync(cancellationToken);
            ValidatePurposeType(purposeType);
            var rule = await _repository.GetByPurposeTypeAsync(
                purposeType,
                cancellationToken);
            return rule is null
                ? null
                : _mapper.Map<PriorityRuleResponse>(rule);
        }

        public async Task<PriorityRuleResponse> CreateAsync(
            CreatePriorityRuleRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var actor = await GetCurrentAdminAsync(cancellationToken);
            ValidatePurposeType(request.PurposeType);

            if (await _repository.IsPurposeTypeExistsAsync(
                    request.PurposeType,
                    null,
                    cancellationToken))
                throw new InvalidOperationException(
                    $"Đã tồn tại quy tắc cho mục đích {request.PurposeType}.");

            var rule = new PriorityRule(
                request.PurposeType,
                request.PriorityLevel,
                request.Description);

            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await _repository.AddAsync(rule, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await WriteAuditAsync(actor.UserId, rule, AuditActionType.Create, null, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);

            return _mapper.Map<PriorityRuleResponse>(rule);
        }

        public async Task UpdateAsync(
            int id,
            UpdatePriorityRuleRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var actor = await GetCurrentAdminAsync(cancellationToken);
            var rule = await GetRuleOrThrowAsync(id, cancellationToken);
            var oldValue = Snapshot(rule);
            rule.UpdateDetails(request.PriorityLevel, request.Description);
            _repository.Update(rule);
            await _auditLogWriter.WriteAsync(
                actor.UserId,
                AuditActionType.Update,
                nameof(PriorityRule),
                rule.PriorityRuleId,
                oldValue,
                Snapshot(rule),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task ActivateAsync(int id, CancellationToken cancellationToken)
        {
            var actor = await GetCurrentAdminAsync(cancellationToken);
            var rule = await GetRuleOrThrowAsync(id, cancellationToken);
            if (rule.Status == PriorityRuleStatus.Active)
                return;
            var oldValue = Snapshot(rule);
            rule.Activate();
            _repository.Update(rule);
            await _auditLogWriter.WriteAsync(
                actor.UserId,
                AuditActionType.Update,
                nameof(PriorityRule),
                rule.PriorityRuleId,
                oldValue,
                Snapshot(rule),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeactivateAsync(int id, CancellationToken cancellationToken)
        {
            var actor = await GetCurrentAdminAsync(cancellationToken);
            var rule = await GetRuleOrThrowAsync(id, cancellationToken);
            if (rule.Status == PriorityRuleStatus.Inactive)
                return;
            var oldValue = Snapshot(rule);
            rule.Deactivate();
            _repository.Update(rule);
            await _auditLogWriter.WriteAsync(
                actor.UserId,
                AuditActionType.Update,
                nameof(PriorityRule),
                rule.PriorityRuleId,
                oldValue,
                Snapshot(rule),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<User> GetAuthenticatedUserAsync(
            CancellationToken cancellationToken)
        {
            int actorId = _currentUserService.GetRequiredUserId();
            var actor = await _unitOfWork.Users.GetUserByIdAsync(actorId, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy người dùng có ID {actorId}.");
            if (actor.Status is UserStatus.Inactive or UserStatus.Locked)
                throw new InvalidOperationException("Tài khoản không được phép thao tác.");
            return actor;
        }

        private async Task<User> GetCurrentAdminAsync(
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            if (actor.Status != UserStatus.Active)
                throw new InvalidOperationException("Tài khoản Admin phải ở trạng thái Active.");
            if (actor.Role?.RoleName != RoleName.Admin)
                throw new UnauthorizedAccessException(
                    "Chỉ Admin được quản lý quy tắc ưu tiên.");
            return actor;
        }

        private async Task<PriorityRule> GetRuleOrThrowAsync(
            int id,
            CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy quy tắc ưu tiên có ID {id}.");
        }

        private async Task WriteAuditAsync(
            int actorId,
            PriorityRule rule,
            AuditActionType actionType,
            object? oldValue,
            CancellationToken cancellationToken)
        {
            await _auditLogWriter.WriteAsync(
                actorId,
                actionType,
                nameof(PriorityRule),
                rule.PriorityRuleId,
                oldValue,
                Snapshot(rule),
                cancellationToken);
        }

        private static object Snapshot(PriorityRule rule) => new
        {
            rule.PriorityRuleId,
            PurposeType = rule.PurposeType.ToString(),
            rule.PriorityLevel,
            rule.Description,
            Status = rule.Status.ToString()
        };

        private static void ValidatePurposeType(BookingPurposeType purposeType)
        {
            if (!Enum.IsDefined(purposeType))
                throw new ArgumentException("PurposeType không hợp lệ.");
        }

    }
}
