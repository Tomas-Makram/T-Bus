using BusinessLayer.DTOs.Trips;
using BusinessLayer.Models;
using BusinessLayer.Services;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Functions
{
    public interface ITripsManager
    {
        Task<ResponceApi<List<TripViewDTO>>> GetAllTripsAsync();
        Task<ResponceApi<TripViewDTO>> GetTripByIdAsync(Guid tripId);

        Task<ResponceApi<bool>> CreateTripAsync(CreateTripDTO newTrip);
        Task<ResponceApi<bool>> UpdateTripAsync(Guid tripId, UpdateTripDTO updatedTrip);
        Task<ResponceApi<bool>> DeleteTripAsync(Guid tripId);
        Task<ResponceApi<bool>> DeleteTripPermanentlyAsync(Guid tripId);

        Task<ResponceApi<bool>> AddPaymentAsync(Guid tripId, CreatePaymentDTO payment);
        Task<ResponceApi<bool>> UpdatePaymentAsync(Guid paymentId, UpdatePaymentDTO payment);
        Task<ResponceApi<bool>> DeletePaymentAsync(Guid paymentId, Guid userId);

        Task<ResponceApi<bool>> AddTahseelAsync(Guid tripId, CreateTahseelDTO dto);
        Task<ResponceApi<bool>> UpdateTahseelAsync(Guid tahseelId, UpdateTahseelDTO dto);
        Task<ResponceApi<bool>> DeleteTahseelAsync(Guid tahseelId, Guid userId);
        Task<ResponceApi<bool>> UpdateSolarAsync(Guid tripId, UpdateTripSolarDTO dto);

        Task<ResponceApi<bool>> MarkTripsAsDoneAsync(List<Guid> tripIds);
    }

    public class TripsManager : ITripsManager
    {
        private readonly DBContext _db;
        private readonly IDataCiphers _cipher;
        private readonly CairoTimeService _timeService;

        private const string PaymentTypeCache = "Cache";
        private const string PaymentTypeTahseel = "Tahseel";
        private const string PaymentTypeNoCustody = "NoCustody";
        private const string PaymentTypeMixed = "Mixed";

        private const string DriverStatusSettled = "Settled";
        private const string DriverStatusDriverOwesCompany = "DriverOwesCompany";
        private const string DriverStatusCompanyOwesDriver = "CompanyOwesDriver";

        private const int PermanentDeleteWindowHours = 72;

        public TripsManager(DBContext db, IDataCiphers cipher, CairoTimeService timeService)
        {
            _db = db;
            _cipher = cipher;
            _timeService = timeService;
        }

        private static ResponceApi<bool>? GuardTripIsEditable(Trips? trip, string actionName)
        {
            if (trip?.IsDone == true)
                return ResponceApi<bool>.Fail($"This trip has already been settled. You cannot {actionName}. Only solar can be updated.");

            return null;
        }

        public async Task<ResponceApi<List<TripViewDTO>>> GetAllTripsAsync()
        {
            try
            {
                var trips = await _db.Trips
                    .AsNoTracking()
                    .Include(t => t.Bus)
                    .Include(t => t.Driver)
                    .Include(t => t.User)
                    .Include(t => t.Payments)
                        .ThenInclude(p => p.User)
                    .Include(t => t.TahseelItems)
                        .ThenInclude(x => x.User)
                    .OrderByDescending(t => t.TripDate)
                    .ToListAsync();

                var result = trips.Select(MapTripToDTO).ToList();
                return ResponceApi<List<TripViewDTO>>.Ok(result, "Trips retrieved successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<List<TripViewDTO>>.Fail($"Error retrieving trips: {ex.Message}");
            }
        }

        public async Task<ResponceApi<TripViewDTO>> GetTripByIdAsync(Guid tripId)
        {
            try
            {
                var trip = await LoadTripAsync(tripId, asNoTracking: true);

                if (trip == null)
                    return ResponceApi<TripViewDTO>.Fail("Trip not found");

                return ResponceApi<TripViewDTO>.Ok(MapTripToDTO(trip), "Trip retrieved successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<TripViewDTO>.Fail($"Error retrieving trip: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> CreateTripAsync(CreateTripDTO newTrip)
        {
            try
            {
                var validation = await ValidateCreateTripAsync(newTrip);
                if (!validation.Success)
                    return validation;

                var trip = new Trips
                {
                    TripId = Guid.NewGuid(),
                    BusId = newTrip.BusId,
                    DriverId = newTrip.DriverId,
                    AdminId = newTrip.UserId,
                    TripDate = newTrip.TripDate,
                    FromLocation = newTrip.FromLocation.Trim(),
                    ToLocation = newTrip.ToLocation.Trim(),
                    Cache = NormalizeNullableMoney(newTrip.Cache),
                    TripPrice = newTrip.TripPrice,
                    DriverPrice = newTrip.DriverPrice,
                    Solar = newTrip.Solar,
                    Notes = newTrip.Notes?.Trim(),
                    CompanyName = ResolveCompanyName(newTrip.inSide, newTrip.CompanyName),
                    inSide = newTrip.inSide
                };

                if (newTrip.TahseelItems != null && newTrip.TahseelItems.Any())
                {
                    foreach (var th in newTrip.TahseelItems.Where(x => x.Amount > 0))
                    {
                        trip.TahseelItems.Add(new TripTahseel
                        {
                            TahseelId = Guid.NewGuid(),
                            TripId = trip.TripId,
                            Amount = th.Amount,
                            Notes = th.Notes?.Trim() ?? string.Empty,
                            UserId = th.UserId == Guid.Empty ? newTrip.UserId : th.UserId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (newTrip.Payments != null && newTrip.Payments.Any())
                {
                    foreach (var p in newTrip.Payments.Where(p => p.Amount > 0))
                    {
                        var userId = p.UserId == Guid.Empty ? newTrip.UserId : p.UserId;
                        var coverage = ApplyPaymentCoverage(trip, p.Amount, p.PaymentType, null);

                        trip.Payments.Add(new Payments
                        {
                            PaymentId = Guid.NewGuid(),
                            Amount = p.Amount,
                            Notes = p.Notes?.Trim() ?? string.Empty,
                            TripId = trip.TripId,
                            UserId = userId,
                            PaymentType = coverage.PaymentType,
                            isOCtine = false
                        });
                    }
                }

                await _db.Trips.AddAsync(trip);
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "Trip created successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error creating trip: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> UpdateTripAsync(Guid tripId, UpdateTripDTO updatedTrip)
        {
            try
            {
                var validation = await ValidateUpdateTripAsync(updatedTrip);
                if (!validation.Success)
                    return validation;

                var trip = await _db.Trips
                    .Include(t => t.Payments)
                    .Include(t => t.TahseelItems)
                    .FirstOrDefaultAsync(t => t.TripId == tripId);

                if (trip == null)
                    return ResponceApi<bool>.Fail("Trip not found");

                var editGuard = GuardTripIsEditable(trip, "update this trip");
                if (editGuard != null)
                    return editGuard;

                trip.BusId = updatedTrip.BusId;
                trip.DriverId = updatedTrip.DriverId;
                trip.TripDate = updatedTrip.TripDate;
                trip.FromLocation = updatedTrip.FromLocation.Trim();
                trip.ToLocation = updatedTrip.ToLocation.Trim();
                trip.Cache = NormalizeNullableMoney(updatedTrip.Cache);
                trip.TripPrice = updatedTrip.TripPrice;
                trip.DriverPrice = updatedTrip.DriverPrice;
                trip.Solar = updatedTrip.Solar;
                trip.Notes = updatedTrip.Notes?.Trim();
                trip.CompanyName = ResolveCompanyName(updatedTrip.inSide, updatedTrip.CompanyName);
                trip.inSide = updatedTrip.inSide;

                if (trip.Payments.Any())
                    _db.Payments.RemoveRange(trip.Payments);

                if (trip.TahseelItems.Any())
                    _db.TripTahseel.RemoveRange(trip.TahseelItems);

                trip.Payments.Clear();
                trip.TahseelItems.Clear();

                if (updatedTrip.TahseelItems != null && updatedTrip.TahseelItems.Any())
                {
                    foreach (var th in updatedTrip.TahseelItems.Where(x => x.Amount > 0))
                    {
                        trip.TahseelItems.Add(new TripTahseel
                        {
                            TahseelId = Guid.NewGuid(),
                            TripId = trip.TripId,
                            Amount = th.Amount,
                            Notes = th.Notes?.Trim() ?? string.Empty,
                            UserId = th.UserId == Guid.Empty ? updatedTrip.UserId : th.UserId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (updatedTrip.Payments != null && updatedTrip.Payments.Any())
                {
                    foreach (var p in updatedTrip.Payments.Where(p => p.Amount > 0))
                    {
                        var userId = p.UserId == Guid.Empty ? updatedTrip.UserId : p.UserId;
                        var coverage = ApplyPaymentCoverage(trip, p.Amount, p.PaymentType, null);

                        trip.Payments.Add(new Payments
                        {
                            PaymentId = Guid.NewGuid(),
                            Amount = p.Amount,
                            Notes = p.Notes?.Trim() ?? string.Empty,
                            TripId = trip.TripId,
                            UserId = userId,
                            PaymentType = coverage.PaymentType,
                            isOCtine = false
                        });
                    }
                }

                _db.Trips.Update(trip);
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "Trip updated successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error updating trip: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> DeleteTripAsync(Guid tripId)
        {
            try
            {
                var trip = await _db.Trips
                    .Include(t => t.Payments)
                    .Include(t => t.TahseelItems)
                    .FirstOrDefaultAsync(t => t.TripId == tripId);

                if (trip == null)
                    return ResponceApi<bool>.Fail("Trip not found");

                var deleteGuard = GuardTripIsEditable(trip, "delete this trip");
                if (deleteGuard != null)
                    return deleteGuard;

                if (trip.Payments.Any())
                    _db.Payments.RemoveRange(trip.Payments);

                if (trip.TahseelItems.Any())
                    _db.TripTahseel.RemoveRange(trip.TahseelItems);

                _db.Trips.Remove(trip);
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "Trip deleted successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error deleting trip: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> DeleteTripPermanentlyAsync(Guid tripId)
        {
            try
            {
                if (tripId == Guid.Empty)
                    return ResponceApi<bool>.Fail("Invalid trip id");

                var trip = await _db.Trips
                    .Include(t => t.Payments)
                    .Include(t => t.TahseelItems)
                    .FirstOrDefaultAsync(t => t.TripId == tripId);

                if (trip == null)
                    return ResponceApi<bool>.Fail("Trip not found");

                var deleteState = GetPermanentDeleteState(trip);
                if (!deleteState.CanDelete)
                    return ResponceApi<bool>.Fail(deleteState.Message);

                await using var transaction = await _db.Database.BeginTransactionAsync();

                if (trip.Payments.Any())
                    _db.Payments.RemoveRange(trip.Payments);

                if (trip.TahseelItems.Any())
                    _db.TripTahseel.RemoveRange(trip.TahseelItems);

                _db.Trips.Remove(trip);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<bool>.Ok(true, "Trip permanently deleted successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error permanently deleting trip: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> AddPaymentAsync(Guid tripId, CreatePaymentDTO payment)
        {
            try
            {
                if (payment == null || payment.Amount <= 0)
                    return ResponceApi<bool>.Fail("Invalid payment data");

                var userResult = await GetPaymentUserAsync(payment.UserId);
                if (!userResult.Success || userResult.User == null)
                    return ResponceApi<bool>.Fail(userResult.Message);

                var trip = await LoadTripAsync(tripId, asNoTracking: false);
                if (trip == null)
                    return ResponceApi<bool>.Fail("Trip not found");

                var paymentGuard = GuardTripIsEditable(trip, "add payment to this trip");
                if (paymentGuard != null)
                    return paymentGuard;

                var permission = ValidateTripPermission(userResult, trip, "add payment to this trip");
                if (!permission.Success)
                    return ResponceApi<bool>.Fail(permission.Message);

                var coverage = ApplyPaymentCoverage(trip, payment.Amount, payment.PaymentType, null);

                var newPayment = new Payments
                {
                    PaymentId = Guid.NewGuid(),
                    TripId = tripId,
                    UserId = payment.UserId,
                    Amount = payment.Amount,
                    Notes = payment.Notes?.Trim() ?? string.Empty,
                    PaymentType = coverage.PaymentType,
                    isOCtine = false
                };

                await _db.Payments.AddAsync(newPayment);
                _db.Trips.Update(trip);
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, BuildPaymentResultMessage(coverage));
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error adding payment: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> UpdatePaymentAsync(Guid paymentId, UpdatePaymentDTO payment)
        {
            try
            {
                if (payment == null || payment.Amount <= 0)
                    return ResponceApi<bool>.Fail("Invalid payment data");

                var userResult = await GetPaymentUserAsync(payment.UserId);
                if (!userResult.Success || userResult.User == null)
                    return ResponceApi<bool>.Fail(userResult.Message);

                var oldPayment = await _db.Payments
                    .Include(p => p.Trip)
                        .ThenInclude(t => t.Payments)
                    .Include(p => p.Trip)
                        .ThenInclude(t => t.TahseelItems)
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                if (oldPayment == null)
                    return ResponceApi<bool>.Fail("Payment not found");

                var paymentEditGuard = GuardTripIsEditable(oldPayment.Trip, "update this payment");
                if (paymentEditGuard != null)
                    return paymentEditGuard;

                var permission = ValidateTripPermission(userResult, oldPayment.Trip, "update this payment");
                if (!permission.Success)
                    return ResponceApi<bool>.Fail(permission.Message);

                RestorePaymentCoverage(oldPayment.Trip, oldPayment.PaymentType, oldPayment.Amount);
                var coverage = ApplyPaymentCoverage(oldPayment.Trip, payment.Amount, payment.PaymentType, oldPayment.PaymentId);

                oldPayment.Amount = payment.Amount;
                oldPayment.Notes = payment.Notes?.Trim() ?? string.Empty;
                oldPayment.UserId = payment.UserId;
                oldPayment.PaymentType = coverage.PaymentType;
                oldPayment.isOCtine = false;

                _db.Payments.Update(oldPayment);
                _db.Trips.Update(oldPayment.Trip);
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, BuildPaymentResultMessage(coverage, "Payment updated successfully"));
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error updating payment: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> DeletePaymentAsync(Guid paymentId, Guid userId)
        {
            try
            {
                var userResult = await GetPaymentUserAsync(userId);
                if (!userResult.Success || userResult.User == null)
                    return ResponceApi<bool>.Fail(userResult.Message);

                var payment = await _db.Payments
                    .Include(p => p.Trip)
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                if (payment == null)
                    return ResponceApi<bool>.Fail("Payment not found");

                var paymentDeleteGuard = GuardTripIsEditable(payment.Trip, "delete this payment");
                if (paymentDeleteGuard != null)
                    return paymentDeleteGuard;

                var permission = ValidateTripPermission(userResult, payment.Trip, "delete this payment");
                if (!permission.Success)
                    return ResponceApi<bool>.Fail(permission.Message);

                RestorePaymentCoverage(payment.Trip, payment.PaymentType, payment.Amount);

                _db.Payments.Remove(payment);
                _db.Trips.Update(payment.Trip);
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "Payment deleted successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error deleting payment: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> AddTahseelAsync(Guid tripId, CreateTahseelDTO dto)
        {
            try
            {
                if (dto == null || dto.Amount <= 0)
                    return ResponceApi<bool>.Fail("Invalid tahseel data");

                var userResult = await GetPaymentUserAsync(dto.UserId);
                if (!userResult.Success || userResult.User == null)
                    return ResponceApi<bool>.Fail(userResult.Message);

                var trip = await _db.Trips.FirstOrDefaultAsync(t => t.TripId == tripId);
                if (trip == null)
                    return ResponceApi<bool>.Fail("Trip not found");

                var tahseelAddGuard = GuardTripIsEditable(trip, "add tahseel to this trip");
                if (tahseelAddGuard != null)
                    return tahseelAddGuard;

                var permission = ValidateTripPermission(userResult, trip, "add tahseel to this trip");
                if (!permission.Success)
                    return ResponceApi<bool>.Fail(permission.Message);

                await _db.TripTahseel.AddAsync(new TripTahseel
                {
                    TahseelId = Guid.NewGuid(),
                    TripId = tripId,
                    UserId = dto.UserId,
                    Amount = dto.Amount,
                    Notes = dto.Notes?.Trim() ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
                return ResponceApi<bool>.Ok(true, "Tahseel added successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error adding tahseel: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> UpdateTahseelAsync(Guid tahseelId, UpdateTahseelDTO dto)
        {
            try
            {
                if (dto == null || dto.Amount <= 0)
                    return ResponceApi<bool>.Fail("Invalid tahseel data");

                var userResult = await GetPaymentUserAsync(dto.UserId);
                if (!userResult.Success || userResult.User == null)
                    return ResponceApi<bool>.Fail(userResult.Message);

                var tahseel = await _db.TripTahseel
                    .Include(t => t.Trip)
                    .FirstOrDefaultAsync(t => t.TahseelId == tahseelId);

                if (tahseel == null)
                    return ResponceApi<bool>.Fail("Tahseel not found");

                var tahseelEditGuard = GuardTripIsEditable(tahseel.Trip, "update this tahseel");
                if (tahseelEditGuard != null)
                    return tahseelEditGuard;

                var permission = ValidateTripPermission(userResult, tahseel.Trip, "update this tahseel");
                if (!permission.Success)
                    return ResponceApi<bool>.Fail(permission.Message);

                tahseel.Amount = dto.Amount;
                tahseel.Notes = dto.Notes?.Trim() ?? string.Empty;
                tahseel.UserId = dto.UserId;

                _db.TripTahseel.Update(tahseel);
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "Tahseel updated successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error updating tahseel: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> DeleteTahseelAsync(Guid tahseelId, Guid userId)
        {
            try
            {
                var userResult = await GetPaymentUserAsync(userId);
                if (!userResult.Success || userResult.User == null)
                    return ResponceApi<bool>.Fail(userResult.Message);

                var tahseel = await _db.TripTahseel
                    .Include(t => t.Trip)
                    .FirstOrDefaultAsync(t => t.TahseelId == tahseelId);

                if (tahseel == null)
                    return ResponceApi<bool>.Fail("Tahseel not found");

                var tahseelDeleteGuard = GuardTripIsEditable(tahseel.Trip, "delete this tahseel");
                if (tahseelDeleteGuard != null)
                    return tahseelDeleteGuard;

                var permission = ValidateTripPermission(userResult, tahseel.Trip, "delete this tahseel");
                if (!permission.Success)
                    return ResponceApi<bool>.Fail(permission.Message);

                _db.TripTahseel.Remove(tahseel);
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "Tahseel deleted successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error deleting tahseel: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> UpdateSolarAsync(Guid tripId, UpdateTripSolarDTO dto)
        {
            try
            {
                if (dto == null)
                    return ResponceApi<bool>.Fail("Invalid solar data");

                if (dto.Solar < 0)
                    return ResponceApi<bool>.Fail("Solar cannot be negative");

                var trip = await _db.Trips.FirstOrDefaultAsync(t => t.TripId == tripId);
                if (trip == null)
                    return ResponceApi<bool>.Fail("Trip not found");

                trip.Solar = dto.Solar;
                _db.Trips.Update(trip);
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "Solar updated successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error updating solar: {ex.Message}");
            }
        }

        public async Task<ResponceApi<bool>> MarkTripsAsDoneAsync(List<Guid> tripIds)
        {
            try
            {
                if (tripIds == null || !tripIds.Any())
                    return ResponceApi<bool>.Fail("No trips selected");

                var distinctIds = tripIds
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .ToList();

                if (!distinctIds.Any())
                    return ResponceApi<bool>.Fail("No valid trips selected");

                var trips = await _db.Trips
                    .Where(t => distinctIds.Contains(t.TripId))
                    .ToListAsync();

                if (!trips.Any())
                    return ResponceApi<bool>.Fail("Trips not found");

                foreach (var trip in trips)
                    trip.IsDone = true;

                _db.Trips.UpdateRange(trips);
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "Trips marked as done successfully");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail($"Error marking trips as done: {ex.Message}");
            }
        }

        private async Task<Trips?> LoadTripAsync(Guid tripId, bool asNoTracking)
        {
            var query = _db.Trips
                .Include(t => t.Bus)
                .Include(t => t.Driver)
                .Include(t => t.User)
                .Include(t => t.Payments)
                    .ThenInclude(p => p.User)
                .Include(t => t.TahseelItems)
                    .ThenInclude(x => x.User)
                .AsQueryable();

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(t => t.TripId == tripId);
        }

        private TripViewDTO MapTripToDTO(Trips trip)
        {
            var summary = BuildTripMoneySummary(trip);
            var deleteState = GetPermanentDeleteState(trip);

            return new TripViewDTO
            {
                TripId = trip.TripId,
                BusId = trip.BusId,
                BusName = trip.Bus?.Name ?? string.Empty,
                BusPalateNumber = trip.Bus?.PalateNumber ?? string.Empty,
                DriverId = trip.DriverId,
                DriverName = trip.Driver?.Name ?? string.Empty,
                DriverNationalId = trip.Driver?.NationalId ?? string.Empty,
                TripDate = _timeService.UtcToCairo(trip.TripDate),
                FromLocation = trip.FromLocation,
                ToLocation = trip.ToLocation,
                Cache = trip.Cache,
                TripPrice = trip.TripPrice,
                DriverPrice = trip.DriverPrice,
                Solar = trip.Solar,
                AdminId = trip.AdminId,
                AdminName = trip.User != null ? trip.User.UserName : string.Empty,
                TotalPayments = summary.TotalPayments,
                TotalTahseel = summary.TotalTahseel,
                TahseelRemaining = summary.TahseelRemaining,
                TotalAvailableMoney = summary.TotalAvailableMoney,
                DriverPaidFromPocket = summary.DriverPaidFromPocket,
                DriverAccountAmount = summary.DriverAccountAmount,
                AdminBalanceAmount = summary.AdminBalanceAmount,
                DriverAccountStatus = summary.DriverAccountStatus!,
                isDone = trip.IsDone,
                inSide = trip.inSide,
                Notes = trip.Notes,
                CompanyName = trip.inSide ? trip.CompanyName : "Go Bus",
                CanBePermanentlyDeleted = deleteState.CanDelete,
                PermanentDeleteReason = deleteState.Message,
                PermanentDeleteWindowHours = PermanentDeleteWindowHours,
                PermanentDeleteAllowedFrom = _timeService.UtcToCairo(deleteState.AllowedFromUtc),
                PermanentDeleteAllowedUntil = _timeService.UtcToCairo(deleteState.AllowedUntilUtc),
                Payments = trip.Payments
                    .OrderByDescending(p => p.PaymentId)
                    .Select(p => new GetPaymentDTO
                    {
                        PaymentId = p.PaymentId,
                        Amount = p.Amount,
                        Notes = p.Notes,
                        TripId = p.TripId,
                        UserId = p.UserId,
                        UserName = DisplayUserName(p.User),
                        PaymentType = p.PaymentType
                    })
                    .ToList(),
                TahseelItems = trip.TahseelItems
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new GetTahseelDTO
                    {
                        TahseelId = x.TahseelId,
                        Amount = x.Amount,
                        Notes = x.Notes,
                        CreatedAt = _timeService.UtcToCairo(x.CreatedAt),
                        TripId = x.TripId,
                        UserId = x.UserId,
                        UserName = DisplayUserName(x.User)
                    })
                    .ToList()
            };
        }

        private async Task<ResponceApi<bool>> ValidateCreateTripAsync(CreateTripDTO trip)
        {
            if (trip == null)
                return ResponceApi<bool>.Fail("Invalid trip data");

            if (trip.UserId == Guid.Empty)
                return ResponceApi<bool>.Fail("UserId is required");

            if (trip.BusId == Guid.Empty || trip.DriverId == Guid.Empty)
                return ResponceApi<bool>.Fail("BusId and DriverId are required");

            if (string.IsNullOrWhiteSpace(trip.FromLocation) || string.IsNullOrWhiteSpace(trip.ToLocation))
                return ResponceApi<bool>.Fail("From Location and ToLocation are required");

            if (!ValidateTripMoney(trip.Cache, trip.TripPrice, trip.DriverPrice, trip.Solar, out var moneyError))
                return ResponceApi<bool>.Fail(moneyError);

            if (trip.inSide && string.IsNullOrWhiteSpace(trip.CompanyName))
                return ResponceApi<bool>.Fail("CompanyName is required for outside trips");

            if (!await _db.Users.AnyAsync(u => u.UserId == trip.UserId))
                return ResponceApi<bool>.Fail("User not found");

            if (!await _db.Buses.AnyAsync(b => b.BusId == trip.BusId))
                return ResponceApi<bool>.Fail("Bus not found");

            if (!await _db.Drivers.AnyAsync(d => d.DriverId == trip.DriverId))
                return ResponceApi<bool>.Fail("Driver not found");

            return ResponceApi<bool>.Ok(true);
        }

        private async Task<ResponceApi<bool>> ValidateUpdateTripAsync(UpdateTripDTO trip)
        {
            if (trip == null)
                return ResponceApi<bool>.Fail("Invalid trip data");

            if (trip.BusId == Guid.Empty || trip.DriverId == Guid.Empty)
                return ResponceApi<bool>.Fail("BusId and DriverId are required");

            if (string.IsNullOrWhiteSpace(trip.FromLocation) || string.IsNullOrWhiteSpace(trip.ToLocation))
                return ResponceApi<bool>.Fail("From Location and ToLocation are required");

            if (!ValidateTripMoney(trip.Cache, trip.TripPrice, trip.DriverPrice, trip.Solar, out var moneyError))
                return ResponceApi<bool>.Fail(moneyError);

            if (trip.inSide && string.IsNullOrWhiteSpace(trip.CompanyName))
                return ResponceApi<bool>.Fail("CompanyName is required for outside trips");

            if (!await _db.Buses.AnyAsync(b => b.BusId == trip.BusId))
                return ResponceApi<bool>.Fail("Bus not found");

            if (!await _db.Drivers.AnyAsync(d => d.DriverId == trip.DriverId))
                return ResponceApi<bool>.Fail("Driver not found");

            return ResponceApi<bool>.Ok(true);
        }

        private static bool ValidateTripMoney(decimal? cache, decimal tripPrice, decimal driverPrice, decimal solar, out string message)
        {
            message = string.Empty;

            if (cache.HasValue && cache.Value < 0)
            {
                message = "Custody cannot be negative";
                return false;
            }

            if (tripPrice < 0)
            {
                message = "TripPrice cannot be negative";
                return false;
            }

            if (driverPrice < 0)
            {
                message = "DriverPrice cannot be negative";
                return false;
            }

            if (solar < 0)
            {
                message = "Solar cannot be negative";
                return false;
            }

            return true;
        }

        private PaymentCoverage ApplyPaymentCoverage(Trips trip, decimal amount, string? requestedPaymentType, Guid? excludePaymentId)
        {
            if (amount <= 0)
                return PaymentCoverage.Empty();

            var requested = NormalizePaymentType(requestedPaymentType);

            if (requested == PaymentTypeNoCustody)
                return PaymentCoverage.FromNoCustody(amount);

            var remaining = amount;
            var cashUsed = 0m;
            var tahseelUsed = 0m;
            var driverPaid = 0m;

            var cacheAvailable = Math.Max(0, trip.Cache ?? 0);
            if (cacheAvailable > 0 && remaining > 0)
            {
                cashUsed = Math.Min(cacheAvailable, remaining);
                trip.Cache = cacheAvailable - cashUsed;
                remaining -= cashUsed;
            }

            var tahseelAvailable = GetTahseelRemaining(trip, excludePaymentId);
            if (tahseelAvailable > 0 && remaining > 0)
            {
                tahseelUsed = Math.Min(tahseelAvailable, remaining);
                remaining -= tahseelUsed;
            }

            if (remaining > 0)
                driverPaid = remaining;

            return new PaymentCoverage(cashUsed, tahseelUsed, driverPaid);
        }

        private void RestorePaymentCoverage(Trips trip, string? paymentType, decimal fallbackAmount)
        {
            var coverage = ParseCoverage(paymentType, fallbackAmount);

            if (coverage.CacheAmount > 0)
                trip.Cache = (trip.Cache ?? 0) + coverage.CacheAmount;
        }

        private TripMoneySummary BuildTripMoneySummary(Trips trip)
        {
            var totalPayments = trip.Payments.Sum(p => p.Amount);
            var totalTahseel = trip.TahseelItems.Sum(x => x.Amount);
            var tahseelUsed = trip.Payments.Sum(p => ParseCoverage(p.PaymentType, p.Amount).TahseelAmount);
            var driverPaidFromPocket = trip.Payments.Sum(p => ParseCoverage(p.PaymentType, p.Amount).NoCustodyAmount);
            var tahseelRemaining = Math.Max(0, totalTahseel - tahseelUsed);
            var cashRemaining = Math.Max(0, trip.Cache ?? 0);
            var totalAvailableMoney = cashRemaining + tahseelRemaining;
            var driverAccountAmount = trip.DriverPrice + driverPaidFromPocket;
            var adminBalanceAmount = totalAvailableMoney - driverAccountAmount;

            var status = DriverStatusSettled;
            if (adminBalanceAmount > 0)
                status = DriverStatusDriverOwesCompany;
            else if (adminBalanceAmount < 0)
                status = DriverStatusCompanyOwesDriver;

            return new TripMoneySummary
            {
                TotalPayments = totalPayments,
                TotalTahseel = totalTahseel,
                TahseelUsed = tahseelUsed,
                TahseelRemaining = tahseelRemaining,
                CashRemaining = cashRemaining,
                TotalAvailableMoney = totalAvailableMoney,
                DriverPaidFromPocket = driverPaidFromPocket,
                DriverAccountAmount = driverAccountAmount,
                AdminBalanceAmount = adminBalanceAmount,
                DriverAccountStatus = status
            };
        }

        private decimal GetTahseelRemaining(Trips trip, Guid? excludePaymentId)
        {
            var totalTahseel = trip.TahseelItems.Sum(x => x.Amount);
            var usedTahseel = trip.Payments
                .Where(p => excludePaymentId == null || p.PaymentId != excludePaymentId.Value)
                .Sum(p => ParseCoverage(p.PaymentType, p.Amount).TahseelAmount);

            return Math.Max(0, totalTahseel - usedTahseel);
        }

        private static PaymentCoverage ParseCoverage(string? paymentType, decimal fallbackAmount)
        {
            var normalized = NormalizePaymentType(paymentType);

            if (normalized == PaymentTypeCache)
                return PaymentCoverage.FromCache(fallbackAmount);

            if (normalized == PaymentTypeTahseel)
                return PaymentCoverage.FromTahseel(fallbackAmount);

            if (normalized == PaymentTypeNoCustody)
                return PaymentCoverage.FromNoCustody(fallbackAmount);

            if (!string.IsNullOrWhiteSpace(paymentType) && paymentType.StartsWith(PaymentTypeMixed, StringComparison.OrdinalIgnoreCase))
            {
                var cash = ReadCoveragePart(paymentType, "Cache");
                var tahseel = ReadCoveragePart(paymentType, "Tahseel");
                var noCustody = ReadCoveragePart(paymentType, "NoCustody");
                return new PaymentCoverage(cash, tahseel, noCustody);
            }

            return PaymentCoverage.FromCache(fallbackAmount);
        }

        private static decimal ReadCoveragePart(string paymentType, string key)
        {
            if (string.IsNullOrWhiteSpace(paymentType))
                return 0m;

            var parts = paymentType.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                var cleaned = part;
                if (cleaned.StartsWith(PaymentTypeMixed, StringComparison.OrdinalIgnoreCase))
                    cleaned = cleaned.Replace(PaymentTypeMixed, string.Empty, StringComparison.OrdinalIgnoreCase).Trim(':', ' ', ';');

                var pair = cleaned.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (pair.Length != 2)
                    continue;

                if (!pair[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (decimal.TryParse(pair[1], out var value))
                    return value;
            }

            return 0m;
        }

        private static string NormalizePaymentType(string? paymentType)
        {
            if (string.IsNullOrWhiteSpace(paymentType))
                return PaymentTypeCache;

            var value = paymentType.Trim();

            if (value.StartsWith(PaymentTypeMixed, StringComparison.OrdinalIgnoreCase))
                return PaymentTypeMixed;

            if (value.Equals(PaymentTypeCache, StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                return PaymentTypeCache;

            if (value.Equals(PaymentTypeTahseel, StringComparison.OrdinalIgnoreCase) ||
                value.Equals("تحصيل", StringComparison.OrdinalIgnoreCase))
                return PaymentTypeTahseel;

            if (value.Equals(PaymentTypeNoCustody, StringComparison.OrdinalIgnoreCase) ||
                value.Equals("No Custody", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("بدون عهدة", StringComparison.OrdinalIgnoreCase))
                return PaymentTypeNoCustody;

            return PaymentTypeCache;
        }

        private static string BuildPaymentResultMessage(PaymentCoverage coverage, string defaultMessage = "Payment added successfully")
        {
            if (coverage.NoCustodyAmount <= 0)
                return defaultMessage;

            return $"{defaultMessage}. Driver paid {coverage.NoCustodyAmount:0.##} from pocket and it was added to driver account.";
        }

        private static decimal? NormalizeNullableMoney(decimal? value)
        {
            if (!value.HasValue)
                return null;

            return value.Value <= 0 ? null : value.Value;
        }

        private static string? ResolveCompanyName(bool isOutsideCompany, string? companyName)
        {
            if (!isOutsideCompany)
                return "Go Bus";

            return string.IsNullOrWhiteSpace(companyName) ? null : companyName.Trim();
        }

        private string DisplayUserName(Users? user)
        {
            if (user == null)
                return string.Empty;

            if (user.DriverId.HasValue)
            {
                return _db.Drivers
                    .AsNoTracking()
                    .Where(d => d.DriverId == user.DriverId.Value)
                    .Select(d => d.Name)
                    .FirstOrDefault() ?? user.UserName;
            }

            return user.UserName;
        }

        private static (bool Success, string Message) ValidateTripPermission((bool Success, string Message, Users? User, bool IsAdmin) userResult, Trips trip, string action)
        {
            if (userResult.IsAdmin)
                return (true, string.Empty);

            if (userResult.User == null)
                return (false, "User not found");

            if (userResult.User.DriverId == null)
                return (false, "This user is not linked to a driver");

            if (trip.DriverId != userResult.User.DriverId.Value)
                return (false, $"You are not allowed to {action}");

            if (!IsSameTripDay(trip.TripDate))
                return (false, "Driver can edit only on trip day");

            return (true, string.Empty);
        }

        private static bool IsSameTripDay(DateTime tripDate)
        {
            var today = DateTime.UtcNow.Date;
            var tripDay = tripDate.Date;

            return today >= tripDay && today <= tripDay.AddDays(1);
        }

        private static PermanentDeleteState GetPermanentDeleteState(Trips? trip)
        {
            if (trip == null)
                return PermanentDeleteState.Blocked("Trip not found", DateTime.UtcNow, DateTime.UtcNow);

            var tripUtc = NormalizeTripDateForCompare(trip.TripDate);
            var allowedFromUtc = tripUtc.AddHours(-PermanentDeleteWindowHours);
            var allowedUntilUtc = tripUtc.AddHours(PermanentDeleteWindowHours);

            if (trip.IsDone)
            {
                return PermanentDeleteState.Blocked(
                    "Cannot permanently delete this trip because it has already been settled/accounted.",
                    allowedFromUtc,
                    allowedUntilUtc);
            }

            var nowUtc = DateTime.UtcNow;

            //if (nowUtc < allowedFromUtc)
            //{
            //    return PermanentDeleteState.Blocked(
            //        $"Cannot permanently delete this trip before {PermanentDeleteWindowHours} hours from the trip departure time.",
            //        allowedFromUtc,
            //        allowedUntilUtc);
            //}

            if (false)//nowUtc > allowedUntilUtc)
            {
                return PermanentDeleteState.Blocked(
                    $"Cannot permanently delete this trip because more than {PermanentDeleteWindowHours} hours have passed since the trip departure time.",
                    allowedFromUtc,
                    allowedUntilUtc);
            }

            return PermanentDeleteState.Allowed(
                "Trip can be permanently deleted.",
                allowedFromUtc,
                allowedUntilUtc);
        }

        private static DateTime NormalizeTripDateForCompare(DateTime tripDate)
        {
            return tripDate.Kind == DateTimeKind.Local
                ? tripDate.ToUniversalTime()
                : tripDate;
        }

        private async Task<(bool Success, string Message, Users? User, bool IsAdmin)> GetPaymentUserAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                return (false, "UserId is required", null, false);

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return (false, "User not found", null, false);

            var role = string.IsNullOrWhiteSpace(user.RoleEncrypted)
                ? string.Empty
                : _cipher.Decrypt(user.RoleEncrypted);

            var isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            return (true, string.Empty, user, isAdmin);
        }

        private readonly struct PaymentCoverage
        {
            public PaymentCoverage(decimal cacheAmount, decimal tahseelAmount, decimal noCustodyAmount)
            {
                CacheAmount = cacheAmount < 0 ? 0 : cacheAmount;
                TahseelAmount = tahseelAmount < 0 ? 0 : tahseelAmount;
                NoCustodyAmount = noCustodyAmount < 0 ? 0 : noCustodyAmount;
            }

            public decimal CacheAmount { get; }
            public decimal TahseelAmount { get; }
            public decimal NoCustodyAmount { get; }

            public string PaymentType
            {
                get
                {
                    var usedCount = 0;
                    if (CacheAmount > 0) usedCount++;
                    if (TahseelAmount > 0) usedCount++;
                    if (NoCustodyAmount > 0) usedCount++;

                    if (usedCount <= 0)
                        return PaymentTypeNoCustody;

                    if (usedCount == 1)
                    {
                        if (CacheAmount > 0) return PaymentTypeCache;
                        if (TahseelAmount > 0) return PaymentTypeTahseel;
                        return PaymentTypeNoCustody;
                    }

                    return $"{PaymentTypeMixed}:Cache={CacheAmount};Tahseel={TahseelAmount};NoCustody={NoCustodyAmount}";
                }
            }

            public static PaymentCoverage Empty() => new(0, 0, 0);
            public static PaymentCoverage FromCache(decimal amount) => new(amount, 0, 0);
            public static PaymentCoverage FromTahseel(decimal amount) => new(0, amount, 0);
            public static PaymentCoverage FromNoCustody(decimal amount) => new(0, 0, amount);
        }
    }
}