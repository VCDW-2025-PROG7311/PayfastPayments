using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;

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
            { "amount", amount.ToString("F2", CultureInfo.InvariantCulture) },
            { "item_name", itemName },
            { "item_description", itemDescription },
            { "email_address", emailAddress },
            { "return_url", "https://payfastpayments.onrender.com/api/payment/payment-success" },
            { "cancel_url", "https://payfastpayments.onrender.com/api/payment/payment-cancel" },
            { "notify_url", "https://payfastpayments.onrender.com/api/payment/payment-notify" },
        };

        var signature = CreateSignature(data);
        // test: "949137cf0a104a76fb59fb1105815629" - works
        // tools: c9099e29757147f6ed2581083f7fdd30 - generates as this, doesn't work.
        
        data.Add("signature", signature);
        
        var url = $"https://sandbox.payfast.co.za/eng/process";

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

    public string CreateSignature(Dictionary<string, string> data)
    {
        var orderedData = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("merchant_id", data["merchant_id"]),
            new KeyValuePair<string, string>("merchant_key", data["merchant_key"]),
            new KeyValuePair<string, string>("return_url", data["return_url"]),
            new KeyValuePair<string, string>("cancel_url", data["cancel_url"]),
            new KeyValuePair<string, string>("notify_url", data["notify_url"]),
            new KeyValuePair<string, string>("email_address", data["email_address"]),
            new KeyValuePair<string, string>("amount", data["amount"]),
            new KeyValuePair<string, string>("item_name", data["item_name"]),
            new KeyValuePair<string, string>("item_description", data["item_description"]),
            new KeyValuePair<string, string>("passphrase", _passphrase)
        };

        var concatenatedString = string.Join("&", orderedData
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{kv.Key}={kv.Value}"));
        
        using (var md5 = MD5.Create())
        {
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(concatenatedString));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    public async Task<string> VerifyPaymentResponseAsync(string response)
    {
        var values = ParseQueryString(response);

        var responseSignature = values["signature"];
        values.Remove("signature");

        var calculatedSignature = CreateSignature(values);

        if (responseSignature == calculatedSignature)
        {
            return "Valid Payment Response";
        }
        else
        {
            return "Invalid Payment Response";
        }
    }

    private Dictionary<string, string> ParseQueryString(string queryString)
    {
        return queryString.Split('&')
                          .Select(param => param.Split('='))
                          .ToDictionary(param => param[0], param => param[1]);
    }

public async Task<bool> ValidateNotificationAsync(string m_payment_id, string pf_payment_id, string payment_status)
{
    var notificationData = new Dictionary<string, string>
    {
        { "m_payment_id", m_payment_id },
        { "pf_payment_id", pf_payment_id },
        { "payment_status", payment_status }
    };

    var notificationSignature = notificationData["signature"];

    notificationData.Remove("signature");

    var calculatedSignature = CreateSignature(notificationData);

    if (notificationSignature == calculatedSignature)
    {
        return true;
    }
    else
    {
        return false;
    }
}

}
