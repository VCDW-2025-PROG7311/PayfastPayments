## PayFast Integration Activity Guide (ASP.NET Core Web API + Render Deployment)

This guide is designed to walk you through the process of integrating PayFast into an ASP.NET Core Web API application. You will learn how to initiate payments, receive ITN (Instant Transaction Notification) messages, and persist transaction data to a JSON file. You will also learn how to deploy the application to Render using Docker.

### Step 1: Get PayFast Sandbox Credentials
Visit https://sandbox.payfast.co.za to register a sandbox account. Once logged in:
1. Go to "Settings"
2. Click "Integration"
3. Note the following values:
   - Merchant ID
   - Merchant Key
   - Passphrase (add one in)
4. Set the sandbox URL to:
   - `https://sandbox.payfast.co.za/eng/process`

### Step 2: Set Environment Variables
These credentials must be added both locally and in Render.

#### Locally (for testing):
Add these environment variables in Windows.

```
PayFast_MerchantId: your-merchant-id
PayFast_MerchantKey: your-merchant-key
PayFast_Passphrase: your-passphrase
PayFast_SandboxUrl: https://sandbox.payfast.co.za/eng/process
```

#### On Render:
1. Go to your Render dashboard
2. Select your Web Service
3. Click the "Environment" tab
4. Add each variable:
   - `PayFast_MerchantId`
   - `PayFast_MerchantKey`
   - `PayFast_Passphrase`
   - `PayFast_SandboxUrl`

These variables are injected into the container at runtime and used by the `PayFastService` class.

### Step 3: Create the ASP.NET Core Project
Open a terminal and run the following commands:
```bash
mkdir PayFastPayments
cd PayFastPayments
dotnet new webapi
```

### Step 4: Add Swagger for API Documentation
Run this command to add Swagger:
```bash
dotnet add package Swashbuckle.AspNetCore
```
This will allow you to visually test endpoints at `/swagger`.

### Step 5: Implement the Models

#### Transaction.cs in the Models folder
```csharp
public class Transaction
{
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentStatus { get; set; }
    public string OrderId { get; set; }
    public string OrderDescription { get; set; }
    public string Email { get; set; }
    public string PaymentId { get; set; }
}

```

#### PaymentNotification.cs in the Models folder
```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

public class PaymentNotification
{
    [FromForm(Name = "payment_status")]
    [Required]
    public string PaymentStatus { get; set; }

    [FromForm(Name = "pf_payment_id")]
    [Required]
    public string PfPaymentId { get; set; }

    [FromForm(Name = "m_payment_id")]
    public string MPaymentId { get; set; }

    [FromForm(Name = "item_name")]
    [Required]
    public string ItemName { get; set; }

    [FromForm(Name = "item_description")]
    public string ItemDescription { get; set; }

    [FromForm(Name = "amount_gross")]
    public decimal AmountGross { get; set; }

    [FromForm(Name = "amount_fee")]
    public decimal AmountFee { get; set; }

    [FromForm(Name = "amount_net")]
    public decimal AmountNet { get; set; }

    [FromForm(Name = "custom_str1")]
    public string CustomStr1 { get; set; }

    [FromForm(Name = "custom_str2")]
    public string CustomStr2 { get; set; }

    [FromForm(Name = "custom_str3")]
    public string CustomStr3 { get; set; }

    [FromForm(Name = "custom_str4")]
    public string CustomStr4 { get; set; }

    [FromForm(Name = "custom_str5")]
    public string CustomStr5 { get; set; }

    [FromForm(Name = "email_address")]
    public string EmailAddress { get; set; }

    [FromForm(Name = "merchant_id")]
    [Required]
    public string MerchantId { get; set; }

    [FromForm(Name = "signature")]
    public string Signature { get; set; }
}

```

### Step 6: Create TransactionService.cs in the Services folder
Make sure you actually understand the code!
```csharp
using Newtonsoft.Json;

public class TransactionService
{
    private readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "transactions.json");

    public List<Transaction> GetTransactions()
    {
        if (!File.Exists(_filePath))
            return new List<Transaction>();

        var json = File.ReadAllText(_filePath);
        return JsonConvert.DeserializeObject<List<Transaction>>(json) ?? new List<Transaction>();
    }

    public void SaveTransactions(List<Transaction> transactions)
    {
        var json = JsonConvert.SerializeObject(transactions, Formatting.Indented);
        File.WriteAllText(_filePath, json);
    }

    public void AddTransaction(Transaction transaction)
    {
        var transactions = GetTransactions();
        transactions.Add(transaction);
        SaveTransactions(transactions);
    }

    public void ClearTransactions()
    {
        var emptyList = new List<Transaction>();
        var json = JsonConvert.SerializeObject(emptyList, Formatting.Indented);
        File.WriteAllText(_filePath, json);
    }
}

```

