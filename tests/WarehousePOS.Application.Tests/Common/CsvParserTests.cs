using System.Text;
using FluentAssertions;
using WarehousePOS.Application.Common;
using Xunit;

namespace WarehousePOS.Application.Tests.Common;

public sealed class CsvParserTests
{
    [Fact]
    public void Parse_SimpleCsv_ReturnsCorrectRecords()
    {
        string csv = "Name,SKU,Category\nMattress 72x36,M7236,Mattress\nOffice Chair,OC01,Office Chair";
        using var reader = new StringReader(csv);

        var result = CsvParser.Parse(reader);

        result.Should().HaveCount(3);
        result[0].Should().Equal("Name", "SKU", "Category");
        result[1].Should().Equal("Mattress 72x36", "M7236", "Mattress");
        result[2].Should().Equal("Office Chair", "OC01", "Office Chair");
    }

    [Fact]
    public void Parse_QuotedFieldsWithCommas_PreservesCommas()
    {
        string csv = "Name,Description\n\"Chair, Deluxe\",\"Ergonomic, high back\"\nTable,Simple";
        using var reader = new StringReader(csv);

        var result = CsvParser.Parse(reader);

        result.Should().HaveCount(3);
        result[1][0].Should().Be("Chair, Deluxe");
        result[1][1].Should().Be("Ergonomic, high back");
        result[2][0].Should().Be("Table");
    }

    [Fact]
    public void Parse_EscapedQuotes_HandlesCorrectly()
    {
        string csv = "Name,Notes\n\"Chair \"\"Executive\"\" Edition\",Standard";
        using var reader = new StringReader(csv);

        var result = CsvParser.Parse(reader);

        result.Should().HaveCount(2);
        result[1][0].Should().Be("Chair \"Executive\" Edition");
    }

    [Fact]
    public void Parse_WithBom_StripsBomCorrectly()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, (byte)'A', (byte)',', (byte)'B', (byte)'\n', (byte)'1', (byte)',', (byte)'2'];
        using var stream = new MemoryStream(bytes);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var result = CsvParser.Parse(reader);

        result.Should().HaveCount(2);
        result[0][0].Should().Be("A");
        result[0][1].Should().Be("B");
    }

    [Fact]
    public void Parse_EmptyLines_IgnoresBlankLines()
    {
        string csv = "Col1,Col2\n\n\nVal1,Val2\n   \n";
        using var reader = new StringReader(csv);

        var result = CsvParser.Parse(reader);

        result.Should().HaveCount(2);
        result[1][0].Should().Be("Val1");
    }
}
