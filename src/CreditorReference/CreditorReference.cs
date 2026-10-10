using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Iso11649.Internal;

namespace Iso11649;

/// <summary>
/// An ISO 11649 Structured Creditor Reference ("RF" reference): "RF", two ISO 7064 MOD 97-10
/// check digits, and a reference body of 1 to 21 alphanumeric characters (25 characters at most).
/// </summary>
/// <remarks>All operations are culture-independent. Input is case-insensitive and may contain spaces.</remarks>
public sealed class CreditorReference : IEquatable<CreditorReference>
{
    private CreditorReference(string checkDigits, string body)
    {
        CheckDigits = checkDigits;
        Body = body;
    }

    /// <summary>Gets the two check digits.</summary>
    public string CheckDigits { get; }

    /// <summary>Gets the reference body (1 to 21 characters, upper case).</summary>
    public string Body { get; }

    /// <summary>Gets the compact reference, for example "RF18539007547034".</summary>
    public string Value => string.Concat(Constants.Prefix, CheckDigits, Body);

    /// <summary>Returns whether <paramref name="value"/> is a valid creditor reference.</summary>
    /// <param name="value">The candidate reference, with or without spaces.</param>
    /// <returns><see langword="true"/> if valid; otherwise <see langword="false"/>.</returns>
    public static bool IsValid(string? value) => TryParse(value, out _, out _);

    /// <summary>Validates <paramref name="value"/> and reports why it failed, if it did.</summary>
    /// <param name="value">The candidate reference, with or without spaces.</param>
    /// <param name="error">The failure reason, or <see cref="CreditorReferenceError.None"/>.</param>
    /// <returns><see langword="true"/> if valid; otherwise <see langword="false"/>.</returns>
    public static bool TryValidate(string? value, out CreditorReferenceError error) =>
        TryParse(value, out _, out error);

    /// <summary>Validates <paramref name="value"/> and throws if it is not a valid creditor reference.</summary>
    /// <param name="value">The candidate reference, with or without spaces.</param>
    /// <exception cref="FormatException">The value is not a valid creditor reference.</exception>
    public static void Validate(string? value) => _ = Parse(value);

    /// <summary>Parses a creditor reference.</summary>
    /// <param name="value">The reference, with or without spaces, in any case.</param>
    /// <returns>The parsed reference.</returns>
    /// <exception cref="FormatException">The value is not a valid creditor reference.</exception>
    public static CreditorReference Parse(string? value) =>
        TryParse(value, out var result, out var error)
            ? result
            : throw new FormatException(Describe(error));

    /// <summary>Tries to parse a creditor reference.</summary>
    /// <param name="value">The reference, with or without spaces, in any case.</param>
    /// <param name="result">The parsed reference when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if valid; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string? value, [NotNullWhen(true)] out CreditorReference? result) =>
        TryParse(value, out result, out _);

