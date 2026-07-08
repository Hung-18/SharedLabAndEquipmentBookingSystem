
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class BookingItemRepository
       : BaseRepository<BookingItem>, IBookingItemRepository
    {
        public BookingItemRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<IReadOnlyList<BookingItem>> GetByBookingIdAsync(
            int bookingId,
            CancellationToken cancellationToken = default)
        {
            return await Context.BookingItems
                .Include(x => x.LabRoom)
                .Include(x => x.Equipment)
                .Where(x => x.BookingId == bookingId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<BookingItem>> GetByResourceAsync(
            ResourceType resourceType,
            int resourceId,
            CancellationToken cancellationToken = default)
        {
            var query = Context.BookingItems
                .Include(x => x.Booking)
                .Include(x => x.LabRoom)
                .Include(x => x.Equipment)
                .AsQueryable();

            if (resourceType == ResourceType.LabRoom || resourceType.ToString() == "Lab")
            {
                query = query.Where(x => x.LabId == resourceId);
            }
            else if (resourceType == ResourceType.Equipment)
            {
                query = query.Where(x => x.EquipmentId == resourceId);
            }
            else
            {
                return new List<BookingItem>();
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<bool> HasResourceInBookingAsync(
            int bookingId,
            ResourceType resourceType,
            int resourceId,
            CancellationToken cancellationToken = default)
        {
            var query = Context.BookingItems
                .Where(x => x.BookingId == bookingId);

            if (resourceType == ResourceType.LabRoom || resourceType.ToString() == "Lab")
            {
                return await query.AnyAsync(
                    x => x.LabId == resourceId,
                    cancellationToken);
            }

            if (resourceType == ResourceType.Equipment)
            {
                return await query.AnyAsync(
                    x => x.EquipmentId == resourceId,
                    cancellationToken);
            }

            return false;
        }
    }
}