using BusinessLayer.DTOs.PaymentAlso;
using BusinessLayer.Models;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Functions
{
    public interface IPaymentAlsoManager
    {
        Task<ResponceApi<List<GetPaymentAlsoDTO>>> GetAllPaymentAlsoAsync();
        Task<ResponceApi<GetPaymentAlsoDTO>> GetPaymentAlsoByIdAsync(Guid paymentAlsoId);
        Task<ResponceApi<bool>> CreatePaymentAlsoAsync(CreatePaymentAlsoDTO dto);
        Task<ResponceApi<bool>> UpdatePaymentAlsoAsync(Guid paymentAlsoId, UpdatePaymentAlsoDTO dto);
        Task<ResponceApi<bool>> DeletePaymentAlsoAsync(Guid paymentAlsoId, Guid userId);
    }

    public class PaymentAlsoManager : IPaymentAlsoManager
    {
        private readonly DBContext _db;

        public PaymentAlsoManager(DBContext db)
        {
            _db = db;
        }

        public async Task<ResponceApi<List<GetPaymentAlsoDTO>>> GetAllPaymentAlsoAsync()
        {
            try
            {
                var data = await _db.PaymentAlso
                    .AsNoTracking()
                    .Include(p => p.user)
                    .Select(p => new GetPaymentAlsoDTO
                    {
                        PaymentAlsoId = p.PaymentAlsoId,
                        PaymentAlsoNote = p.PaymentAlsoNote,
                        PaymentAlsoPrice = p.PaymentAlsoPrice,
                        CreateAt = p.CreateAt,
                        UserId = p.UserId,
                        UserName = p.user != null ? p.user.UserName : string.Empty,
                        DriverId = p.user != null ? p.user.DriverId : null,
                        UserRole = p.user != null && p.user.DriverId.HasValue ? "Driver" : "Admin",
                        CreatedByName = p.user != null && p.user.DriverId.HasValue
                            ? (_db.Drivers
                                .Where(d => d.DriverId == p.user.DriverId.Value)
                                .Select(d => d.Name)
                                .FirstOrDefault() ?? p.user.UserName)
                            : (p.user != null ? p.user.UserName : string.Empty)
                    })
                    .OrderByDescending(p => p.CreateAt)
                    .ToListAsync();

                return ResponceApi<List<GetPaymentAlsoDTO>>.Ok(data, "General expenses retrieved successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<List<GetPaymentAlsoDTO>>.Fail($"Error retrieving general expenses: {ex.Message}");
            }
        }

        public async Task<ResponceApi<GetPaymentAlsoDTO>> GetPaymentAlsoByIdAsync(Guid paymentAlsoId)
        {
            try
            {
                var item = await _db.PaymentAlso
                    .AsNoTracking()
                    .Include(p => p.user)
                    .FirstOrDefaultAsync(p => p.PaymentAlsoId == paymentAlsoId);

                if (item == null)
                    return ResponceApi<GetPaymentAlsoDTO>.Fail("General expense not found");

                return ResponceApi<GetPaymentAlsoDTO>.Ok(await MapAsync(item), "General expense retrieved successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<GetPaymentAlsoDTO>.Fail($"Error retrieving general expense: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> CreatePaymentAlsoAsync(CreatePaymentAlsoDTO dto)
        {
            try
            {
                if (dto == null)
                    return ResponceApi<bool>.Fail("Invalid general expense data");

                if (dto.UserId == Guid.Empty)
                    return ResponceApi<bool>.Fail("UserId is required");

                if (string.IsNullOrWhiteSpace(dto.PaymentAlsoNote))
                    return ResponceApi<bool>.Fail("PaymentAlsoNote is required");

                if (dto.PaymentAlsoPrice <= 0)
                    return ResponceApi<bool>.Fail("PaymentAlsoPrice must be greater than zero");

                var userExists = await _db.Users.AnyAsync(u => u.UserId == dto.UserId);
                if (!userExists)
                    return ResponceApi<bool>.Fail("User not found");

                var item = new PaymentAlso
                {
                    PaymentAlsoId = Guid.NewGuid(),
                    PaymentAlsoNote = dto.PaymentAlsoNote.Trim(),
                    PaymentAlsoPrice = dto.PaymentAlsoPrice,
                    CreateAt = dto.CreateAt ?? DateTime.UtcNow,
                    UserId = dto.UserId
                };

                await _db.PaymentAlso.AddAsync(item);
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "General expense created successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error creating general expense: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> UpdatePaymentAlsoAsync(Guid paymentAlsoId, UpdatePaymentAlsoDTO dto)
        {
            try
            {
                if (dto == null)
                    return ResponceApi<bool>.Fail("Invalid general expense data");

                if (dto.UserId == Guid.Empty)
                    return ResponceApi<bool>.Fail("UserId is required");

                if (string.IsNullOrWhiteSpace(dto.PaymentAlsoNote))
                    return ResponceApi<bool>.Fail("PaymentAlsoNote is required");

                if (dto.PaymentAlsoPrice <= 0)
                    return ResponceApi<bool>.Fail("PaymentAlsoPrice must be greater than zero");

                var currentUser = await _db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == dto.UserId);

                if (currentUser == null)
                    return ResponceApi<bool>.Fail("User not found");

                var item = await _db.PaymentAlso
                    .FirstOrDefaultAsync(p => p.PaymentAlsoId == paymentAlsoId);

                if (item == null)
                    return ResponceApi<bool>.Fail("General expense not found");

                var isAdmin = await IsUserAdminAsync(dto.UserId);

                if (item.UserId != dto.UserId && !isAdmin)
                    return ResponceApi<bool>.Fail("You are not authorized to update this general expense");

                item.PaymentAlsoNote = dto.PaymentAlsoNote.Trim();
                item.PaymentAlsoPrice = dto.PaymentAlsoPrice;

                _db.PaymentAlso.Update(item);
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "General expense updated successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error updating general expense: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> DeletePaymentAlsoAsync(Guid paymentAlsoId, Guid userId)
        {
            try
            {
                if (userId == Guid.Empty)
                    return ResponceApi<bool>.Fail("UserId is required");

                var currentUser = await _db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (currentUser == null)
                    return ResponceApi<bool>.Fail("User not found");

                var item = await _db.PaymentAlso
                    .FirstOrDefaultAsync(p => p.PaymentAlsoId == paymentAlsoId);

                if (item == null)
                    return ResponceApi<bool>.Fail("General expense not found");

                var isAdmin = await IsUserAdminAsync(userId);

                if (item.UserId != userId && !isAdmin)
                    return ResponceApi<bool>.Fail("You are not authorized to delete this general expense");

                _db.PaymentAlso.Remove(item);
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "General expense deleted successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error deleting general expense: {ex.Message}");
            }
        }

        private async Task<bool> IsUserAdminAsync(Guid userId)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return false;

            return !user.DriverId.HasValue;
        }

        private async Task<GetPaymentAlsoDTO> MapAsync(PaymentAlso item)
        {
            var user = item.user ?? await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == item.UserId);
            var isDriver = user?.DriverId.HasValue == true;
            var createdByName = user?.UserName ?? string.Empty;

            if (isDriver)
            {
                createdByName = await _db.Drivers
                    .Where(d => d.DriverId == user!.DriverId!.Value)
                    .Select(d => d.Name)
                    .FirstOrDefaultAsync() ?? user!.UserName;
            }

            return new GetPaymentAlsoDTO
            {
                PaymentAlsoId = item.PaymentAlsoId,
                PaymentAlsoNote = item.PaymentAlsoNote,
                PaymentAlsoPrice = item.PaymentAlsoPrice,
                CreateAt = item.CreateAt,
                UserId = item.UserId,
                UserName = user?.UserName ?? string.Empty,
                CreatedByName = createdByName,
                UserRole = isDriver ? "Driver" : "Admin",
                DriverId = user?.DriverId
            };
        }
    }
}
