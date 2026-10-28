namespace FinanceTrackerAPI.DTOs.Auth;

using System.ComponentModel.DataAnnotations;

public class SendOtpRequestDto
{
    [Required(ErrorMessage = "Mobile number is required")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number")]
    public string MobileNumber { get; set; } = string.Empty;

    public string? Purpose { get; set; } // "login" or "signup"
}

public class SendOtpResponseDto
{
    public string MobileNumber { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; } = 300; // 5 minutes
    public bool IsRegisteredUser { get; set; }
    
    // As explicitly requested:
    // "For signup and login, generate a random OTP instead of using a fixed OTP like 1234. For now, do not send SMS. Display the generated OTP on the same web screen for testing purposes. Later, when we integrate an SMS service, send the OTP to the user's mobile number and do not display it in the web application."
    public string? TestOtp { get; set; }
}

public class VerifyOtpRequestDto
{
    [Required(ErrorMessage = "Mobile number is required")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number")]
    public string MobileNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP is required")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be exactly 6 digits")]
    public string Otp { get; set; } = string.Empty;

    // Optional on login, mandatory on new user signup
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Purpose { get; set; } // "login" or "signup"
}

public class UserDto
{
    public Guid Id { get; set; }
    public string MobileNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsMobileVerified { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
    public bool IsNewUser { get; set; }
}

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
