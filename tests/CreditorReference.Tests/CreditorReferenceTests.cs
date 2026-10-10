using Iso11649;

namespace CreditorReference.Tests;

public class CreditorReferenceTests
{
    public static TheoryData<string, string, string> Worked => new()
    {
        { "539007547034", "18", "RF18539007547034" },
        { "000000000539007547034", "18", "RF18000000000539007547034" },
        { "G72UUR", "45", "RF45G72UUR" },
        { "35UIR2OGZUPQXUAA", "50", "RF5035UIR2OGZUPQXUAA" },
        { "1", "74", "RF741" },
        { "ZZZZZZZZZZZZZZZZZZZZZ", "09", "RF09ZZZZZZZZZZZZZZZZZZZZZ" },
        { "INV2024", "30", "RF30INV2024" },
        { "12345678901234567890A", "73", "RF7312345678901234567890A" },
    };

    [Theory]
    [MemberData(nameof(Worked))]
    public void Create_ProducesExactCheckDigitsAndValue(string body, string check, string full)
    {
        var reference = Iso11649.CreditorReference.Create(body);
        Assert.Equal(check, reference.CheckDigits);
        Assert.Equal(body, reference.Body);
        Assert.Equal(full, reference.Value);
        Assert.Equal(full, reference.ToString());
    }

    [Theory]
    [MemberData(nameof(Worked))]
    public void Create_RoundTripsThroughValidate(string body, string _, string full)
    {
        var created = Iso11649.CreditorReference.Create(body);
        Assert.True(Iso11649.CreditorReference.IsValid(created.Value));
        Assert.True(Iso11649.CreditorReference.IsValid(full));
        Assert.Equal(created, Iso11649.CreditorReference.Parse(full));
    }

    [Theory]
    [InlineData("RF18 5390 0754 7034")]
    [InlineData("rf18 5390 0754 7034")]
    [InlineData("  RF18539007547034  ")]
    [InlineData("RF45 G72U UR")]
    [InlineData("rf45g72uur")]
    public void Parse_AcceptsSpacedAndMixedCase(string input)
    {
        Assert.True(Iso11649.CreditorReference.IsValid(input));
        Iso11649.CreditorReference.Validate(input);
    }

    [Fact]
    public void Parse_ExposesParts()
    {
        var r = Iso11649.CreditorReference.Parse("RF18 5390 0754 7034");
        Assert.Equal("18", r.CheckDigits);
        Assert.Equal("539007547034", r.Body);
    }

    [Theory]
    [InlineData("RF18 5390 0754 7034", "RF18 5390 0754 7034")]
    [InlineData("RF45G72UUR", "RF45 G72U UR")]
    [InlineData("RF18000000000539007547034", "RF18 0000 0000 0539 0075 4703 4")]
    public void ToFormattedString_GroupsByFour(string input, string expected) =>
        Assert.Equal(expected, Iso11649.CreditorReference.Parse(input).ToFormattedString());

    [Theory]
    [InlineData(null, CreditorReferenceError.NullOrEmpty)]
    [InlineData("", CreditorReferenceError.NullOrEmpty)]
    [InlineData("   ", CreditorReferenceError.NullOrEmpty)]
    [InlineData("XX18539007547034", CreditorReferenceError.MissingPrefix)]
    [InlineData("18539007547034", CreditorReferenceError.MissingPrefix)]
    [InlineData("RF18", CreditorReferenceError.TooShort)]
    [InlineData("RF1", CreditorReferenceError.TooShort)]
    [InlineData("RF180000000000539007547034", CreditorReferenceError.TooLong)]
    [InlineData("RF18-5390-0754-7034", CreditorReferenceError.InvalidCharacter)]
    [InlineData("RF18539007547034!", CreditorReferenceError.InvalidCharacter)]
    [InlineData("RFAB539007547034", CreditorReferenceError.InvalidCharacter)]
    [InlineData("RF19539007547034", CreditorReferenceError.BadCheckDigits)]
    [InlineData("RF18539007547035", CreditorReferenceError.BadCheckDigits)]
    [InlineData("RF00539007547034", CreditorReferenceError.BadCheckDigits)]
    [InlineData("RF01539007547034", CreditorReferenceError.BadCheckDigits)]
    [InlineData("RF46G72UUR", CreditorReferenceError.BadCheckDigits)]
    public void Invalid_ReportsExactError(string? input, CreditorReferenceError expected)
    {
        Assert.False(Iso11649.CreditorReference.IsValid(input));
        Assert.False(Iso11649.CreditorReference.TryValidate(input, out var error));
        Assert.Equal(expected, error);
        Assert.False(Iso11649.CreditorReference.TryParse(input, out var parsed));
        Assert.Null(parsed);
        Assert.Throws<FormatException>(() => Iso11649.CreditorReference.Validate(input));
        Assert.Throws<FormatException>(() => Iso11649.CreditorReference.Parse(input));
    }

    [Fact]
    public void WrongRearrangementOrder_WouldNotBeAccepted()
    {
        Assert.False(Iso11649.CreditorReference.IsValid("RF18G72UUR"));
        Assert.True(Iso11649.CreditorReference.IsValid("RF45G72UUR"));
    }

    [Fact]
    public void SingleCharacterMutation_IsAlwaysDetected()
    {
        const string valid = "RF18539007547034";
        const string digits = "0123456789";
        for (var i = 2; i < valid.Length; i++)
        {
            foreach (var d in digits)
            {
                if (d == valid[i])
                {
                    continue;
                }

                var mutated = valid.Remove(i, 1).Insert(i, d.ToString());
                Assert.False(Iso11649.CreditorReference.IsValid(mutated), mutated);
            }
        }
    }

    [Fact]
    public void AdjacentTransposition_IsDetectedForDistinctDigits()
    {
        Assert.False(Iso11649.CreditorReference.IsValid("RF18539007547304"));
    }

    [Fact]
    public void ComputedCheckDigits_AreNeverZeroOrOne()
    {
        for (var n = 0; n < 2000; n++)
        {
            var check = Iso11649.CreditorReference.Create(n.ToString(System.Globalization.CultureInfo.InvariantCulture)).CheckDigits;
            Assert.NotEqual("00", check);
            Assert.NotEqual("01", check);
            Assert.InRange(int.Parse(check, System.Globalization.CultureInfo.InvariantCulture), 2, 98);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567890123456789012")]
    [InlineData("ABC-123")]
    [InlineData("ABC_123")]
    [InlineData("ABC.123")]
    public void Create_RejectsBadBodies(string body) =>
        Assert.Throws<ArgumentException>(() => Iso11649.CreditorReference.Create(body));

    [Fact]
    public void Create_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => Iso11649.CreditorReference.Create(null!));

    [Fact]
    public void Create_AcceptsLowerCaseAndSpaces()
    {
        Assert.Equal("RF45G72UUR", Iso11649.CreditorReference.Create("g72u ur").Value);
    }

    [Fact]
    public void Create_MaxBody_IsExactly25Characters()
    {
        var r = Iso11649.CreditorReference.Create("ZZZZZZZZZZZZZZZZZZZZZ");
        Assert.Equal(25, r.Value.Length);
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var a = Iso11649.CreditorReference.Parse("RF18 5390 0754 7034");
        var b = Iso11649.CreditorReference.Create("539007547034");
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.False(a.Equals(null));
    }
}
