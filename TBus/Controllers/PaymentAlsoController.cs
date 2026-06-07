using Asp.Versioning;
using BusinessLayer.DTOs.PaymentAlso;
using BusinessLayer.Filters;
using BusinessLayer.Functions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize]
    public class PaymentAlsoController : ControllerBase
    {
        private readonly IPaymentAlsoManager _paymentAlsoManager;

        public PaymentAlsoController(IPaymentAlsoManager paymentAlsoManager)
        {
            _paymentAlsoManager = paymentAlsoManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _paymentAlsoManager.GetAllPaymentAlsoAsync();
            return Ok(result);
        }

        [HttpGet("{paymentAlsoId:guid}")]
        public async Task<IActionResult> GetById(Guid paymentAlsoId)
        {
            var result = await _paymentAlsoManager.GetPaymentAlsoByIdAsync(paymentAlsoId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentAlsoDTO dto)
        {
            var result = await _paymentAlsoManager.CreatePaymentAlsoAsync(dto);
            return Ok(result);
        }

        [HttpPut("{paymentAlsoId:guid}")]
        public async Task<IActionResult> Update(Guid paymentAlsoId, [FromBody] UpdatePaymentAlsoDTO dto)
        {
            var result = await _paymentAlsoManager.UpdatePaymentAlsoAsync(paymentAlsoId, dto);
            return Ok(result);
        }

        [HttpDelete("{paymentAlsoId:guid}")]
        public async Task<IActionResult> Delete(Guid paymentAlsoId, [FromQuery] Guid userId)
        {
            var result = await _paymentAlsoManager.DeletePaymentAlsoAsync(paymentAlsoId, userId);
            return Ok(result);
        }
    }
}
