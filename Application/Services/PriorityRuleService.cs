using Application.DTOs.PriorityRules;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class PriorityRuleService : IPriorityRuleService
    {
        private readonly IPriorityRuleRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public PriorityRuleService(
            IPriorityRuleRepository repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<PriorityRuleResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var rules = await _repository.GetAllAsync(
                cancellationToken);

            return rules
                .OrderBy(x => x.PriorityLevel)
                .ThenBy(x => x.PurposeType)
                .Select(MapResponse)
                .ToList();
        }

        public async Task<List<PriorityRuleResponse>> GetActiveAsync(
            CancellationToken cancellationToken)
        {
            var rules = await _repository.GetActiveRulesAsync(
                cancellationToken);

            return rules
                .Select(MapResponse)
                .ToList();
        }

        public async Task<PriorityRuleResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var rule = await _repository.GetByIdAsync(
                id,
                cancellationToken);

            return rule is null
                ? null
                : MapResponse(rule);
        }

        public async Task<PriorityRuleResponse?> GetByPurposeTypeAsync(
            BookingPurposeType purposeType,
            CancellationToken cancellationToken)
        {
            ValidatePurposeType(purposeType);

            var rule =
                await _repository.GetByPurposeTypeAsync(
                    purposeType,
                    cancellationToken);

            return rule is null
                ? null
                : MapResponse(rule);
        }

        public async Task<PriorityRuleResponse> CreateAsync(
            CreatePriorityRuleRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            await ValidateAdminAsync(
                request.ActorUserId,
                cancellationToken);

            ValidatePurposeType(request.PurposeType);

            bool purposeTypeExists =
                await _repository.IsPurposeTypeExistsAsync(
                    request.PurposeType,
                    null,
                    cancellationToken);

            if (purposeTypeExists)
            {
                throw new InvalidOperationException(
                    $"Đã tồn tại quy tắc cho mục đích {request.PurposeType}.");
            }

            var rule = new PriorityRule(
                request.PurposeType,
                request.PriorityLevel,
                request.Description);

            await _repository.AddAsync(
                rule,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return MapResponse(rule);
        }

        public async Task UpdateAsync(
            int id,
            UpdatePriorityRuleRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            await ValidateAdminAsync(
                request.ActorUserId,
                cancellationToken);

            var rule = await GetRuleOrThrowAsync(
                id,
                cancellationToken);

            rule.UpdateDetails(
                request.PriorityLevel,
                request.Description);

            _repository.Update(rule);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task ActivateAsync(
            int id,
            PriorityRuleActionRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            await ValidateAdminAsync(
                request.ActorUserId,
                cancellationToken);

            var rule = await GetRuleOrThrowAsync(
                id,
                cancellationToken);

            rule.Activate();

            _repository.Update(rule);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task DeactivateAsync(
            int id,
            PriorityRuleActionRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            await ValidateAdminAsync(
                request.ActorUserId,
                cancellationToken);

            var rule = await GetRuleOrThrowAsync(
                id,
                cancellationToken);

            rule.Deactivate();

            _repository.Update(rule);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        private async Task ValidateAdminAsync(
            int actorUserId,
            CancellationToken cancellationToken)
        {
            var actor =
                await _unitOfWork.Users.GetUserByIdAsync(
                    actorUserId,
                    cancellationToken);

            if (actor is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy người thực hiện có ID {actorUserId}.");
            }

            if (actor.Status != UserStatus.Active)
            {
                throw new InvalidOperationException(
                    "Người thực hiện hiện không ở trạng thái Active.");
            }

            if (actor.Role?.RoleName != RoleName.Admin)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ Admin được quản lý quy tắc ưu tiên.");
            }
        }

        private async Task<PriorityRule> GetRuleOrThrowAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var rule = await _repository.GetByIdAsync(
                id,
                cancellationToken);

            if (rule is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy quy tắc ưu tiên có ID {id}.");
            }

            return rule;
        }

        private static void ValidatePurposeType(
            BookingPurposeType purposeType)
        {
            if (!Enum.IsDefined(
                    typeof(BookingPurposeType),
                    purposeType))
            {
                throw new ArgumentException(
                    "Loại mục đích đặt lịch không hợp lệ.");
            }
        }

        private static PriorityRuleResponse MapResponse(
            PriorityRule rule)
        {
            return new PriorityRuleResponse
            {
                PriorityRuleId = rule.PriorityRuleId,
                PurposeType = rule.PurposeType.ToString(),
                PriorityLevel = rule.PriorityLevel,
                Description = rule.Description,
                Status = rule.Status.ToString()
            };
        }
    }

}