    /// <summary>Tries to parse a creditor reference and reports the failure reason.</summary>
    /// <param name="value">The reference, with or without spaces, in any case.</param>
    /// <param name="result">The parsed reference when the method returns <see langword="true"/>.</param>
    /// <param name="error">The failure reason, or <see cref="CreditorReferenceError.None"/>.</param>
    /// <returns><see langword="true"/> if valid; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(
        string? value,
        [NotNullWhen(true)] out CreditorReference? result,
        out CreditorReferenceError error)
    {
        result = null;
        var normalized = Normalize(value);

        if (normalized.Length == 0)
        {
            error = CreditorReferenceError.NullOrEmpty;
            return false;
        }

        if (!normalized.StartsWith(Constants.Prefix, StringComparison.Ordinal))
        {
            error = CreditorReferenceError.MissingPrefix;
            return false;
        }

        if (normalized.Length > Constants.MaxLength)
        {
            error = CreditorReferenceError.TooLong;
            return false;
        }

        if (normalized.Length < Constants.HeaderLength + Constants.MinBodyLength)
        {
            error = CreditorReferenceError.TooShort;
            return false;
        }

        if (!AllAlphanumeric(normalized)
            || !IsDigit(normalized[Constants.PrefixLength])
            || !IsDigit(normalized[Constants.PrefixLength + 1]))
        {
            error = CreditorReferenceError.InvalidCharacter;
            return false;
        }

        var rearranged = string.Concat(
            normalized.AsSpan(Constants.HeaderLength),
            normalized.AsSpan(0, Constants.HeaderLength));
        if (Mod97(rearranged) != Constants.ValidRemainder)
        {
            error = CreditorReferenceError.BadCheckDigits;
            return false;
        }

        result = new CreditorReference(
            normalized.Substring(Constants.PrefixLength, Constants.CheckDigitsLength),
            normalized.Substring(Constants.HeaderLength));
        error = CreditorReferenceError.None;
        return true;
    }

    /// <summary>Generates the full creditor reference for a reference body.</summary>
    /// <param name="referenceBody">1 to 21 alphanumeric characters (spaces ignored, case-insensitive).</param>
    /// <returns>The reference, for example "RF18539007547034" for body "539007547034".</returns>
    /// <exception cref="ArgumentException">The body is empty, longer than 21 characters, or not alphanumeric.</exception>
    public static CreditorReference Create(string referenceBody)
    {
        ArgumentNullException.ThrowIfNull(referenceBody);
        var normalized = Normalize(referenceBody);

        if (normalized.Length < Constants.MinBodyLength)
        {
            throw new ArgumentException("The reference body must not be empty.", nameof(referenceBody));
        }

        if (normalized.Length > Constants.MaxBodyLength)
        {
            throw new ArgumentException(
                $"The reference body must be at most {Constants.MaxBodyLength} characters.", nameof(referenceBody));
        }

        if (!AllAlphanumeric(normalized))
        {
            throw new ArgumentException(
                "The reference body must contain only the characters A-Z and 0-9.", nameof(referenceBody));
        }

        var remainder = Mod97(string.Concat(normalized, Constants.Prefix, Constants.ZeroCheckDigits));
        var check = (Constants.CheckDigitComplement - remainder)
            .ToString(CultureInfo.InvariantCulture)
            .PadLeft(Constants.CheckDigitsLength, Constants.PadChar);
        return new CreditorReference(check, normalized);
    }

    /// <summary>Returns the reference in print format: groups of four characters separated by spaces.</summary>
    /// <returns>For example "RF18 5390 0754 7034".</returns>
    public string ToFormattedString()
    {
        var value = Value;
        var builder = new StringBuilder(value.Length + (value.Length / Constants.GroupSize));
        for (var i = 0; i < value.Length; i++)
        {
            if (i > 0 && i % Constants.GroupSize == 0)
            {
                builder.Append(Constants.GroupSeparator);
            }

            builder.Append(value[i]);
        }

        return builder.ToString();
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    public bool Equals(CreditorReference? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as CreditorReference);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    private static string Describe(CreditorReferenceError error) => error switch
    {
        CreditorReferenceError.NullOrEmpty => "The creditor reference must not be null or empty.",
        CreditorReferenceError.MissingPrefix => "A creditor reference must start with \"RF\".",
        CreditorReferenceError.TooShort => "A creditor reference needs at least one character after the check digits.",
        CreditorReferenceError.TooLong => $"A creditor reference is at most {Constants.MaxLength} characters.",
        CreditorReferenceError.InvalidCharacter => "A creditor reference contains only A-Z and 0-9, with digits for the check digits.",
        CreditorReferenceError.BadCheckDigits => "The check digits are not valid (ISO 7064 MOD 97-10).",
        _ => "Invalid creditor reference.",
    };

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Replace(Constants.GroupSeparator.ToString(), string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();
    }

    private static bool AllAlphanumeric(string value)
    {
        foreach (var c in value)
        {
            if (!IsDigit(c) && !IsUpperLetter(c))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    private static bool IsUpperLetter(char c) => c is >= 'A' and <= 'Z';

    private static int Mod97(string value)
    {
        var remainder = 0;
        foreach (var c in value)
        {
            remainder = IsDigit(c)
                ? ((remainder * Constants.DecimalRadix) + (c - '0')) % Constants.Mod97Divisor
                : ((remainder * Constants.LetterRadix) + (c - 'A' + Constants.LetterValueOffset)) % Constants.Mod97Divisor;
        }

        return remainder;
    }
}
