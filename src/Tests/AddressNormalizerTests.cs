using System.Linq;
using HOA.Export.Services;
using Xunit;

namespace Tests;

public class AddressNormalizerTests
{
    [Theory]
    [InlineData("241 Sills Ln", "241 Sills Lane")]
    [InlineData("241 Sills Lane", "241 Sills Lane")]
    [InlineData("241 sills ln", "241 Sills Lane")]
    [InlineData("241 SILLS LN", "241 Sills Lane")]
    [InlineData("241 Sills Ln.", "241 Sills Lane")]
    public void NormalizesAbbreviationsAndCase(string input, string expected)
    {
        Assert.Equal(expected, AddressNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("241 Sills Ln, Crofton, MD 21114", "241 Sills Lane")]
    [InlineData("241 Sills Ln, Crofton MD", "241 Sills Lane")]
    [InlineData("100 Main St, Annapolis, MD 21401", "100 Main Street")]
    [InlineData("293 North Caldwell Circe Downingtown, PA 19335", "293 North Caldwell Circe")]
    public void StripsCityStateZipAfterComma(string input, string expected)
    {
        Assert.Equal(expected, AddressNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("241 Sills Ln Crofton", "241 Sills Lane")]
    [InlineData("241 Sills Ln Gambrills", "241 Sills Lane")]
    [InlineData("100 Oak Dr Odenton", "100 Oak Drive")]
    [InlineData("11 Evans Court Downingtown", "11 Evans Court")]
    [InlineData("11 Evans Ct Downingtown", "11 Evans Court")]
    [InlineData("500 Cedar Dr Springfield", "500 Cedar Drive")]
    [InlineData("200 Oak Ave Anytown USA", "200 Oak Avenue")]
    public void StripsCityNameAfterStreetType(string input, string expected)
    {
        Assert.Equal(expected, AddressNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("241 Sills Ln 21114", "241 Sills Lane")]
    [InlineData("241 Sills Ln 21114-1234", "241 Sills Lane")]
    public void StripsTrailingZipCode(string input, string expected)
    {
        Assert.Equal(expected, AddressNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("100 Maple Blvd", "100 Maple Boulevard")]
    [InlineData("200 Oak Ave", "200 Oak Avenue")]
    [InlineData("300 Pine Rd", "300 Pine Road")]
    [InlineData("400 Elm Ct", "400 Elm Court")]
    [InlineData("500 Cedar Dr", "500 Cedar Drive")]
    [InlineData("600 Birch Cir", "600 Birch Circle")]
    [InlineData("700 Ash Pl", "700 Ash Place")]
    [InlineData("800 Walnut Ter", "800 Walnut Terrace")]
    [InlineData("900 Spruce Pkwy", "900 Spruce Parkway")]
    [InlineData("1000 Cherry Hwy", "1000 Cherry Highway")]
    [InlineData("1100 Willow Trl", "1100 Willow Trail")]
    public void ExpandsAllAbbreviations(string input, string expected)
    {
        Assert.Equal(expected, AddressNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("  241  Sills   Ln  ", "241 Sills Lane")]
    public void NormalizesWhitespace(string input, string expected)
    {
        Assert.Equal(expected, AddressNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void HandlesEmptyInput(string? input, string expected)
    {
        Assert.Equal(expected, AddressNormalizer.Normalize(input!));
    }

    [Fact]
    public void VariantsOfSameAddressNormalizeIdentically()
    {
        var variants = new[]
        {
            "241 Sills Ln",
            "241 Sills Lane",
            "241 sills ln",
            "241 SILLS LN",
            "241 Sills Ln.",
            "241 Sills Ln, Crofton, MD 21114",
            "  241  sills  ln  ",
        };

        var normalized = variants.Select(AddressNormalizer.Normalize).Distinct().ToList();
        Assert.Single(normalized);
        Assert.Equal("241 Sills Lane", normalized[0]);
    }
}
