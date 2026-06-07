using Asp.Versioning;
using BusinessLayer.DTOs.Buses;
using BusinessLayer.DTOs.Drivers;
using BusinessLayer.DTOs.Trips;
using BusinessLayer.Filters;
using BusinessLayer.Functions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TBus.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize]
    [ApiController]
    [EncryptedRole("Admin")]
    public class ManagementController : ControllerBase
    {
        private readonly IBusesManager _busesManager;
        private readonly IDriversManager _driversManager;
        private readonly ITripsManager _tripsManager;

        public ManagementController(IBusesManager busesManager, IDriversManager driversManager, ITripsManager tripsManager)
        {
            _busesManager = busesManager;
            _driversManager = driversManager;
            _tripsManager = tripsManager;
        }

        [HttpGet("buses")]
        public async Task<IActionResult> GetAllBuses()
        {
            var result = await _busesManager.GetAllBuses();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("buses/{busId}")]
        public async Task<IActionResult> GetBusById(Guid busId)
        {
            var result = await _busesManager.GetBusByIdAsync(busId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost("buses")]
        public async Task<IActionResult> CreateBus([FromBody] CreateNewBusDTO newBus)
        {
            var result = await _busesManager.CreateBusAsync(newBus);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("buses/{busId}")]
        public async Task<IActionResult> UpdateBus(Guid busId, [FromBody] UpdateBusDTO updatedBus)
        {
            var result = await _busesManager.UpdateBusAsync(busId, updatedBus);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("buses/{busId}")]
        public async Task<IActionResult> DeleteBus(Guid busId)
        {
            var result = await _busesManager.DeleteBusAsync(busId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("drivers")]
        public async Task<IActionResult> GetAllDrivers()
        {
            var result = await _driversManager.GetAllDriversAsync();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("drivers/{driverId}")]
        public async Task<IActionResult> GetDriverById(Guid driverId)
        {
            var result = await _driversManager.GetDriverByIdAsync(driverId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost("drivers")]
        public async Task<IActionResult> CreateDriver([FromBody] CreateDriverDTO newDriver)
        {
            if (newDriver == null)
                return BadRequest("Invalid driver data");

            newDriver.NationalId = newDriver.NationalId?.Trim() ?? string.Empty;
            newDriver.PhoneNumbers = newDriver.PhoneNumbers?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList() ?? new List<string>();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _driversManager.CreateDriverAsync(newDriver);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("drivers/{driverId}")]
        public async Task<IActionResult> UpdateDriver(Guid driverId, [FromBody] UpdateDriverDTO updatedDriver)
        {
            if (updatedDriver == null)
                return BadRequest("Invalid driver data");

            updatedDriver.NationalId = updatedDriver.NationalId?.Trim() ?? string.Empty;
            updatedDriver.PhoneNumbers = updatedDriver.PhoneNumbers?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList() ?? new List<string>();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _driversManager.UpdateDriverAsync(driverId, updatedDriver);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("drivers/{driverId}")]
        public async Task<IActionResult> DeleteDriver(Guid driverId)
        {
            var result = await _driversManager.DeleteDriverAsync(driverId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("trips")]
        public async Task<IActionResult> GetAllTrips()
        {
            var result = await _tripsManager.GetAllTripsAsync();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("trips/{tripId}")]
        public async Task<IActionResult> GetTripById(Guid tripId)
        {
            var result = await _tripsManager.GetTripByIdAsync(tripId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost("trips")]
        public async Task<IActionResult> CreateTrip([FromBody] CreateTripDTO newTrip)
        {
            newTrip.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) != null
                ? Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
                : Guid.Empty;

            var result = await _tripsManager.CreateTripAsync(newTrip);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("trips/{tripId}")]
        public async Task<IActionResult> UpdateTrip(Guid tripId, [FromBody] UpdateTripDTO updatedTrip)
        {
            var result = await _tripsManager.UpdateTripAsync(tripId, updatedTrip);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("trips/{tripId}")]
        public async Task<IActionResult> DeleteTrip(Guid tripId)
        {
            var result = await _tripsManager.DeleteTripAsync(tripId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // Admin-only hard delete. Removes the trip and its related payments/tahseel records
        // only if the trip departure time has not passed by more than 72 hours.
        [HttpDelete("trips/{tripId}/permanent")]
        public async Task<IActionResult> DeleteTripPermanently(Guid tripId)
        {
            var result = await _tripsManager.DeleteTripPermanentlyAsync(tripId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("trips/{tripId}/payments")]
        public async Task<IActionResult> AddPayment(Guid tripId, [FromBody] CreatePaymentDTO payment)
        {
            var result = await _tripsManager.AddPaymentAsync(tripId, payment);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("payments/{paymentId}")]
        public async Task<IActionResult> UpdatePayment(Guid paymentId, [FromBody] UpdatePaymentDTO payment)
        {
            var result = await _tripsManager.UpdatePaymentAsync(paymentId, payment);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("payments/{paymentId}")]
        public async Task<IActionResult> DeletePayment(Guid paymentId)
        {
            var result = await _tripsManager.DeletePaymentAsync(paymentId, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("trips/{tripId}/tahseel")]
        public async Task<IActionResult> AddTahseel(Guid tripId, [FromBody] CreateTahseelDTO dto)
        {
            dto.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) != null
                ? Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
                : Guid.Empty;

            var result = await _tripsManager.AddTahseelAsync(tripId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("tahseel/{tahseelId}")]
        public async Task<IActionResult> UpdateTahseel(Guid tahseelId, [FromBody] UpdateTahseelDTO dto)
        {
            dto.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) != null
                ? Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
                : Guid.Empty;

            var result = await _tripsManager.UpdateTahseelAsync(tahseelId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("tahseel/{tahseelId}")]
        public async Task<IActionResult> DeleteTahseel(Guid tahseelId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _tripsManager.DeleteTahseelAsync(tahseelId, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("trips/{tripId}/solar")]
        public async Task<IActionResult> UpdateSolar(Guid tripId, [FromBody] UpdateTripSolarDTO dto)
        {
            var result = await _tripsManager.UpdateSolarAsync(tripId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("trips/mark-done")]
        public async Task<IActionResult> MarkTripsAsDone([FromBody] MarkTripsDoneDTO dto)
        {
            var result = await _tripsManager.MarkTripsAsDoneAsync(dto.TripIds);
            return Ok(result);
        }
    }
}
