namespace WarehousePOS.Infrastructure.Printing;

/// <summary>
/// ESC/P2 hardware printer control byte commands for Epson LQ-310 24-pin dot-matrix printer.
/// </summary>
public static class EscP2Commands
{
    /// <summary>ESC @: Resets printer hardware to defaults.</summary>
    public static readonly byte[] Initialize = [0x1B, 0x40];

    /// <summary>ESC x 0: High-speed Draft font mode (347 characters per second).</summary>
    public static readonly byte[] SelectDraft = [0x1B, 0x78, 0x00];

    /// <summary>ESC x 1: Near Letter Quality (NLQ) font mode.</summary>
    public static readonly byte[] SelectNlq = [0x1B, 0x78, 0x01];

    /// <summary>ESC k 0: Epson Roman font.</summary>
    public static readonly byte[] FontRoman = [0x1B, 0x6B, 0x00];

    /// <summary>ESC k 1: Epson Sans Serif font.</summary>
    public static readonly byte[] FontSansSerif = [0x1B, 0x6B, 0x01];

    /// <summary>ESC E: Emphasized (Bold) printing ON.</summary>
    public static readonly byte[] BoldOn = [0x1B, 0x45];

    /// <summary>ESC F: Emphasized (Bold) printing OFF.</summary>
    public static readonly byte[] BoldOff = [0x1B, 0x46];

    /// <summary>ESC W 1: Double-width characters ON.</summary>
    public static readonly byte[] DoubleWidthOn = [0x1B, 0x57, 0x01];

    /// <summary>ESC W 0: Double-width characters OFF.</summary>
    public static readonly byte[] DoubleWidthOff = [0x1B, 0x57, 0x00];

    /// <summary>SI (0x0F): Condensed mode ON (17 CPI, fits 136 columns on 80-col paper).</summary>
    public static readonly byte[] CondensedOn = [0x0F];

    /// <summary>DC2 (0x12): Condensed mode OFF (return to 10 CPI standard 80 columns).</summary>
    public static readonly byte[] CondensedOff = [0x12];

    /// <summary>ESC 2: Standard 1/6 inch line spacing (6 lines per inch).</summary>
    public static readonly byte[] LineSpacing1_6 = [0x1B, 0x32];

    /// <summary>ESC 0: Compact 1/8 inch line spacing (8 lines per inch).</summary>
    public static readonly byte[] LineSpacing1_8 = [0x1B, 0x30];

    /// <summary>FF (0x0C): Form Feed - advances continuous tractor paper to next page perforation.</summary>
    public static readonly byte[] FormFeed = [0x0C];

    /// <summary>
    /// ESC C n: Sets form length in lines (e.g. 33 lines for 5.5-inch continuous half-sheet at 6 LPI).
    /// </summary>
    public static byte[] SetPageLengthInLines(byte lines) => [0x1B, 0x43, lines];
}
