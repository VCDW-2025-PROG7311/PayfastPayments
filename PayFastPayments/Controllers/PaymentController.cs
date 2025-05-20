using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly PayFastService _payFastService;

    public PaymentController(PayFastService payFastService, ApplicationDbContext dbContext)
    {
        _payFastService = payFastService;
        _dbContext = dbContext;
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok("App is running!");
    }

    [HttpPost("initiate-payment")]
    public IActionResult InitiatePayment(decimal amount)
    {
        var paymentUrl = _payFastService.GeneratePaymentData(amount);

        var transaction = new Transaction
        {
            MerchantId = Environment.GetEnvironmentVariable("PayFast__MerchantId"),
            Amount = amount,
            PaymentStatus = "Initiated",
            PaymentDate = DateTime.UtcNow, // Use UTC time instead of local time
        };

        _dbContext.Transactions.Add(transaction);
        _dbContext.SaveChanges();

        return Redirect(paymentUrl);
    }

    [HttpPost("verify-payment")]
    public async Task<IActionResult> VerifyPayment([FromBody] string response)
    {
        var paymentStatus = await _payFastService.VerifyPaymentResponseAsync(response);

        var transaction = _dbContext.Transactions.LastOrDefault();
        if (transaction != null)
        {
            transaction.PaymentStatus = paymentStatus;
            _dbContext.SaveChanges();
        }

        return Ok(paymentStatus);
    }

    [HttpGet("payment-success")]
    public IActionResult PaymentSuccess(
        [FromQuery] string m_payment_id,
        [FromQuery] string pf_payment_id,
        [FromQuery] string payment_status,
        [FromQuery] string amount_gross,
        [FromQuery] string amount_fee,
        [FromQuery] string amount_settled,
        [FromQuery] string item_name,
        [FromQuery] string item_description,
        [FromQuery] string email_address)
    {
        if (payment_status == "COMPLETE")
        {
            var transaction = _dbContext.Transactions.FirstOrDefault(t => t.MerchantId == m_payment_id);
            if (transaction != null)
            {
                transaction.PaymentStatus = "Completed";
                transaction.AmountPaid = decimal.Parse(amount_settled);
                _dbContext.SaveChanges();
            }

            return Ok("Payment successful. Thank you for your purchase.");
        }
        else
        {
            return BadRequest("Payment was not successful.");
        }
    }

    [HttpGet("payment-cancel")]
    public IActionResult PaymentCancel([FromQuery] string m_payment_id, [FromQuery] string payment_status)
    {
        if (payment_status == "CANCELLED")
        {
            var transaction = _dbContext.Transactions.FirstOrDefault(t => t.MerchantId == m_payment_id);
            if (transaction != null)
            {
                transaction.PaymentStatus = "Canceled";
                _dbContext.SaveChanges();
            }

            return Ok("Payment was canceled. Please try again.");
        }
        else
        {
            return BadRequest("Payment cancellation failed.");
        }
    }

    [HttpPost("payment-notify")]
    public async Task<IActionResult> PaymentNotify(
        [FromForm] string m_payment_id,
        [FromForm] string pf_payment_id,
        [FromForm] string payment_status,
        [FromForm] string amount_gross,
        [FromForm] string amount_fee,
        [FromForm] string amount_settled,
        [FromForm] string item_name,
        [FromForm] string item_description,
        [FromForm] string email_address)
    {
        var isValid = await _payFastService.ValidateNotificationAsync(m_payment_id, pf_payment_id, payment_status);

        if (isValid)
        {
            var transaction = _dbContext.Transactions.FirstOrDefault(t => t.MerchantId == m_payment_id);
            if (transaction != null)
            {
                transaction.PaymentStatus = payment_status;
                transaction.AmountPaid = decimal.Parse(amount_settled);
                _dbContext.SaveChanges();
            }

            return Ok("Notification received and processed.");
        }
        else
        {
            return BadRequest("Invalid notification received.");
        }
    }

    [HttpGet("all-transactions")]
    public IActionResult GetAllTransactions()
    {
        var transactions = _dbContext.Transactions.ToList();
        return Ok(transactions);
    }

}