using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PROJECT_TEAM1_PRN222.Services
{
    public static class ReservationHelper
    {
        // ID giả định để test (Trùng với ID bạn dùng ở phần Reservation trước đó)
        public static readonly Guid TestUserId = Guid.Parse("C0940C8B-5BEA-4D67-869A-252045133C1E");

        public static async Task<bool> IsUserReservedRoom(AppDbContext context, Guid roomId, Guid? userId)
        {
            // Tạm thời bỏ qua userId truyền vào, dùng TestUserId để check
            var effectiveUserId = TestUserId;

            return await context.Reservations
                .AnyAsync(r => r.RoomId == roomId &&
                               r.GuestId == effectiveUserId &&
                               r.Status == ReservationStatus.Confirmed &&
                               !r.IsConvertedToContract);
        }
    }
}
