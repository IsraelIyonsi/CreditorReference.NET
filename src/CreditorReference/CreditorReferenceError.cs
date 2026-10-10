namespace Iso11649;

/// <summary>The reason a creditor reference or reference body was rejected.</summary>
public enum CreditorReferenceError
{
    /// <summary>No error; the value is valid.</summary>
    None = 0,

    /// <summary>The value was null, empty or whitespace.</summary>
    NullOrEmpty,

    /// <summary>A full reference did not start with "RF".</summary>
    MissingPrefix,

    /// <summary>The value has no reference body after the "RF" and two check digits.</summary>
    TooShort,

    /// <summary>The value exceeds 25 characters.</summary>
    TooLong,

    /// <summary>A character other than A-Z or 0-9 was found, or the check digits are not digits.</summary>
    InvalidCharacter,

    /// <summary>The check digits do not satisfy ISO 7064 MOD 97-10.</summary>
    BadCheckDigits,
}
