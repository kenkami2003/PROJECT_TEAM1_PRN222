
using BoardingHouseManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BoardingHouseManagement.Services.Admin
{
    public class AdminInvoiceService : IAdminInvoiceService
    {
        private readonly AppDbContext _context;

        public AdminInvoiceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Invoice>> GetAllAsync(int? month = null, int? year = null, InvoiceStatus? status = null)
        {
            var query = _context.Invoices
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Tenan)
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Room)
                .AsQueryable();

            if (month.HasValue) query = query.Where(i => i.Month == month.Value);
            if (year.HasValue) query = query.Where(i => i.Year == year.Value);
            if (status.HasValue) query = query.Where(i => i.Status == status.Value);

            return await query.OrderByDescending(i => i.Year).ThenByDescending(i => i.Month).ToListAsync();
        }

        public async Task<Invoice?> GetByIdAsync(Guid id)
        {
            return await _context.Invoices
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Tenan)
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Room)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<bool> CreateAsync(Invoice invoice)
        {
            try
            {
                invoice.Id = Guid.NewGuid();
                // Tự động tính tổng tiền nếu các thành phần đã có
                invoice.TotalAmount = invoice.RoomAmount + invoice.UtilityAmount + invoice.ServiceAmount + invoice.PenaltyAmount;
                
                _context.Invoices.Add(invoice);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateAsync(Invoice invoice)
        {
            try
            {
                var existing = await _context.Invoices.FindAsync(invoice.Id);
                if (existing == null || existing.Status == InvoiceStatus.Paid) return false;

                // Cập nhật các trường cho phép sửa
                existing.RoomAmount = invoice.RoomAmount;
                existing.UtilityAmount = invoice.UtilityAmount;
                existing.ServiceAmount = invoice.ServiceAmount;
                existing.PenaltyAmount = invoice.PenaltyAmount;
                existing.TotalAmount = invoice.RoomAmount + invoice.UtilityAmount + invoice.ServiceAmount + invoice.PenaltyAmount;
                existing.DueDate = invoice.DueDate;
                existing.Status = invoice.Status;
                existing.Month = invoice.Month;
                existing.Year = invoice.Year;

                _context.Invoices.Update(existing);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<IEnumerable<Contract>> GetActiveContractsAsync()
        {
            return await _context.Contracts
                .Include(c => c.Tenan)
                .Include(c => c.Room)
                .Where(c => c.IsActive)
                .ToListAsync();
        }

        public async Task<BoardingHouseManagement.ViewModels.Admin.InvoiceSuggestionVM> GetInvoiceSuggestionAsync(Guid contractId, int month, int year)
        {
            var res = new BoardingHouseManagement.ViewModels.Admin.InvoiceSuggestionVM();
            
            // 1. Check duplicate hóa đơn
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.ContractId == contractId && i.Month == month && i.Year == year);
            if (existingInvoice != null)
            {
                res.IsDuplicateInvoice = true;
                res.WarningMessage = "CẢNH BÁO: Hóa đơn cho hợp đồng này trong tháng/năm đã tồn tại.";
            }

            // 2. Lấy contract cùng room, property
            var contract = await _context.Contracts
                .Include(c => c.Room)
                    .ThenInclude(r => r.Building)
                        .ThenInclude(b => b.Property)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null || contract.Room == null || contract.Room.Building?.Property == null)
            {
                res.WarningMessage = string.IsNullOrEmpty(res.WarningMessage) 
                    ? "Lỗi: Không tìm thấy hợp đồng hoặc thông tin cơ sở." 
                    : res.WarningMessage + " | Lỗi tìm hợp đồng.";
                return res;
            }

            var room = contract.Room;
            var property = room.Building.Property;

            res.RoomAmount = room.BasePrice;
            res.ServiceAmount = (decimal)property.ServiceFee;

            // 3. Lấy utility reading của đúng tháng/năm
            var currentReading = await _context.UtilityReadings
                .FirstOrDefaultAsync(u => u.RoomId == room.Id && u.Month == month && u.Year == year);

            if (currentReading == null)
            {
                res.HasUtilityReading = false;
                res.UtilityAmount = 0;
                res.ElectricityAmount = 0;
                res.WaterAmount = 0;
                res.WarningMessage = string.IsNullOrEmpty(res.WarningMessage)
                    ? $"Phòng {room.RoomNumber} chưa có dữ liệu chốt điện/nước tháng {month}/{year}."
                    : res.WarningMessage + $" | Chưa có điện/nước tháng {month}/{year}.";
            }
            else
            {
                res.HasUtilityReading = true;

                res.OldElectricity = currentReading.OldElectricity;
                res.NewElectricity = currentReading.NewElectricity;
                res.OldWater = currentReading.OldWater;
                res.NewWater = currentReading.NewWater;

                double electricityUsed = Math.Max(0, currentReading.NewElectricity - currentReading.OldElectricity);
                double waterUsed = Math.Max(0, currentReading.NewWater - currentReading.OldWater);

                res.ElectricityAmount = (decimal)(electricityUsed * property.PriceUnitElectricity);
                res.WaterAmount = (decimal)(waterUsed * property.PriceUnitWater);
                
                res.UtilityAmount = res.ElectricityAmount + res.WaterAmount;
            }

            res.TotalAmount = res.RoomAmount + res.ServiceAmount + res.UtilityAmount;

            return res;
        }
    }
}
