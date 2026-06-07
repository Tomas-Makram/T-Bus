using Asp.Versioning;
using BusinessLayer.DTOs.Trips;
using BusinessLayer.Filters;
using BusinessLayer.Functions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TBus.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/driver")]
    [Authorize]
    [EncryptedRole("Driver")]
    public class DriverController : ControllerBase
    {
        private readonly ITripsManager _tripsManager;
        private readonly IDriversManager _driversManager;

        public DriverController(ITripsManager tripsManager, IDriversManager driversManager)
        {
            _tripsManager = tripsManager;
            _driversManager = driversManager;
        }

        private Guid? GetDriverIdFromToken()
        {
            var value = User.FindFirstValue("driver_id");
            if (string.IsNullOrWhiteSpace(value)) return null;
            return Guid.TryParse(value, out var id) ? id : null;
        }

        [HttpGet("my-profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var driverId = GetDriverIdFromToken();
            if (driverId == null)
                return Forbid("This user is not linked to a driver.");

            var result = await _driversManager.GetDriverByIdAsync(driverId.Value);

            if (!result.Success || result.Data == null)
                return NotFound(result);

            return Ok(result);
        }

        [HttpGet("today-trips")]
        public async Task<IActionResult> GetTodayTrips()
        {
            var driverId = GetDriverIdFromToken();
            if (driverId == null)
                return Forbid("This user is not linked to a driver.");

            var result = await _tripsManager.GetAllTripsAsync();
            if (!result.Success || result.Data == null)
                return BadRequest(result);

            var today = DateTime.UtcNow.Date;

            result.Data = result.Data
                .Where(t => t.DriverId == driverId.Value && t.TripDate.Date == today)
                .ToList();

            return Ok(result);
        }

        [HttpGet("my-trips")]
        public async Task<IActionResult> GetMyTrips()
        {
            var driverId = GetDriverIdFromToken();
            if (driverId == null)
                return Forbid("This user is not linked to a driver.");

            var result = await _tripsManager.GetAllTripsAsync();
            if (!result.Success || result.Data == null)
                return BadRequest(result);

            result.Data = result.Data
                .Where(t => t.DriverId == driverId.Value)
                .OrderByDescending(t => t.TripDate)
                .ToList();

            return Ok(result);
        }

        [HttpGet("trips/{tripId}")]
        public async Task<IActionResult> GetMyTrip(Guid tripId)
        {
            var driverId = GetDriverIdFromToken();
            if (driverId == null)
                return Forbid("This user is not linked to a driver.");

            var result = await _tripsManager.GetTripByIdAsync(tripId);
            if (!result.Success || result.Data == null)
                return NotFound(result);

            if (result.Data.DriverId != driverId.Value)
                return Forbid("You are not allowed to access this trip.");

            return Ok(result);
        }

        [HttpPost("trips/{tripId}/payments")]
        public async Task<IActionResult> AddPayment(Guid tripId, [FromBody] CreatePaymentDTO payment)
        {
            var driverId = GetDriverIdFromToken();
            if (driverId == null)
                return Forbid("This user is not linked to a driver.");

            payment.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) != null ? Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : Guid.Empty;

            var tripResult = await _tripsManager.GetTripByIdAsync(tripId);
            if (!tripResult.Success || tripResult.Data == null)
                return NotFound(tripResult);

            if (tripResult.Data.DriverId != driverId.Value)
                return Forbid("You are not allowed to add payment to this trip.");

            var result = await _tripsManager.AddPaymentAsync(tripId, payment);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("payments/{paymentId}")]
        public async Task<IActionResult> UpdatePayment(Guid paymentId, [FromBody] UpdatePaymentDTO payment)
        {
            var driverId = GetDriverIdFromToken();
            if (driverId == null)
                return Forbid("This user is not linked to a driver.");

            payment.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) != null ? Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : Guid.Empty;

            var myTrips = await _tripsManager.GetAllTripsAsync();
            if (!myTrips.Success || myTrips.Data == null)
                return BadRequest(myTrips);

            var belongs = myTrips.Data.Any(t =>
                t.DriverId == driverId.Value &&
                t.Payments.Any(p => p.PaymentId == paymentId));

            if (!belongs)
                return Forbid("You are not allowed to update this payment.");

            var result = await _tripsManager.UpdatePaymentAsync(paymentId, payment);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("payments/{paymentId}")]
        public async Task<IActionResult> DeletePayment(Guid paymentId)
        {
            var driverId = GetDriverIdFromToken();
            if (driverId == null)
                return Forbid("This user is not linked to a driver.");

            var myTrips = await _tripsManager.GetAllTripsAsync();
            if (!myTrips.Success || myTrips.Data == null)
                return BadRequest(myTrips);

            var belongs = myTrips.Data.Any(t =>
                t.DriverId == driverId.Value &&
                t.Payments.Any(p => p.PaymentId == paymentId));

            if (!belongs)
                return Forbid("You are not allowed to delete this payment.");

            var result = await _tripsManager.DeletePaymentAsync(paymentId, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("trips/{tripId}/tahseel")]
        public async Task<IActionResult> AddTahseel(Guid tripId, [FromBody] CreateTahseelDTO dto)
        {
            var driverId = GetDriverIdFromToken();
            if (driverId == null)
                return Forbid("This user is not linked to a driver.");

            dto.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) != null ? Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : Guid.Empty;

            var tripResult = await _tripsManager.GetTripByIdAsync(tripId);
            if (!tripResult.Success || tripResult.Data == null)
                return NotFound(tripResult);

            if (tripResult.Data.DriverId != driverId.Value)
                return Forbid("You are not allowed to add tahseel to this trip.");

            var result = await _tripsManager.AddTahseelAsync(tripId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("tahseel/{tahseelId}")]
        public async Task<IActionResult> UpdateTahseel(Guid tahseelId, [FromBody] UpdateTahseelDTO dto)
        {
            var driverId = GetDriverIdFromToken();
            if (driverId == null)
                return Forbid("This user is not linked to a driver.");

            dto.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) != null ? Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : Guid.Empty;

            var myTrips = await _tripsManager.GetAllTripsAsync();
            if (!myTrips.Success || myTrips.Data == null)
                return BadRequest(myTrips);

            var belongs = myTrips.Data.Any(t =>
                t.DriverId == driverId.Value &&
                t.TahseelItems!.Any(x => x.TahseelId == tahseelId));

            if (!belongs)
                return Forbid("You are not allowed to update this tahseel.");

            var result = await _tripsManager.UpdateTahseelAsync(tahseelId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("tahseel/{tahseelId}")]
        public async Task<IActionResult> DeleteTahseel(Guid tahseelId)
        {
            var driverId = GetDriverIdFromToken();
            if (driverId == null)
                return Forbid("This user is not linked to a driver.");

            var myTrips = await _tripsManager.GetAllTripsAsync();
            if (!myTrips.Success || myTrips.Data == null)
                return BadRequest(myTrips);

            var belongs = myTrips.Data.Any(t =>
                t.DriverId == driverId.Value &&
                t.TahseelItems!.Any(x => x.TahseelId == tahseelId));

            if (!belongs)
                return Forbid("You are not allowed to delete this tahseel.");

            var result = await _tripsManager.DeleteTahseelAsync(tahseelId, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
            return result.Success ? Ok(result) : BadRequest(result);
        }

    }
}