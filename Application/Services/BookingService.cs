using Application.DTOs.Booking;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public BookingService(
            IBookingRepository repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<BookingResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var bookings = await _repository.GetAllAsync(cancellationToken);
            return bookings.Select(MapResponse).ToList();
        }

        public async Task<BookingDetailResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var booking = await _repository.GetDetailAsync(id, cancellationToken);
            return booking is null ? null : MapDetailResponse(booking);
        }

        public async Task<List<BookingResponse>> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetUserByIdAsync(userId, cancellationToken);
            if (user is null)
                throw new KeyNotFoundException($"Không tìm thấy người dùng có ID {userId}.");

            var bookings = await _repository.GetByUserIdAsync(userId, cancellationToken);
            return bookings.Select(MapResponse).ToList();
        }

        public async Task<List<BookingResponse>> GetPendingAsync(
            CancellationToken cancellationToken)
        {
            var bookings = await _repository.GetPendingBookingsAsync(cancellationToken);
            return bookings.Select(MapResponse).ToList();
        }

        public async Task<List<BookingResponse>> GetCalendarAsync(
            DateTime from,
            DateTime to,
            int? labId,
            int? equipmentId,
            CancellationToken cancellationToken)
        {
            if (from >= to)
                throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");

            if (labId.HasValue && equipmentId.HasValue)
                throw new ArgumentException("Chỉ được lọc theo LabId hoặc EquipmentId, không truyền cả hai.");

            var bookings = await _repository.GetCalendarAsync(
                from,
                to,
                labId,
                equipmentId,
                cancellationToken);

            return bookings.Select(MapResponse).ToList();
        }

        public async Task<BookingDetailResponse> CreateAsync(
            CreateBookingRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateTime(request.StartTime, request.EndTime, requireFuture: true);

            await ValidateRequesterAsync(request.UserId, cancellationToken);

            var priorityRule = await GetPriorityRuleAsync(
                request.PurposeType,
                cancellationToken);

            await ValidateItemsAsync(
                request.Items,
                request.StartTime,
                request.EndTime,
                excludeBookingId: null,
                cancellationToken);

            var booking = new Booking(
                request.UserId,
                priorityRule?.PriorityRuleId,
                request.PurposeType,
                request.PurposeDescription,
                request.StartTime,
                request.EndTime);

            foreach (var item in request.Items)
            {
                if (item.ResourceType == ResourceType.LabRoom)
                {
                    booking.AddLabRoom(item.LabId!.Value, item.Note);
                }
                else
                {
                    booking.AddEquipment(item.EquipmentId!.Value, item.Note);
                }
            }

            await _repository.AddAsync(booking, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var createdBooking = await _repository.GetDetailAsync(
                booking.BookingId,
                cancellationToken);

            if (createdBooking is null)
                throw new InvalidOperationException("Không thể lấy thông tin booking vừa tạo.");

            return MapDetailResponse(createdBooking);
        }

        public async Task UpdateAsync(
            int id,
            UpdateBookingRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateTime(request.StartTime, request.EndTime, requireFuture: true);

            var booking = await _repository.GetDetailAsync(id, cancellationToken);
            if (booking is null)
                throw new KeyNotFoundException($"Không tìm thấy booking có ID {id}.");

            var priorityRule = await GetPriorityRuleAsync(
                request.PurposeType,
                cancellationToken);

            var currentItems = booking.BookingItems
                .Select(x => new BookingItemRequest
                {
                    ResourceType = x.ResourceType,
                    LabId = x.LabId,
                    EquipmentId = x.EquipmentId,
                    Note = x.Note
                })
                .ToList();

            await ValidateItemsAsync(
                currentItems,
                request.StartTime,
                request.EndTime,
                excludeBookingId: id,
                cancellationToken);

            booking.UpdateDetails(
                priorityRule?.PriorityRuleId,
                request.PurposeType,
                request.PurposeDescription,
                request.StartTime,
                request.EndTime);

            _repository.Update(booking);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task ApproveAsync(
            int id,
            BookingActionRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var approver = await ValidateManagerOrAdminAsync(
                request.UserId,
                cancellationToken);

            var booking = await GetBookingOrThrowAsync(id, cancellationToken);

            var items = booking.BookingItems
                .Select(x => new BookingItemRequest
                {
                    ResourceType = x.ResourceType,
                    LabId = x.LabId,
                    EquipmentId = x.EquipmentId,
                    Note = x.Note
                })
                .ToList();

            await ValidateItemsAsync(
                items,
                booking.StartTime,
                booking.EndTime,
                excludeBookingId: id,
                cancellationToken);

            booking.Approve(approver.UserId);
            _repository.Update(booking);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task RejectAsync(
            int id,
            RejectBookingRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var approver = await ValidateManagerOrAdminAsync(
                request.UserId,
                cancellationToken);

            var booking = await GetBookingOrThrowAsync(id, cancellationToken);
            booking.Reject(approver.UserId, request.RejectionReason);

            _repository.Update(booking);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task CancelAsync(
            int id,
            BookingActionRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var actor = await ValidateActiveUserAsync(request.UserId, cancellationToken);
            var booking = await GetBookingOrThrowAsync(id, cancellationToken);

            bool isOwner = booking.UserId == actor.UserId;
            bool isPrivileged = actor.Role?.RoleName is RoleName.Admin or RoleName.LabManager;

            if (!isOwner && !isPrivileged)
                throw new UnauthorizedAccessException("Bạn không có quyền hủy booking này.");

            booking.Cancel();
            _repository.Update(booking);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task CompleteAsync(
            int id,
            BookingActionRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            await ValidateManagerOrAdminAsync(request.UserId, cancellationToken);

            var booking = await GetBookingOrThrowAsync(id, cancellationToken);
            booking.Complete();

            _repository.Update(booking);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkNoShowAsync(
            int id,
            BookingActionRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            await ValidateManagerOrAdminAsync(request.UserId, cancellationToken);

            var booking = await GetBookingOrThrowAsync(id, cancellationToken);
            booking.MarkNoShow();

            _repository.Update(booking);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task ValidateRequesterAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetUserByIdAsync(userId, cancellationToken);

            if (user is null)
                throw new KeyNotFoundException($"Không tìm thấy người dùng có ID {userId}.");

            if (user.Status == UserStatus.Restricted)
            {
                if (user.RestrictionUntil.HasValue && user.RestrictionUntil.Value > DateTime.UtcNow)
                {
                    throw new InvalidOperationException(
                        $"Người dùng bị hạn chế đặt lịch đến {user.RestrictionUntil:yyyy-MM-dd HH:mm:ss} UTC.");
                }

                user.Unlock();
                _unitOfWork.Users.Update(user);
            }
            else if (user.Status != UserStatus.Active)
            {
                throw new InvalidOperationException("Tài khoản không ở trạng thái hoạt động.");
            }
        }

        private async Task<User> ValidateActiveUserAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetUserByIdAsync(userId, cancellationToken);

            if (user is null)
                throw new KeyNotFoundException($"Không tìm thấy người dùng có ID {userId}.");

            if (user.Status != UserStatus.Active)
                throw new InvalidOperationException("Tài khoản không ở trạng thái hoạt động.");

            return user;
        }

        private async Task<User> ValidateManagerOrAdminAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var user = await ValidateActiveUserAsync(userId, cancellationToken);

            if (user.Role?.RoleName is not RoleName.Admin and not RoleName.LabManager)
                throw new UnauthorizedAccessException("Chỉ Admin hoặc LabManager được thực hiện thao tác này.");

            return user;
        }

        private async Task<PriorityRule?> GetPriorityRuleAsync(
            BookingPurposeType purposeType,
            CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(purposeType))
                throw new ArgumentException("Loại mục đích đặt lịch không hợp lệ.");

            var rule = await _unitOfWork.PriorityRules.GetByPurposeTypeAsync(
                purposeType,
                cancellationToken);

            return rule?.Status == PriorityRuleStatus.Active ? rule : null;
        }

        private async Task ValidateItemsAsync(
            IReadOnlyCollection<BookingItemRequest> items,
            DateTime startTime,
            DateTime endTime,
            int? excludeBookingId,
            CancellationToken cancellationToken)
        {
            if (items is null || items.Count == 0)
                throw new ArgumentException("Booking phải có ít nhất một phòng lab hoặc thiết bị.");

            var resourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                if (!Enum.IsDefined(item.ResourceType))
                    throw new ArgumentException("Loại tài nguyên không hợp lệ.");

                if (item.ResourceType == ResourceType.LabRoom)
                {
                    if (!item.LabId.HasValue || item.LabId.Value <= 0 || item.EquipmentId.HasValue)
                        throw new ArgumentException("Item phòng lab phải có LabId hợp lệ và EquipmentId phải null.");

                    if (!resourceKeys.Add($"LAB:{item.LabId.Value}"))
                        throw new ArgumentException($"Phòng lab ID {item.LabId.Value} bị lặp trong booking.");

                    var lab = await _unitOfWork.LabRooms.GetByIdAsync(
                        item.LabId.Value,
                        cancellationToken);

                    if (lab is null)
                        throw new KeyNotFoundException($"Không tìm thấy phòng lab có ID {item.LabId.Value}.");

                    if (lab.Status != LabRoomStatus.Available)
                        throw new InvalidOperationException($"Phòng lab ID {item.LabId.Value} hiện không khả dụng.");

                    await EnsureNoConflictAsync(
                        item.LabId.Value,
                        null,
                        startTime,
                        endTime,
                        excludeBookingId,
                        cancellationToken);
                }
                else
                {
                    if (!item.EquipmentId.HasValue || item.EquipmentId.Value <= 0 || item.LabId.HasValue)
                        throw new ArgumentException("Item thiết bị phải có EquipmentId hợp lệ và LabId phải null.");

                    if (!resourceKeys.Add($"EQUIPMENT:{item.EquipmentId.Value}"))
                        throw new ArgumentException($"Thiết bị ID {item.EquipmentId.Value} bị lặp trong booking.");

                    var equipment = await _unitOfWork.Equipments.GetDetailAsync(
                        item.EquipmentId.Value,
                        cancellationToken);

                    if (equipment is null)
                        throw new KeyNotFoundException($"Không tìm thấy thiết bị có ID {item.EquipmentId.Value}.");

                    if (equipment.Status != EquipmentStatus.Available)
                        throw new InvalidOperationException($"Thiết bị ID {item.EquipmentId.Value} hiện không khả dụng.");

                    if (equipment.LabRoom is not null && equipment.LabRoom.Status != LabRoomStatus.Available)
                        throw new InvalidOperationException("Phòng chứa thiết bị hiện không khả dụng.");

                    await EnsureNoConflictAsync(
                        null,
                        item.EquipmentId.Value,
                        startTime,
                        endTime,
                        excludeBookingId,
                        cancellationToken);
                }
            }
        }

        private async Task EnsureNoConflictAsync(
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            int? excludeBookingId,
            CancellationToken cancellationToken)
        {
            bool bookingConflict = await _repository.HasBookingConflictAsync(
                labId,
                equipmentId,
                startTime,
                endTime,
                excludeBookingId,
                includePending: true,
                cancellationToken: cancellationToken);

            if (bookingConflict)
                throw new InvalidOperationException("Khung giờ này đã có booking khác.");

            bool maintenanceConflict = await _unitOfWork.Maintenances.HasMaintenanceConflictAsync(
                labId,
                equipmentId,
                startTime,
                endTime,
                excludeMaintenanceId: null,
                cancellationToken: cancellationToken);

            if (maintenanceConflict)
                throw new InvalidOperationException("Khung giờ này đang có lịch bảo trì.");
        }

        private async Task<Booking> GetBookingOrThrowAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var booking = await _repository.GetDetailAsync(id, cancellationToken);

            if (booking is null)
                throw new KeyNotFoundException($"Không tìm thấy booking có ID {id}.");

            return booking;
        }

        private static void ValidateTime(
            DateTime startTime,
            DateTime endTime,
            bool requireFuture)
        {
            if (startTime >= endTime)
                throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");

            if (requireFuture && endTime <= DateTime.UtcNow)
                throw new ArgumentException("Thời gian kết thúc phải ở tương lai.");
        }

        private static BookingResponse MapResponse(Booking booking)
        {
            return new BookingResponse
            {
                BookingId = booking.BookingId,
                UserId = booking.UserId,
                PurposeType = booking.PurposeType.ToString(),
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status.ToString(),
                CreatedAt = booking.CreatedAt
            };
        }

        private static BookingDetailResponse MapDetailResponse(Booking booking)
        {
            return new BookingDetailResponse
            {
                BookingId = booking.BookingId,
                UserId = booking.UserId,
                UserName = booking.User?.FullName,
                PriorityRuleId = booking.PriorityRuleId,
                PriorityLevel = booking.PriorityRule?.PriorityLevel,
                ApprovedById = booking.ApprovedById,
                ApprovedByName = booking.ApprovedBy?.FullName,
                PurposeType = booking.PurposeType.ToString(),
                PurposeDescription = booking.PurposeDescription,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status.ToString(),
                RejectionReason = booking.RejectionReason,
                ApprovedAt = booking.ApprovedAt,
                CreatedAt = booking.CreatedAt,
                Items = booking.BookingItems.Select(item => new BookingItemResponse
                {
                    BookingItemId = item.BookingItemId,
                    ResourceType = item.ResourceType.ToString(),
                    LabId = item.LabId,
                    LabName = item.LabRoom?.LabName,
                    EquipmentId = item.EquipmentId,
                    EquipmentName = item.Equipment?.EquipmentName,
                    Note = item.Note
                }).ToList()
            };
        }
    }

}
