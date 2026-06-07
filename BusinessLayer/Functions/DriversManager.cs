using BusinessLayer.DTOs.Drivers;
using BusinessLayer.Models;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Functions
{
    public interface IDriversManager
    {
        Task<ResponceApi<List<GetDriverDTO>>> GetAllDriversAsync();

        Task<ResponceApi<GetDriverDTO>> GetDriverByIdAsync(Guid driverId);

        Task<ResponceApi<bool>> CreateDriverAsync(CreateDriverDTO newDriver);

        Task<ResponceApi<bool>> UpdateDriverAsync(Guid driverId, UpdateDriverDTO updatedDriver);

        Task<ResponceApi<bool>> DeleteDriverAsync(Guid driverId);
    }

    public class DriversManager : IDriversManager
    {
        private readonly DBContext _db;

        public DriversManager(DBContext db)
        {
            _db = db;
        }

        public async Task<ResponceApi<List<GetDriverDTO>>> GetAllDriversAsync()
        {
            try
            {
                var drivers = await _db.Drivers
                    .AsNoTracking()
                    .Include(d => d.PhoneNumbers)
                    .Select(d => new GetDriverDTO
                    {
                        DriverId = d.DriverId,
                        Name = d.Name,
                        NationalId = d.NationalId,
                        JouinAt = d.JouinAt,
                        PhoneNumbers = d.PhoneNumbers
                            .Select(p => p.Number)
                            .ToList()
                    })
                    .ToListAsync();

                return new ResponceApi<List<GetDriverDTO>>
                {
                    Data = drivers,
                    Success = true,
                    Message = "Drivers retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResponceApi<List<GetDriverDTO>>
                {
                    Data = new List<GetDriverDTO>(),
                    Success = false,
                    Message = $"Error retrieving drivers: {ex.Message}"
                };
            }
        }

        public async Task<ResponceApi<GetDriverDTO>> GetDriverByIdAsync(Guid driverId)
        {
            try
            {
                var driver = await _db.Drivers
                    .AsNoTracking()
                    .Include(d => d.PhoneNumbers)
                    .FirstOrDefaultAsync(d => d.DriverId == driverId);

                if (driver == null)
                {
                    return new ResponceApi<GetDriverDTO>
                    {
                        Data = null,
                        Success = false,
                        Message = "Driver not found"
                    };
                }

                return new ResponceApi<GetDriverDTO>
                {
                    Data = new GetDriverDTO
                    {
                        DriverId = driver.DriverId,
                        Name = driver.Name,
                        NationalId = driver.NationalId,
                        JouinAt = driver.JouinAt,
                        PhoneNumbers = driver.PhoneNumbers
                            .Select(p => p.Number)
                            .ToList()
                    },
                    Success = true,
                    Message = "Driver retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResponceApi<GetDriverDTO>
                {
                    Data = null,
                    Success = false,
                    Message = $"Error retrieving driver: {ex.Message}"
                };
            }
        }

        public async Task<ResponceApi<bool>> CreateDriverAsync(CreateDriverDTO newDriver)
        {
            try
            {
                if (newDriver == null)
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Invalid driver data"
                    };
                }

                if (string.IsNullOrWhiteSpace(newDriver.Name) ||
                    string.IsNullOrWhiteSpace(newDriver.NationalId))
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Name and NationalId are required"
                    };
                }

                var nationalIdExists = await _db.Drivers
                    .AnyAsync(d => d.NationalId == newDriver.NationalId);

                if (nationalIdExists)
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "National Id already exists"
                    };
                }

                foreach (var phone in newDriver.PhoneNumbers ?? [])
                {
                    var phoneNumbers = await _db.Drivers.AnyAsync(d => d.PhoneNumbers.Any(p => p.Number == phone));
                    if (phoneNumbers)
                    {
                        return new ResponceApi<bool>
                        {
                            Data = false,
                            Success = false,
                            Message = $"Phone Number {phone} already exists"
                        };
                    }
                }


                var driver = new Drivers
                {
                    DriverId = Guid.NewGuid(),
                    Name = newDriver.Name.Trim(),
                    NationalId = newDriver.NationalId.Trim(),
                    JouinAt = DateTime.UtcNow
                };

                if (newDriver.PhoneNumbers != null &&
                    newDriver.PhoneNumbers.Any())
                {
                    driver.PhoneNumbers = newDriver.PhoneNumbers
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Select(p => new PhoneNumbers
                        {
                            PhoneId = Guid.NewGuid(),
                            Number = p.Trim()
                        })
                        .ToList();
                }

                await _db.Drivers.AddAsync(driver);
                await _db.SaveChangesAsync();

                return new ResponceApi<bool>
                {
                    Data = true,
                    Success = true,
                    Message = "Driver created successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResponceApi<bool>
                {
                    Data = false,
                    Success = false,
                    Message = $"Error creating driver: {ex.Message}"
                };
            }
        }

        public async Task<ResponceApi<bool>> UpdateDriverAsync(Guid driverId, UpdateDriverDTO updatedDriver)
        {
            try
            {
                if (updatedDriver == null)
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Invalid driver data"
                    };
                }

                if (string.IsNullOrWhiteSpace(updatedDriver.Name) ||
                    string.IsNullOrWhiteSpace(updatedDriver.NationalId))
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Name and NationalId are required"
                    };
                }

                var driver = await _db.Drivers
                    .FirstOrDefaultAsync(d => d.DriverId == driverId);

                if (driver == null)
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Driver not found"
                    };
                }

                var nationalId = updatedDriver.NationalId.Trim();

                var nationalIdExists = await _db.Drivers
                    .AnyAsync(d =>
                        d.DriverId != driverId &&
                        d.NationalId == nationalId);

                if (nationalIdExists)
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Another driver already uses this NationalId"
                    };
                }

                var newPhones = updatedDriver.PhoneNumbers?
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim())
                    .Distinct()
                    .ToList() ?? new List<string>();

                foreach (var phone in newPhones)
                {
                    var phoneExists = await _db.PhoneNumbers
                        .AnyAsync(p =>
                            p.DriverId != driverId &&
                            p.Number == phone);

                    if (phoneExists)
                    {
                        return new ResponceApi<bool>
                        {
                            Data = false,
                            Success = false,
                            Message = $"Phone Number {phone} already exists"
                        };
                    }
                }

                driver.Name = updatedDriver.Name.Trim();
                driver.NationalId = nationalId;

                await _db.SaveChangesAsync();

                await _db.PhoneNumbers
                    .Where(p => p.DriverId == driverId)
                    .ExecuteDeleteAsync();

                var phonesToAdd = newPhones
                    .Select(phone => new PhoneNumbers
                    {
                        PhoneId = Guid.NewGuid(),
                        Number = phone,
                        DriverId = driverId
                    })
                    .ToList();

                if (phonesToAdd.Any())
                {
                    await _db.PhoneNumbers.AddRangeAsync(phonesToAdd);
                    await _db.SaveChangesAsync();
                }

                return new ResponceApi<bool>
                {
                    Data = true,
                    Success = true,
                    Message = "Driver updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResponceApi<bool>
                {
                    Data = false,
                    Success = false,
                    Message = $"Error updating driver: {ex.Message}"
                };
            }
        }

        public async Task<ResponceApi<bool>> DeleteDriverAsync(Guid driverId)
        {
            try
            {
                var driver = await _db.Drivers
                    .Include(d => d.PhoneNumbers)
                    .FirstOrDefaultAsync(d => d.DriverId == driverId);

                if (driver == null)
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Driver not found"
                    };
                }

                var hasTrips = await _db.Trips
                    .AnyAsync(t => t.DriverId == driverId);

                if (hasTrips)
                {
                    return new ResponceApi<bool>
                    {
                        Data = false,
                        Success = false,
                        Message = "Cannot delete this driver because he is assigned to one or more trips"
                    };
                }

                if (driver.PhoneNumbers.Any())
                {
                    _db.PhoneNumbers.RemoveRange(driver.PhoneNumbers);
                }

                _db.Drivers.Remove(driver);

                await _db.SaveChangesAsync();

                return new ResponceApi<bool>
                {
                    Data = true,
                    Success = true,
                    Message = "Driver deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResponceApi<bool>
                {
                    Data = false,
                    Success = false,
                    Message = $"Error deleting driver: {ex.Message}"
                };
            }
        }
    }
}