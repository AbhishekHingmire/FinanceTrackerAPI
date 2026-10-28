namespace FinanceTrackerAPI.Services.Implementations;

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using FinanceTrackerAPI.Services.Interfaces;

public class TextplateSmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TextplateSmsService> _logger;

    public TextplateSmsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TextplateSmsService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendOtpSmsAsync(string mobileNumber, string otpCode, CancellationToken ct = default)
    {
        // 1. Strict Indian Mobile Number Validation before attempting API call (preserves quota)
        var cleanedDigits = Regex.Replace(mobileNumber ?? string.Empty, @"\D", "");
        string formattedMobile;

        if (cleanedDigits.Length == 10 && Regex.IsMatch(cleanedDigits, @"^[6-9]\d{9}$"))
        {
            formattedMobile = $"+91{cleanedDigits}";
        }
        else if (cleanedDigits.Length == 12 && cleanedDigits.StartsWith("91") && Regex.IsMatch(cleanedDigits[2..], @"^[6-9]\d{9}$"))
        {
            formattedMobile = $"+{cleanedDigits}";
        }
        else
        {
            return new SmsSendResult
            {
                Success = false,
                Message = "Invalid mobile number format. Please provide a valid 10-digit Indian mobile number."
            };
        }

        // Validate 6-digit numeric OTP
        if (string.IsNullOrWhiteSpace(otpCode) || !Regex.IsMatch(otpCode, @"^\d{4,8}$"))
        {
            return new SmsSendResult
            {
                Success = false,
                Message = "Invalid OTP code generated."
            };
        }

        // 2. Read Configuration
        var isEnabled = _configuration.GetValue<bool>("Textplate:Enabled", true);
        if (!isEnabled)
        {
            _logger.LogWarning("Textplate SMS service is disabled in configuration. Simulating success for mobile {Mobile}", formattedMobile);
            return new SmsSendResult
            {
                Success = true,
                Message = "SMS dispatch is simulated (Textplate:Enabled = false).",
                GatewayStatus = "Simulated"
            };
        }

        var apiUrl = _configuration["Textplate:ApiUrl"] ?? "https://api.textplate.in/v1/send-sms";
        var apiKey = _configuration["Textplate:ApiKey"] ?? "0f5d7968183ceabe30fac292f87e4a56711d6450";
        var templateId = _configuration["Textplate:TemplateId"] ?? "1";
        var expiryValue = _configuration["Textplate:ExpiryValue"] ?? "5 minutes";
        var detailValue = _configuration["Textplate:DetailValue"] ?? "WealthTrack verification";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new SmsSendResult
            {
                Success = false,
                Message = "Textplate API Key is not configured."
            };
        }

        // 3. Build Multipart Form Data matching cURL -F specifications
        using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

        using var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent(formattedMobile), "mobileNumber");
        formContent.Add(new StringContent(templateId), "templateId");
        formContent.Add(new StringContent(otpCode), "otpValue");
        formContent.Add(new StringContent(expiryValue), "expiryValue");
        formContent.Add(new StringContent(detailValue), "detailValue");

        request.Content = formContent;

        try
        {
            _logger.LogInformation("Dispatching SMS via Textplate for recipient {Mobile} with Template {TemplateId}",
                formattedMobile[..Math.Min(formattedMobile.Length, 6)] + "****", templateId);

            var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("Textplate response: HTTP {StatusCode}, Body: {Body}",
                (int)response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                var failureMsg = ParseErrorMessage(responseBody, $"SMS service error (HTTP {(int)response.StatusCode}: {response.ReasonPhrase})");
                return new SmsSendResult
                {
                    Success = false,
                    Message = failureMsg,
                    GatewayStatus = response.StatusCode.ToString(),
                    RawResponse = responseBody
                };
            }

            // Inspect parsed JSON body if any error was reported inside 200 envelope
            var parsedError = CheckForErrorInSuccessPayload(responseBody);
            if (!string.IsNullOrWhiteSpace(parsedError))
            {
                return new SmsSendResult
                {
                    Success = false,
                    Message = parsedError,
                    GatewayStatus = "Rejected",
                    RawResponse = responseBody
                };
            }

            return new SmsSendResult
            {
                Success = true,
                Message = "OTP dispatched successfully via SMS to your mobile number.",
                GatewayStatus = "Delivered",
                RawResponse = responseBody
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while calling Textplate SMS API");
            return new SmsSendResult
            {
                Success = false,
                Message = $"SMS gateway communication error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during SMS dispatch");
            return new SmsSendResult
            {
                Success = false,
                Message = $"Failed to dispatch SMS: {ex.Message}"
            };
        }
    }

    private static string ParseErrorMessage(string body, string defaultMessage)
    {
        if (string.IsNullOrWhiteSpace(body)) return defaultMessage;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
            {
                return msgProp.GetString()!;
            }

            if (root.TryGetProperty("error", out var errProp))
            {
                if (errProp.ValueKind == JsonValueKind.String) return errProp.GetString()!;
                if (errProp.TryGetProperty("message", out var nestedMsg) && nestedMsg.ValueKind == JsonValueKind.String)
                {
                    return nestedMsg.GetString()!;
                }
            }
        }
        catch
        {
            // Not valid JSON
        }

        return body.Length > 200 ? body[..200] : body;
    }

    private static string? CheckForErrorInSuccessPayload(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("status", out var statusProp) &&
                statusProp.ValueKind == JsonValueKind.String &&
                statusProp.GetString()?.Equals("error", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (root.TryGetProperty("message", out var msgProp)) return msgProp.GetString();
                return "SMS gateway rejected the dispatch request.";
            }

            if (root.TryGetProperty("success", out var succProp) &&
                succProp.ValueKind == JsonValueKind.False)
            {
                if (root.TryGetProperty("message", out var msgProp)) return msgProp.GetString();
                return "SMS gateway rejected the dispatch request.";
            }
        }
        catch
        {
            // Ignore JSON parse error on successful status code
        }

        return null;
    }
}