### Step 7: Create PayFastService. in the Services folder
Make sure you actually understand the code!
```csharp
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public class PayFastService
{
    private readonly string _merchantId;
    private readonly string _merchantKey;
    private readonly string _passphrase;
    private readonly string _sandboxUrl;

    public PayFastService(string merchantId, string merchantKey, string passphrase, string sandboxUrl)
    {
        _merchantId = merchantId;
        _merchantKey = merchantKey;
        _passphrase = passphrase;
        _sandboxUrl = sandboxUrl;
    }

    public string GeneratePaymentData(decimal amount, string itemName, string itemDescription, string emailAddress)
    {
        var data = new Dictionary<string, string>
        {
            { "merchant_id", _merchantId },
            { "merchant_key", _merchantKey },            
            { "return_url", "https://[your site].onrender.com/api/payment/payment-success" },
            { "cancel_url", "https://[your site].onrender.com/api/payment/payment-cancel" },
            { "notify_url", "https://[your site].onrender.com/api/payment/payment-notify" },
            { "email_address", emailAddress },
            { "amount", amount.ToString("F2", CultureInfo.InvariantCulture) },
            { "item_name", itemName },
            { "item_description", itemDescription },
        };

        var signature = CreateSignature(data);

        data.Add("signature", signature);
        
        var url = _sandboxUrl;

        var formData = data.Select(kv => $"<input type='hidden' name='{kv.Key}' value='{kv.Value}' />").ToList();

        var htmlForm = $@"
            <!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Redirecting to PayFast...</title>
                <script type='text/javascript'>
                    window.onload = function() {{
                        document.getElementById('payfast_form').submit(); // Auto-submit form to PayFast
                    }};
                </script>
            </head>
            <body>
                <h2>Redirecting you to PayFast...</h2>
                <p>If you're not redirected, <a href='#' onclick='document.getElementById('payfast_form').submit();'>click here</a>.</p>
                <form id='payfast_form' action='{url}' method='POST'>
                    {string.Join("\n", formData)}
                    <input type='submit' value='Pay Now' style='display:none;' /> <!-- Hidden submit button -->
                </form>
            </body>
            </html>";

        return htmlForm;        
    }

    // Portions adapted from Payfast Nuget Package Code
    public string CreateSignature(Dictionary<string, string> data)
    {
        var orderedData = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("merchant_id", data.ContainsKey("merchant_id") ? data["merchant_id"] : ""),
            new KeyValuePair<string, string>("merchant_key", data.ContainsKey("merchant_key") ? data["merchant_key"] : ""),
            new KeyValuePair<string, string>("return_url", data.ContainsKey("return_url") ? data["return_url"] : ""),
            new KeyValuePair<string, string>("cancel_url", data.ContainsKey("cancel_url") ? data["cancel_url"] : ""),
            new KeyValuePair<string, string>("notify_url", data.ContainsKey("notify_url") ? data["notify_url"] : ""),
            new KeyValuePair<string, string>("email_address", data.ContainsKey("email_address") ? data["email_address"] : ""),
            new KeyValuePair<string, string>("amount", data.ContainsKey("amount") ? data["amount"] : ""),
            new KeyValuePair<string, string>("item_name", data.ContainsKey("item_name") ? data["item_name"] : ""),
            new KeyValuePair<string, string>("item_description", data.ContainsKey("item_description") ? data["item_description"] : ""),
            new KeyValuePair<string, string>("passphrase", _passphrase)
        };

        var payload = new StringBuilder();
        foreach (var item in orderedData)
        {
            if (item.Key != orderedData.Last().Key)
            {
                payload.Append($"{item.Key}={UrlEncode(item.Value)}&");
            }
            else
            {
                payload.Append($"{item.Key}={UrlEncode(item.Value)}");
            }
        }

        var md5 = MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(payload.ToString());
        var hash = md5.ComputeHash(inputBytes);

        var signature = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            signature.Append(hash[i].ToString("x2"));
        }
        return signature.ToString();
    }

    // Adapted from Payfast Nuget Package Code
    protected string UrlEncode(string url)
    {
        Dictionary<string, string> convertPairs = new Dictionary<string, string>() { { "%", "%25" }, { "!", "%21" }, { "#", "%23" }, { " ", "+" },
        { "$", "%24" }, { "&", "%26" }, { "'", "%27" }, { "(", "%28" }, { ")", "%29" }, { "*", "%2A" }, { "+", "%2B" }, { ",", "%2C" },
        { "/", "%2F" }, { ":", "%3A" }, { ";", "%3B" }, { "=", "%3D" }, { "?", "%3F" }, { "@", "%40" }, { "[", "%5B" }, { "]", "%5D" } };
        var replaceRegex = new Regex(@"[%!# $&'()*+,/:;=?@\[\]]");
        MatchEvaluator matchEval = match => convertPairs[match.Value];
        string encoded = replaceRegex.Replace(url, matchEval);
        return encoded;
    }
}

```
### Step 8: Implement the PaymentController.cs in the controllers folder
Make sure you actually understand the code!
```csharp
using Microsoft.AspNetCore.Mvc;

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
```

### Step 9: Update Program.cs
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TransactionService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<PayFastService>(serviceProvider =>
{
    var configuration = builder.Configuration;
    var merchantId = configuration["PayFast_MerchantId"];
    var merchantKey = configuration["PayFast_MerchantKey"];
    var passphrase = configuration["PayFast_Passphrase"];
    var sandboxUrl = configuration["PayFast_SandboxUrl"];    
    return new PayFastService(merchantId, merchantKey, passphrase, sandboxUrl);
});

// some existing code

var app = builder.Build();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

// some existing code
```

### Step 10: Dockerize
Create a DockerFile and add the following:
```
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["PayFastPayments/PayFastPayments.csproj", "PayFastPayments/"]

RUN dotnet restore "PayFastPayments/PayFastPayments.csproj"

COPY . .

WORKDIR "/src/PayFastPayments"

RUN dotnet build "PayFastPayments.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PayFastPayments.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "PayFastPayments.dll"]
```

### Step 11: Deploy to Render
1. Push to GitHub
2. Create a new Web Service on Render
3. Ensure you have the environment variables:
   - PayFast_MerchantId
   - PayFast_MerchantKey
   - PayFast_Passphrase
   - PayFast_SandboxUrl
4. Run your app

