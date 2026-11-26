namespace SevenPos.Core.Guests;

/// <summary>Mexican phone numbers (WhatsApp), stored as exactly 10 digits.</summary>
public static class PhoneNumber
{
    private const string Separators = " -.()+";

    /// <summary>
    /// Accepts the ways people write Mexican numbers: "55 1234 5678", "(55) 1234-5678",
    /// "+52 55 1234 5678", and the old mobile format "+52 1 55 1234 5678".
    /// Anything other than digits and separators makes the number invalid.
    /// </summary>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(input)) return false;
        if (input.Any(c => !char.IsAsciiDigit(c) && !Separators.Contains(c))) return false;

        var digits = new string(input.Where(char.IsAsciiDigit).ToArray());
        normalized = digits switch
        {
            { Length: 10 } => digits,
            { Length: 12 } when digits.StartsWith("52") => digits[2..],
            { Length: 13 } when digits.StartsWith("521") => digits[3..],
            _ => "",
        };
        return normalized.Length == 10;
    }

    /// <summary>"5512345678" → "55 1234 5678".</summary>
    public static string Format(string phone) =>
        phone.Length == 10 && phone.All(char.IsAsciiDigit) ? $"{phone[..2]} {phone[2..6]} {phone[6..]}" : phone;
}
