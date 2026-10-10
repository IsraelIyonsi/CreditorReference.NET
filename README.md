# CreditorReference.Net

Zero-dependency validation and generation of the ISO 11649 "RF" Structured Creditor Reference used on invoices, SEPA credit transfers and EPC QR codes. Native AOT clean, MIT.

[![NuGet](https://img.shields.io/nuget/v/CreditorReference.Net.svg)](https://www.nuget.org/packages/CreditorReference.Net)

## Why

A creditor reference is `RF`, two check digits and up to 21 alphanumeric characters (25 max). The check is ISO 7064 MOD 97-10, the same family as IBAN and LEI, with the `RF` and check digits rotated to the end before the mod. Today .NET teams either hand-roll it or pull in a large finance monolith. This is the small, correct primitive.

## Install

```
dotnet add package CreditorReference.Net
```

## Use

```csharp
using Iso11649;

bool ok = CreditorReference.IsValid("RF18 5390 0754 7034");        // true, spaces and case ignored

var r = CreditorReference.Create("539007547034");                   // RF18539007547034
string print = r.ToFormattedString();                               // "RF18 5390 0754 7034"

CreditorReference.Validate("RF18539007547034");                     // throws FormatException if invalid
if (CreditorReference.TryParse("RF45G72UUR", out var parsed)) { /* parsed.Body == "G72UUR" */ }
if (!CreditorReference.TryValidate("RF19539007547034", out var error)) { /* error == BadCheckDigits */ }
```

The namespace is `Iso11649` and the class is `CreditorReference`, so consumers can name their own namespace `CreditorReference.*` without breaking name lookup.

## Algorithm

1. Strip spaces, upper-case.
2. Move the first four characters (`RF` + check digits) to the end.
3. Map letters A..Z to 10..35 and fold the result digit by digit into a mod-97 remainder (no `BigInteger`).
4. Valid when the remainder is 1.

`Create(body)` computes `98 - mod97(body + "RF00")`, zero-padded to two digits. Computed check digits are always 02 to 98, so `RF00` and `RF01` references can never validate.

This library accepts only spaces as separators and rejects other punctuation, whereas python-stdnum's `compact()` also strips `-`, `.`, `,`, `/` and `:`; this is a deliberate stricter choice.

## Related

Pairs with EPC QR (the SEPA QR "structured remittance" field), SEPA Pain.001 `CdtrRefInf` and Camt statements, which all carry ISO 11649 references.

Reference implementation and test vectors: [python-stdnum `iso11649`](https://arthurdejong.org/python-stdnum/doc/2.2/stdnum.iso11649) and the [Creditor Reference](https://en.wikipedia.org/wiki/Creditor_Reference) article.

## License

MIT
