using Application.DTOs.Equipments;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class EquipmentService : IEquipmentService
    {
        private readonly IEquipmentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public EquipmentService(
            IEquipmentRepository repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<EquipmentResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var equipments = await _repository.GetAllAsync(
                cancellationToken);

            return equipments
                .Select(equipment => new EquipmentResponse
                {
                    EquipmentId = equipment.EquipmentId,
                    LabId = equipment.LabId,
                    EquipmentName = equipment.EquipmentName,
                    Status = equipment.Status.ToString()
                })
                .ToList();
        }

        public async Task<EquipmentDetailResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var equipment = await _repository.GetDetailAsync(
                id,
                cancellationToken);

            if (equipment is null)
            {
                return null;
            }

            return new EquipmentDetailResponse
            {
                EquipmentId = equipment.EquipmentId,
                LabId = equipment.LabId,
                EquipmentName = equipment.EquipmentName,
                ModelSpecs = equipment.ModelSpecs,
                ImageUrl = equipment.ImageUrl,
                UsageGuideline = equipment.UsageGuideline,
                Status = equipment.Status.ToString()
            };
        }

        public async Task<List<EquipmentResponse>> GetByLabIdAsync(
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

            var equipments = await _repository.GetByLabIdAsync(
                labId,
                cancellationToken);

            return equipments
                .Select(equipment => new EquipmentResponse
                {
                    EquipmentId = equipment.EquipmentId,
                    LabId = equipment.LabId,
                    EquipmentName = equipment.EquipmentName,
                    Status = equipment.Status.ToString()
                })
                .ToList();
        }

        public async Task<EquipmentDetailResponse> CreateAsync(
            CreateEquipmentRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var labRoom = await _unitOfWork.LabRooms.GetByIdAsync(
                request.LabId,
                cancellationToken);

            if (labRoom is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy phòng lab có ID {request.LabId}.");
            }

            if (labRoom.Status == LabRoomStatus.Inactive)
            {
                throw new InvalidOperationException(
                    $"Phòng lab có ID {request.LabId} đã ngừng hoạt động.");
            }

            var equipment = new Equipment(
                request.LabId,
                request.EquipmentName,
                request.ModelSpecs,
                request.ImageUrl,
                request.UsageGuideline);

            await _repository.AddAsync(
                equipment,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            var createdEquipment = await _repository.GetDetailAsync(
                equipment.EquipmentId,
                cancellationToken);

            if (createdEquipment is null)
            {
                throw new InvalidOperationException(
                    "Không thể lấy thông tin thiết bị vừa tạo.");
            }

            return new EquipmentDetailResponse
            {
                EquipmentId = createdEquipment.EquipmentId,
                LabId = createdEquipment.LabId,
                EquipmentName = createdEquipment.EquipmentName,
                ModelSpecs = createdEquipment.ModelSpecs,
                ImageUrl = createdEquipment.ImageUrl,
                UsageGuideline = createdEquipment.UsageGuideline,
                Status = createdEquipment.Status.ToString()
            };
        }

        public async Task UpdateAsync(
            int id,
            UpdateEquipmentRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var equipment = await _repository.GetByIdAsync(
                id,
                cancellationToken);

            if (equipment is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy thiết bị có ID {id}.");
            }

            var labRoom = await _unitOfWork.LabRooms.GetByIdAsync(
                request.LabId,
                cancellationToken);

            if (labRoom is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy phòng lab có ID {request.LabId}.");
            }

            if (labRoom.Status == LabRoomStatus.Inactive)
            {
                throw new InvalidOperationException(
                    $"Phòng lab có ID {request.LabId} đã ngừng hoạt động.");
            }

            equipment.UpdateDetails(
                request.LabId,
                request.EquipmentName,
                request.ModelSpecs,
                request.ImageUrl,
                request.UsageGuideline);

            _repository.Update(equipment);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        // soft delete cho thiết bị, chỉ thay đổi trạng thái của thiết bị thành Retired
        public async Task DeleteAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var equipment = await _repository.GetByIdAsync(
                id,
                cancellationToken);

            if (equipment is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy thiết bị có ID {id}.");
            }

            equipment.Retire();

            _repository.Update(equipment);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
    }
}
