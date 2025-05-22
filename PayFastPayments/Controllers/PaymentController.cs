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
            PaymentStatus = "PENDING",
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
        var notification = new PaymentNotification
        {
            PaymentStatus = form["payment_status"],
            PfPaymentId = form["pf_payment_id"],
            MPaymentId = form["m_payment_id"],
            ItemName = form["item_name"],
            ItemDescription = form["item_description"],
            AmountGross = decimal.TryParse(form["amount_gross"], out var gross) ? gross : 0,
            AmountFee = decimal.TryParse(form["amount_fee"], out var fee) ? fee : 0,
            AmountNet = decimal.TryParse(form["amount_net"], out var net) ? net : 0,
            CustomStr1 = form["custom_str1"],
            CustomStr2 = form["custom_str2"],
            CustomStr3 = form["custom_str3"],
            CustomStr4 = form["custom_str4"],
            CustomStr5 = form["custom_str5"],
            EmailAddress = form["email_address"],
            MerchantId = form["merchant_id"],
            Signature = form["signature"]
        };

        if (notification.PaymentStatus == "COMPLETE")
        {
            var transactions = _transactionService.GetTransactions();
            var transaction = transactions.FirstOrDefault(t => t.OrderId == notification.ItemName);
            
            if (transaction != null)
            {
                transaction.PaymentId = notification.PfPaymentId;
                transaction.PaymentStatus = notification.PaymentStatus;
                transaction.AmountPaid = notification.AmountGross;

                _transactionService.SaveTransactions(transactions);
            }
        } else if(notification.PaymentStatus == "CANCELLED")
        {
            var transactions = _transactionService.GetTransactions();
            var transaction = transactions.FirstOrDefault(t => t.OrderId == notification.ItemName);
            
            if (transaction != null)
            {
                transaction.PaymentId = notification.PfPaymentId;
                transaction.PaymentStatus = notification.PaymentStatus;
                _transactionService.SaveTransactions(transactions);
            }
        }

        return Ok();
    }
    
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