using Application.DTOs.Maintenances;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly IMaintenanceRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public MaintenanceService(
            IMaintenanceRepository repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<MaintenanceResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var maintenances =
                await _repository.GetAllAsync(cancellationToken);

            return maintenances
                .Select(MapResponse)
                .ToList();
        }

        public async Task<MaintenanceDetailResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var maintenance =
                await _repository.GetDetailAsync(
                    id,
                    cancellationToken);

            return maintenance is null
                ? null
                : MapDetailResponse(maintenance);
        }

        public async Task<List<MaintenanceResponse>> GetByLabIdAsync(
            int labId,
            CancellationToken cancellationToken)
        {
            var labRoom = await _unitOfWork.LabRooms.GetByIdAsync(
                labId,
                cancellationToken);

            if (labRoom is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy phòng lab có ID {labId}.");
            }

            var maintenances =
                await _repository.GetByResourceAsync(
                    labId,
                    null,
                    cancellationToken);

            return maintenances
                .Select(MapResponse)
                .ToList();
        }

        public async Task<List<MaintenanceResponse>> GetByEquipmentIdAsync(
            int equipmentId,
            CancellationToken cancellationToken)
        {
            var equipment =
                await _unitOfWork.Equipments.GetByIdAsync(
                    equipmentId,
                    cancellationToken);

            if (equipment is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy thiết bị có ID {equipmentId}.");
            }

            var maintenances =
                await _repository.GetByResourceAsync(
                    null,
                    equipmentId,
                    cancellationToken);

            return maintenances
                .Select(MapResponse)
                .ToList();
        }

        public async Task<MaintenanceDetailResponse> CreateAsync(
            CreateMaintenanceRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            await ValidateCreatorAsync(
                request.CreatedById,
                cancellationToken);

            await ValidateResourceAsync(
                request.LabId,
                request.EquipmentId,
                cancellationToken);

            await ValidateConflictAsync(
                request.LabId,
                request.EquipmentId,
                request.StartTime,
                request.EndTime,
                null,
                cancellationToken);

            var maintenance = new Maintenance(
                request.CreatedById,
                request.LabId,
                request.EquipmentId,
                request.StartTime,
                request.EndTime,
                request.MaintenanceCost,
                request.Notes);

            await _repository.AddAsync(
                maintenance,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            var createdMaintenance =
                await _repository.GetDetailAsync(
                    maintenance.MaintenanceId,
                    cancellationToken);

            if (createdMaintenance is null)
            {
                throw new InvalidOperationException(
                    "Không thể lấy thông tin lịch bảo trì vừa tạo.");
            }

            return MapDetailResponse(createdMaintenance);
        }

        public async Task UpdateAsync(
            int id,
            UpdateMaintenanceRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var maintenance =
                await _repository.GetByIdAsync(
                    id,
                    cancellationToken);

            if (maintenance is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy lịch bảo trì có ID {id}.");
            }

            await ValidateResourceAsync(
                request.LabId,
                request.EquipmentId,
                cancellationToken);

            await ValidateConflictAsync(
                request.LabId,
                request.EquipmentId,
                request.StartTime,
                request.EndTime,
                id,
                cancellationToken);

            maintenance.UpdateDetails(
                request.LabId,
                request.EquipmentId,
                request.StartTime,
                request.EndTime,
                request.MaintenanceCost,
                request.Notes);

            _repository.Update(maintenance);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task StartAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var maintenance =
                await GetMaintenanceOrThrowAsync(
                    id,
                    cancellationToken);

            maintenance.Start();

            _repository.Update(maintenance);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task CompleteAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var maintenance =
                await GetMaintenanceOrThrowAsync(
                    id,
                    cancellationToken);

            maintenance.Complete();

            _repository.Update(maintenance);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task CancelAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var maintenance =
                await GetMaintenanceOrThrowAsync(
                    id,
                    cancellationToken);

            maintenance.Cancel();

            _repository.Update(maintenance);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        private async Task ValidateCreatorAsync(
            int createdById,
            CancellationToken cancellationToken)
        {
            var creator =
                await _unitOfWork.Users.GetUserByIdAsync(
                    createdById,
                    cancellationToken);

            if (creator is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy người tạo có ID {createdById}.");
            }

            if (creator.Status != UserStatus.Active)
            {
                throw new InvalidOperationException(
                    $"Người dùng có ID {createdById} không hoạt động.");
            }

            var roleName = creator.Role?.RoleName;

            if (roleName != RoleName.Admin &&
                roleName != RoleName.LabManager)
            {
                throw new InvalidOperationException(
                    "Chỉ Admin hoặc LabManager được tạo lịch bảo trì.");
            }
        }

        private async Task ValidateResourceAsync(
            int? labId,
            int? equipmentId,
            CancellationToken cancellationToken)
        {
            if (labId.HasValue == equipmentId.HasValue)
            {
                throw new ArgumentException(
                    "Phải chọn đúng một trong hai: LabId hoặc EquipmentId.");
            }

            if (labId.HasValue)
            {
                var labRoom =
                    await _unitOfWork.LabRooms.GetByIdAsync(
                        labId.Value,
                        cancellationToken);

                if (labRoom is null)
                {
                    throw new KeyNotFoundException(
                        $"Không tìm thấy phòng lab có ID {labId.Value}.");
                }

                if (labRoom.Status == LabRoomStatus.Inactive)
                {
                    throw new InvalidOperationException(
                        $"Phòng lab có ID {labId.Value} đã ngừng hoạt động.");
                }
            }

            if (equipmentId.HasValue)
            {
                var equipment =
                    await _unitOfWork.Equipments.GetByIdAsync(
                        equipmentId.Value,
                        cancellationToken);

                if (equipment is null)
                {
                    throw new KeyNotFoundException(
                        $"Không tìm thấy thiết bị có ID {equipmentId.Value}.");
                }

                if (equipment.Status == EquipmentStatus.Retired)
                {
                    throw new InvalidOperationException(
                        $"Thiết bị có ID {equipmentId.Value} đã ngừng sử dụng.");
                }
            }
        }

        private async Task ValidateConflictAsync(
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            int? excludeMaintenanceId,
            CancellationToken cancellationToken)
        {
            if (startTime >= endTime)
            {
                throw new ArgumentException(
                    "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }

            bool maintenanceConflict =
                await _repository.HasMaintenanceConflictAsync(
                    labId,
                    equipmentId,
                    startTime,
                    endTime,
                    excludeMaintenanceId,
                    cancellationToken);

            if (maintenanceConflict)
            {
                throw new InvalidOperationException(
                    "Khung giờ này đã có một lịch bảo trì khác.");
            }

            bool bookingConflict =
                await _repository.HasBookingConflictForMaintenanceAsync(
                    labId,
                    equipmentId,
                    startTime,
                    endTime,
                    null,
                    true,
                    cancellationToken);

            if (bookingConflict)
            {
                throw new InvalidOperationException(
                    "Khung giờ bảo trì bị trùng với một booking đang hoạt động.");
            }
        }

        private async Task<Maintenance> GetMaintenanceOrThrowAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var maintenance =
                await _repository.GetByIdAsync(
                    id,
                    cancellationToken);

            if (maintenance is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy lịch bảo trì có ID {id}.");
            }

            return maintenance;
        }

        private static MaintenanceResponse MapResponse(
            Maintenance maintenance)
        {
            return new MaintenanceResponse
            {
                MaintenanceId = maintenance.MaintenanceId,
                LabId = maintenance.LabId,
                EquipmentId = maintenance.EquipmentId,
                StartTime = maintenance.StartTime,
                EndTime = maintenance.EndTime,
                Status = maintenance.Status.ToString()
            };
        }

        private static MaintenanceDetailResponse MapDetailResponse(
            Maintenance maintenance)
        {
            return new MaintenanceDetailResponse
            {
                MaintenanceId = maintenance.MaintenanceId,
                LabId = maintenance.LabId,
                EquipmentId = maintenance.EquipmentId,
                CreatedById = maintenance.CreatedById,
                StartTime = maintenance.StartTime,
                EndTime = maintenance.EndTime,
                MaintenanceCost = maintenance.MaintenanceCost,
                Notes = maintenance.Notes,
                Status = maintenance.Status.ToString()
            };
        }
    }
}
