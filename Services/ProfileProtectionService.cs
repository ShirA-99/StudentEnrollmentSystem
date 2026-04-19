using Microsoft.AspNetCore.DataProtection;

namespace StudentEnrollmentSystem.Services;

public class ProfileProtectionService(IDataProtectionProvider dataProtectionProvider)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("StudentEnrollmentSystem.Profile.BankDetails.v1");

    public string? Protect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return _protector.Protect(value.Trim());
    }

    public string? Unprotect(string? protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(protectedValue);
        }
        catch
        {
            return null;
        }
    }

    public static string MaskAccountNumber(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            return "Not added";
        }

        var trimmed = accountNumber.Trim();
        var lastFour = trimmed.Length <= 4 ? trimmed : trimmed[^4..];
        var prefix = new string('*', Math.Max(0, trimmed.Length - lastFour.Length));
        return $"{prefix}{lastFour}";
    }
}
