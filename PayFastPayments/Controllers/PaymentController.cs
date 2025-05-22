using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly TransactionService _transactionService;
    private readonly PayFastService _payFastService;

    public PaymentController(TransactionService transactionService, PayFastService payFastService)
    {
        _transactionService = transactionService;
        _payFastService = payFastService;
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok("App is running!");
    }

    [HttpGet("initiate-payment")]
    public IActionResult InitiatePayment(string orderId, string orderDescription, string email, decimal amount)
    {
        var transaction = new Transaction
        {
            OrderId = orderId,
            OrderDescription = orderDescription,
            Email = email,
            Amount = amount,
            PaymentStatus = "Initiated",
            PaymentDate = DateTime.UtcNow,
        };

        _transactionService.AddTransaction(transaction);
        string htmlForm = _payFastService.GeneratePaymentData(amount, orderId, orderDescription, email);

        return Content(htmlForm, "text/html");
    }

    [HttpGet("payment-success")]
    public IActionResult PaymentSuccess()
    {
        return Ok("Payment successful. Thank you for your purchase.");     
    }

    [HttpGet("payment-cancel")]
    public IActionResult PaymentCancel()
    {
        return Ok("Payment was canceled. Please try again.");        
    }

    [HttpPost("payment-notify")]
    public async Task<IActionResult> Notify()
    {
        var form = await Request.ReadFormAsync();
        foreach (var key in form.Keys)
        {
            Console.WriteLine($"{key}: {form[key]}");
        }

        return Ok();
    }
    /**
    [HttpPost("payment-notify")]
    public IActionResult PaymentNotify([FromForm] ITN_Payload ITN_payload)
    {
        System.Console.WriteLine("Notified - " + ITN_payload.PaymentStatus);

        if (ITN_payload.PaymentStatus == "COMPLETE")
        {
            var transaction = _transactionService.GetTransactions()
                .FirstOrDefault(t => t.OrderId == ITN_payload.ItemName);
            if (transaction != null)
            {
                transaction.PaymentId = ITN_payload.PfPaymentId;
                transaction.PaymentStatus = ITN_payload.PaymentStatus;
                transaction.AmountPaid = ITN_payload.AmountGross;
                _transactionService.SaveTransactions(_transactionService.GetTransactions());
            }
        }
        return Ok();
    }
    **/

    [HttpGet("all-transactions")]
    public IActionResult GetAllTransactions()
    {
        var transactions = _transactionService.GetTransactions();
        return Ok(transactions);
    }

    [HttpGet("clear-transactions")]
    public IActionResult ClearTransactions()
    {
        _transactionService.ClearTransactions();
        return Ok();
    }
    
    

}