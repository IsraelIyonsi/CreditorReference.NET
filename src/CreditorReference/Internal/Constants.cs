namespace Iso11649.Internal;

internal static class Constants
{
    public const string Prefix = "RF";
    public const int PrefixLength = 2;
    public const int CheckDigitsLength = 2;
    public const int HeaderLength = PrefixLength + CheckDigitsLength;
    public const int MaxBodyLength = 21;
    public const int MaxLength = HeaderLength + MaxBodyLength;
    public const int MinBodyLength = 1;
    public const int GroupSize = 4;

    public const string ZeroCheckDigits = "00";
    public const char GroupSeparator = ' ';
    public const char PadChar = '0';

    public const int Mod97Divisor = 97;
    public const int ValidRemainder = 1;
    public const int CheckDigitComplement = 98;

    public const int DecimalRadix = 10;
    public const int LetterRadix = 100;
    public const int LetterValueOffset = 10;
}
