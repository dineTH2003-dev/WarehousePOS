using System.Text;

namespace WarehousePOS.Application.Common;

/// <summary>
/// High-performance RFC 4180 compliant CSV parser implemented in pure C#.
/// Supports commas, quoted fields, escaped quotes (""), newlines within fields,
/// UTF-8 BOM stripping, and empty line handling with zero external dependencies.
/// </summary>
public static class CsvParser
{
    public static List<List<string>> Parse(TextReader reader)
    {
        var records = new List<List<string>>();
        var currentRecord = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        int nextChar;
        bool isFirstChar = true;

        while ((nextChar = reader.Read()) != -1)
        {
            char c = (char)nextChar;

            // Strip UTF-8 Byte Order Mark (BOM) if present at the start
            if (isFirstChar)
            {
                isFirstChar = false;
                if (c == '\uFEFF')
                    continue;
            }

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check if escaped quote ("")
                    int peek = reader.Peek();
                    if (peek != -1 && (char)peek == '"')
                    {
                        reader.Read(); // Consume the second quote
                        currentField.Append('"');
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    currentRecord.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else if (c == '\r')
                {
                    // Consume following \n if CRLF
                    if (reader.Peek() != -1 && (char)reader.Peek() == '\n')
                    {
                        reader.Read();
                    }
                    EndRecord(records, currentRecord, currentField);
                }
                else if (c == '\n')
                {
                    EndRecord(records, currentRecord, currentField);
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }

        // Handle end of file
        if (currentField.Length > 0 || currentRecord.Count > 0)
        {
            currentRecord.Add(currentField.ToString().Trim());
            if (currentRecord.Any(field => !string.IsNullOrWhiteSpace(field)))
            {
                records.Add(currentRecord);
            }
        }

        return records;
    }

    private static void EndRecord(List<List<string>> records, List<string> currentRecord, StringBuilder currentField)
    {
        currentRecord.Add(currentField.ToString().Trim());
        currentField.Clear();

        // Only add non-empty records (ignore blank empty lines)
        if (currentRecord.Any(field => !string.IsNullOrWhiteSpace(field)))
        {
            records.Add(new List<string>(currentRecord));
        }
        currentRecord.Clear();
    }
}
