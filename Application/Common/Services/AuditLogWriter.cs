using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Services
{
    public class AuditLogWriter : IAuditLogWriter
    {
        private static readonly JsonSerializerOptions
            SerializerOptions = new()
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition =
                    JsonIgnoreCondition.WhenWritingNull,
                ReferenceHandler =
                    ReferenceHandler.IgnoreCycles
            };

        private readonly IAuditLogRepository _repository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogWriter(
            IAuditLogRepository repository,
            ICurrentUserService currentUserService,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _currentUserService = currentUserService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task WriteAsync(
            int? actorUserId,
            AuditActionType actionType,
            string entityName,
            int entityId,
            object? oldValue = null,
            object? newValue = null,
            CancellationToken cancellationToken = default)
        {
            int resolvedActorUserId =
                ResolveActorUserId(actorUserId);

            string? oldValueJson =
                SerializeValue(oldValue);

            string? newValueJson =
                SerializeValue(newValue);

            string? ipAddress =
                _httpContextAccessor
                    .HttpContext?
                    .Connection
                    .RemoteIpAddress?
                    .ToString();

            var auditLog = new AuditLog(
                resolvedActorUserId,
                actionType,
                entityName,
                entityId,
                oldValueJson,
                newValueJson,
                ipAddress);

            await _repository.AddAsync(
                auditLog,
                cancellationToken);
        }

        private int ResolveActorUserId(
            int? actorUserId)
        {
            int? resolvedActorId =
                actorUserId
                ?? _currentUserService.UserId;

            if (!resolvedActorId.HasValue
                || resolvedActorId.Value <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được người thực hiện để ghi AuditLog.");
            }

            return resolvedActorId.Value;
        }

        private static string? SerializeValue(
            object? value)
        {
            if (value is null)
            {
                return null;
            }

            return JsonSerializer.Serialize(
                value,
                SerializerOptions);
        }
    }

}
