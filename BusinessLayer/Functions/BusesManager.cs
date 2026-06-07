using BusinessLayer.DTOs.Buses;
using BusinessLayer.Models;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Functions
{
    public interface IBusesManager
    {
        Task<ResponceApi<List<GetBusDTO>>> GetAllBuses();
        Task<ResponceApi<GetBusDTO>> GetBusByIdAsync(Guid busId);
        Task<ResponceApi<bool>> CreateBusAsync(CreateNewBusDTO newBus);
        Task<ResponceApi<bool>> UpdateBusAsync(Guid busId, UpdateBusDTO updatedBus);
        Task<ResponceApi<bool>> DeleteBusAsync(Guid busId);
    }

    public class BusesManager : IBusesManager
    {
        private readonly DBContext _db;

        public BusesManager(DBContext db)
        {
            _db = db;
        }

        public async Task<ResponceApi<List<GetBusDTO>>> GetAllBuses()
        {
            try
            {
                var buses = await _db.Buses
                    .AsNoTracking()
                    .Select(b => new GetBusDTO
                    {
                        BusId = b.BusId,
                        Name = b.Name,
                        PalateNumber = b.PalateNumber,
                        CreateAt = b.CreateAt
                    })
                    .ToListAsync();

                return new ResponceApi<List<GetBusDTO>>
                {
                    Data = buses,
                    Success = true,
                    Message = "Buses retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResponceApi<List<GetBusDTO>>
                {
                    Data = new List<GetBusDTO>(),
                    Success = false,
                    Message = $"Error retrieving buses: {ex.Message}"
                };
            }
        }

        public async Task<ResponceApi<GetBusDTO>> GetBusByIdAsync(Guid busId)
        {
            try
            {
                var bus = await _db.Buses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.BusId == busId);

                if (bus == null)
                {
                    return new ResponceApi<GetBusDTO>
                    {
                        Data = null,
                        Success = false,
                        Message = "Bus not found"
                    };
                }

                return new ResponceApi<GetBusDTO>
                {
                    Data = new GetBusDTO
                    {
                        BusId = bus.BusId,
                        Name = bus.Name,
                        PalateNumber = bus.PalateNumber,
                        CreateAt = bus.CreateAt
                    },
                    Success = true,
                    Message = "Bus retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResponceApi<GetBusDTO>
                {
                    Data = null,
                    Success = false,
                    Message = $"Error retrieving bus: {ex.Message}"
                };
            }
        }

        public async Task<ResponceApi<bool>> CreateBusAsync(CreateNewBusDTO newBus)
        {
            try
            {
                if (newBus == null)
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Invalid bus data"
                    };
                }

                if (string.IsNullOrWhiteSpace(newBus.Name) ||
                    string.IsNullOrWhiteSpace(newBus.PalateNumber))
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Name and PalateNumber are required"
                    };
                }

                var exists = await _db.Buses
                    .AnyAsync(b => b.PalateNumber == newBus.PalateNumber);

                if (exists)
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Bus with this palate number already exists"
                    };
                }

                var bus = new Buses
                {
                    BusId = Guid.NewGuid(),
                    Name = newBus.Name.Trim(),
                    PalateNumber = newBus.PalateNumber.Trim(),
                    CreateAt = DateTime.UtcNow
                };

                await _db.Buses.AddAsync(bus);
                await _db.SaveChangesAsync();

                return new ResponceApi<bool>
                {
                    Data = true,
                    Success = true,
                    Message = "Bus created successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResponceApi<bool>
                {
                    Data = false,
                    Success = false,
                    Message = $"Error creating bus: {ex.Message}"
                };
            }
        }

        public async Task<ResponceApi<bool>> UpdateBusAsync(Guid busId, UpdateBusDTO updatedBus)
        {
            try
            {
                if (updatedBus == null)
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Invalid bus data"
                    };
                }

                var bus = await _db.Buses.FirstOrDefaultAsync(b => b.BusId == busId);

                if (bus == null)
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Bus not found"
                    };
                }

                if (string.IsNullOrWhiteSpace(updatedBus.Name) ||
                    string.IsNullOrWhiteSpace(updatedBus.PalateNumber))
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Name and PalateNumber are required"
                    };
                }

                var duplicatePlate = await _db.Buses.AnyAsync(b =>
                    b.BusId != busId &&
                    b.PalateNumber == updatedBus.PalateNumber);

                if (duplicatePlate)
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Another bus with this palate number already exists"
                    };
                }

                bus.Name = updatedBus.Name.Trim();
                bus.PalateNumber = updatedBus.PalateNumber.Trim();

                _db.Buses.Update(bus);
                await _db.SaveChangesAsync();

                return new ResponceApi<bool>
                {
                    Data = true,
                    Success = true,
                    Message = "Bus updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResponceApi<bool>
                {
                    Data = false,
                    Success = false,
                    Message = $"Error updating bus: {ex.Message}"
                };
            }
        }

        public async Task<ResponceApi<bool>> DeleteBusAsync(Guid busId)
        {
            try
            {
                var bus = await _db.Buses.FirstOrDefaultAsync(b => b.BusId == busId);

                if (bus == null)
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Bus not found"
                    };
                }

                _db.Buses.Remove(bus);
                await _db.SaveChangesAsync();

                return new ResponceApi<bool>
                {
                    Data = true,
                    Success = true,
                    Message = "Bus deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResponceApi<bool>
                {
                    Data = false,
                    Success = false,
                    Message = $"Error deleting bus: {ex.Message}"
                };
            }
        }
    }
}