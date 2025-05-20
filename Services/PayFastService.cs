using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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

    public string GeneratePaymentData(decimal amount)
    {
        var data = new Dictionary<string, string>
        {
            { "merchant_id", _merchantId },
            { "merchant_key", _merchantKey },
            { "amount", amount.ToString("0.00") },
            { "item_name", "Sample Item" },
            { "item_description", "Test Payment" },
            { "email_address", "test@gmail.com" },
            { "return_url", "https://your-app-url.com/api/payment/payment-success" },
            { "cancel_url", "https://your-app-url.com/api/payment/payment-cancel" },
            { "notify_url", "https://your-app-url.com/api/payment/payment-notify" },
        };

        var signature = CreateSignature(data);
        data.Add("signature", signature);

        var url = $"{_sandboxUrl}?{string.Join("&", data)}";
        return url;
    }

    private string CreateSignature(Dictionary<string, string> data)
    {
        var queryString = string.Join("&", data.OrderBy(d => d.Key).Select(d => $"{d.Key}={d.Value}"));

        var signatureString = queryString + "&passphrase=" + _passphrase;

        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower(); // Return the signature as a lowercase hex string
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
