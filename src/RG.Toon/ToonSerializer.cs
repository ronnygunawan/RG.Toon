using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace RG.Toon;

/// <summary>
/// Provides methods for serializing objects to TOON (Token-Oriented Object Notation) format
/// and deserializing TOON back to objects.
/// </summary>
public static partial class ToonSerializer
{
    private const int DefaultIndentSize = 2;

    // Pattern for checking if a string is numeric-like
    private static readonly Regex NumericPattern = NumericPatternRegex();

    // Pattern for checking if a string has leading zeros
    private static readonly Regex LeadingZeroPattern = LeadingZeroPatternRegex();

    // Pattern for unquoted keys
    private static readonly Regex UnquotedKeyPattern = UnquotedKeyPatternRegex();

    private static void ValidateIndentSize(int indentSize)
    {
        if (indentSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(indentSize), indentSize, "Indent size must be greater than zero.");
        }
    }

    /// <summary>
    /// Serializes the specified object to a TOON string.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <param name="indentSize">The number of spaces to use for indentation.</param>
    /// <returns>A TOON formatted string representation of the object.</returns>
    public static string Serialize<T>(T value, int indentSize = DefaultIndentSize)
    {
        ValidateIndentSize(indentSize);
        return Serialize((object?)value, indentSize);
    }

    /// <summary>
    /// Serializes the specified object to a TOON string.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="indentSize">The number of spaces to use for indentation.</param>
    /// <returns>A TOON formatted string representation of the object.</returns>
    public static string Serialize(object? value, int indentSize = DefaultIndentSize)
    {
        ValidateIndentSize(indentSize);

        if (value is null)
        {
            return "null";
        }

        var sb = new StringBuilder();
        SerializeValue(sb, value, 0, indentSize, isRootArray: IsArrayLike(value));
        return sb.ToString().TrimEnd('\n');
    }

    /// <summary>
    /// Deserializes the TOON string to an object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
    /// <param name="toon">The TOON string to deserialize.</param>
    /// <param name="indentSize">The number of spaces to use for indentation.</param>
    /// <returns>The deserialized object.</returns>
    public static T? Deserialize<T>(string toon, int indentSize = DefaultIndentSize)
    {
        ValidateIndentSize(indentSize);
        return (T?)Deserialize(toon, typeof(T), indentSize);
    }

    /// <summary>
    /// Deserializes the TOON string to an object of the specified type.
    /// </summary>
    /// <param name="toon">The TOON string to deserialize.</param>
    /// <param name="type">The type of the object to deserialize to.</param>
    /// <param name="indentSize">The number of spaces to use for indentation.</param>
    /// <returns>The deserialized object.</returns>
    public static object? Deserialize(string toon, Type type, int indentSize = DefaultIndentSize)
    {
        ValidateIndentSize(indentSize);

        if (string.IsNullOrEmpty(toon))
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        var lines = toon.Split('\n');
        var context = new ParseContext(lines, indentSize);

        return ParseValue(context, type, 0);
    }

    #region Serialization

    private static void SerializeValue(StringBuilder sb, object? value, int depth, int indentSize, bool isRootArray = false)
    {
        if (value is null)
        {
            sb.Append("null");
            return;
        }

        var type = value.GetType();

        // Handle primitives
        if (IsPrimitive(type))
        {
            sb.Append(EncodePrimitive(value));
            return;
        }

        // Handle strings
        if (value is string str)
        {
            sb.Append(EncodeString(str, ','));
            return;
        }

        // Handle arrays and collections
        if (IsArrayLike(value))
        {
            SerializeArray(sb, (IEnumerable)value, depth, indentSize, null, isRootArray);
            return;
        }

        // Handle objects
        SerializeObject(sb, value, depth, indentSize);
    }

    private static void SerializeObject(StringBuilder sb, object obj, int depth, int indentSize)
    {
        var type = obj.GetType();
        var properties = GetSerializableProperties(type);
        var indent = new string(' ', depth * indentSize);

        bool first = true;
        foreach (var prop in properties)
        {
            var propValue = prop.GetValue(obj);
            var propName = GetPropertyName(prop);
            var encodedKey = EncodeKey(propName);

            if (!first)
            {
                sb.Append('\n');
            }
            first = false;

            if (propValue is null)
            {
                sb.Append(indent);
                sb.Append(encodedKey);
                sb.Append(": null");
            }
            else if (IsPrimitive(propValue.GetType()))
            {
                sb.Append(indent);
                sb.Append(encodedKey);
                sb.Append(": ");
                sb.Append(EncodePrimitive(propValue));
            }
            else if (propValue is string strValue)
            {
                sb.Append(indent);
                sb.Append(encodedKey);
                sb.Append(": ");
                sb.Append(EncodeString(strValue, ','));
            }
            else if (IsArrayLike(propValue))
            {
                SerializeArray(sb, (IEnumerable)propValue, depth, indentSize, encodedKey, false);
            }
            else
            {
                sb.Append(indent);
                sb.Append(encodedKey);
                sb.Append(':');
                sb.Append('\n');
                SerializeObject(sb, propValue, depth + 1, indentSize);
            }
        }
    }

    private static void SerializeArray(StringBuilder sb, IEnumerable items, int depth, int indentSize, string? key, bool isRootArray)
    {
        var list = items.Cast<object?>().ToList();
        var count = list.Count;
        var indent = new string(' ', depth * indentSize);
        var innerIndent = new string(' ', (depth + 1) * indentSize);

        // Check if this is a tabular array (all objects with same keys and primitive values)
        if (IsTabularArray(list, out var fields))
        {
            // Tabular format
            if (key is not null)
            {
                sb.Append(indent);
                sb.Append(key);
            }
            sb.Append('[');
            sb.Append(count);
            sb.Append("]{");
            sb.Append(string.Join(",", fields.Select(EncodeKey)));
            sb.Append("}:");
            sb.Append('\n');

            foreach (var item in list)
            {
                sb.Append(innerIndent);
                SerializeTabularRow(sb, item!, fields);
                sb.Append('\n');
            }
        }
        else if (AllPrimitives(list))
        {
            // Inline primitive array
            if (key is not null)
            {
                sb.Append(indent);
                sb.Append(key);
            }
            sb.Append('[');
            sb.Append(count);
            sb.Append("]:");
            if (count > 0)
            {
                sb.Append(' ');
                sb.Append(string.Join(",", list.Select(v => EncodeArrayValue(v, ','))));
            }
        }
        else
        {
            // Mixed/non-uniform array - use expanded list format
            if (key is not null)
            {
                sb.Append(indent);
                sb.Append(key);
            }
            sb.Append('[');
            sb.Append(count);
            sb.Append("]:");
            sb.Append('\n');

            foreach (var item in list)
            {
                sb.Append(innerIndent);
                sb.Append("- ");
                if (item is null)
                {
                    sb.Append("null");
                }
                else if (IsPrimitive(item.GetType()))
                {
                    sb.Append(EncodePrimitive(item));
                }
                else if (item is string strItem)
                {
                    sb.Append(EncodeString(strItem, ','));
                }
                else if (IsArrayLike(item))
                {
                    // Nested array as list item
                    var nestedList = ((IEnumerable)item).Cast<object?>().ToList();
                    sb.Append('[');
                    sb.Append(nestedList.Count);
                    sb.Append("]: ");
                    sb.Append(string.Join(",", nestedList.Select(v => EncodeArrayValue(v, ','))));
                }
                else
                {
                    // Object as list item - emit first field on hyphen line
                    SerializeObjectAsListItem(sb, item, depth + 1, indentSize);
                }
                sb.Append('\n');
            }
        }
    }

    private static void SerializeObjectAsListItem(StringBuilder sb, object obj, int depth, int indentSize)
    {
        var type = obj.GetType();
        var properties = GetSerializableProperties(type).ToList();
        var innerIndent = new string(' ', (depth + 1) * indentSize);

        if (properties.Count == 0)
        {
            // Empty object
            return;
        }

        // First property on hyphen line
        var firstProp = properties[0];
        var firstPropValue = firstProp.GetValue(obj);
        var firstPropName = GetPropertyName(firstProp);
        var encodedKey = EncodeKey(firstPropName);

        if (firstPropValue is null)
        {
            sb.Append(encodedKey);
            sb.Append(": null");
        }
        else if (IsPrimitive(firstPropValue.GetType()))
        {
            sb.Append(encodedKey);
            sb.Append(": ");
            sb.Append(EncodePrimitive(firstPropValue));
        }
        else if (firstPropValue is string strValue)
        {
            sb.Append(encodedKey);
            sb.Append(": ");
            sb.Append(EncodeString(strValue, ','));
        }
        else if (IsArrayLike(firstPropValue))
        {
            // Inline array on hyphen line
            var list = ((IEnumerable)firstPropValue).Cast<object?>().ToList();
            sb.Append(encodedKey);
            sb.Append('[');
            sb.Append(list.Count);
            sb.Append("]: ");
            sb.Append(string.Join(",", list.Select(v => EncodeArrayValue(v, ','))));
        }
        else
        {
            // Nested object
            sb.Append(encodedKey);
            sb.Append(':');
            sb.Append('\n');
            var doubleIndent = new string(' ', (depth + 2) * indentSize);
            var nestedSb = new StringBuilder();
            SerializeObject(nestedSb, firstPropValue, 0, indentSize);
            foreach (var line in nestedSb.ToString().Split('\n'))
            {
                sb.Append(doubleIndent);
                sb.Append(line);
                sb.Append('\n');
            }
            sb.Length--; // Remove trailing newline
        }

        // Remaining properties at depth+1
        for (int i = 1; i < properties.Count; i++)
        {
            var prop = properties[i];
            var propValue = prop.GetValue(obj);
            var propName = GetPropertyName(prop);
            var propEncodedKey = EncodeKey(propName);

            sb.Append('\n');
            sb.Append(innerIndent);

            if (propValue is null)
            {
                sb.Append(propEncodedKey);
                sb.Append(": null");
            }
            else if (IsPrimitive(propValue.GetType()))
            {
                sb.Append(propEncodedKey);
                sb.Append(": ");
                sb.Append(EncodePrimitive(propValue));
            }
            else if (propValue is string strValue2)
            {
                sb.Append(propEncodedKey);
                sb.Append(": ");
                sb.Append(EncodeString(strValue2, ','));
            }
            else if (IsArrayLike(propValue))
            {
                var list = ((IEnumerable)propValue).Cast<object?>().ToList();
                sb.Append(propEncodedKey);
                sb.Append('[');
                sb.Append(list.Count);
                sb.Append("]: ");
                sb.Append(string.Join(",", list.Select(v => EncodeArrayValue(v, ','))));
            }
            else
            {
                sb.Append(propEncodedKey);
                sb.Append(':');
                sb.Append('\n');
                var doubleIndent = new string(' ', (depth + 2) * indentSize);
                var nestedSb = new StringBuilder();
                SerializeObject(nestedSb, propValue, 0, indentSize);
                foreach (var line in nestedSb.ToString().Split('\n'))
                {
                    sb.Append(doubleIndent);
                    sb.Append(line);
                    sb.Append('\n');
                }
                sb.Length--; // Remove trailing newline
            }
        }
    }

    private static void SerializeTabularRow(StringBuilder sb, object obj, List<string> fields)
    {
        var type = obj.GetType();
        var propsByName = GetSerializableProperties(type)
            .ToDictionary(p => GetPropertyName(p), p => p);

        var values = new List<string>();
        foreach (var field in fields)
        {
            if (propsByName.TryGetValue(field, out var prop))
            {
                var value = prop.GetValue(obj);
                values.Add(EncodeArrayValue(value, ','));
            }
            else
            {
                values.Add("null");
            }
        }
        sb.Append(string.Join(",", values));
    }

    #endregion

    #region Encoding Helpers

    private static string EncodePrimitive(object value)
    {
        return value switch
        {
            bool b => b ? "true" : "false",
            byte n => n.ToString(CultureInfo.InvariantCulture),
            sbyte n => n.ToString(CultureInfo.InvariantCulture),
            short n => n.ToString(CultureInfo.InvariantCulture),
            ushort n => n.ToString(CultureInfo.InvariantCulture),
            int n => n.ToString(CultureInfo.InvariantCulture),
            uint n => n.ToString(CultureInfo.InvariantCulture),
            long n => n.ToString(CultureInfo.InvariantCulture),
            ulong n => n.ToString(CultureInfo.InvariantCulture),
            float f => FormatFloat(f),
            double d => FormatDouble(d),
            decimal m => FormatDecimal(m),
            char c => EncodeString(c.ToString(), ','),
            _ => value.ToString() ?? "null"
        };
    }

    private static string FormatFloat(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return "null";
        }
        // Normalize -0 to 0 using bit comparison to avoid floating point equality issues
        if (BitConverter.SingleToInt32Bits(value) is 0 or unchecked((int)0x80000000))
        {
            return "0";
        }
        var result = value.ToString("G9", CultureInfo.InvariantCulture);
        return NormalizeNumber(result);
    }

    private static string FormatDouble(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return "null";
        }
        // Normalize -0 to 0 using bit comparison to avoid floating point equality issues
        if (BitConverter.DoubleToInt64Bits(value) is 0L or unchecked((long)0x8000000000000000))
        {
            return "0";
        }
        // Use R (round-trip) format first, then normalize
        var result = value.ToString("R", CultureInfo.InvariantCulture);
        return NormalizeNumber(result);
    }

    private static string FormatDecimal(decimal value)
    {
        // Normalize -0 to 0
        if (value == 0m)
        {
            return "0";
        }
        var result = value.ToString(CultureInfo.InvariantCulture);
        return NormalizeNumber(result);
    }

    private static string NormalizeNumber(string number)
    {
        // Remove exponent notation
        if (number.Contains('E', StringComparison.OrdinalIgnoreCase) &&
            double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
        {
            // Format without exponent
            number = d.ToString("0." + new string('#', 350), CultureInfo.InvariantCulture);
        }

        // Remove trailing zeros in fractional part
        if (number.Contains('.'))
        {
            number = number.TrimEnd('0').TrimEnd('.');
        }

        return number;
    }

    private static string EncodeString(string value, char activeDelimiter)
    {
        if (RequiresQuoting(value, activeDelimiter))
        {
            return "\"" + EscapeString(value) + "\"";
        }
        return value;
    }

    private static string EncodeArrayValue(object? value, char activeDelimiter)
    {
        if (value is null)
        {
            return "null";
        }
        if (IsPrimitive(value.GetType()))
        {
            return EncodePrimitive(value);
        }
        if (value is string str)
        {
            return EncodeString(str, activeDelimiter);
        }
        return value.ToString() ?? "null";
    }

    private static string EncodeKey(string key)
    {
        if (UnquotedKeyPattern.IsMatch(key))
        {
            return key;
        }
        return "\"" + EscapeString(key) + "\"";
    }

    private static bool RequiresQuoting(string value, char activeDelimiter)
    {
        // Empty string
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        // Leading or trailing whitespace
        if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]))
        {
            return true;
        }

        // Reserved literals
        if (value is "true" or "false" or "null")
        {
            return true;
        }

        // Numeric-like
        if (NumericPattern.IsMatch(value) || LeadingZeroPattern.IsMatch(value))
        {
            return true;
        }

        // Contains special characters
        if (value.Contains(':') || value.Contains('"') || value.Contains('\\') ||
            value.Contains('[') || value.Contains(']') || value.Contains('{') || value.Contains('}'))
        {
            return true;
        }

        // Contains control characters
        if (value.Contains('\n') || value.Contains('\r') || value.Contains('\t'))
        {
            return true;
        }

        // Contains active delimiter
        if (value.Contains(activeDelimiter))
        {
            return true;
        }

        // Equals "-" or starts with "-"
        if (value == "-" || value.StartsWith('-'))
        {
            return true;
        }

        return false;
    }

    private static string EscapeString(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            sb.Append(c switch
            {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ => c
            });
        }
        return sb.ToString();
    }

    #endregion

    #region Deserialization

    private static object? ParseValue(ParseContext context, Type type, int depth)
    {
        if (!context.HasMoreLines())
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        var firstLine = context.PeekLine().Trim();
        
        // Check if it's a primitive type being deserialized directly
        if (IsPrimitiveType(type) || type == typeof(string) || Nullable.GetUnderlyingType(type) is not null)
        {
            // If there's only one line and no colon (not a key-value), treat as primitive
            if (!firstLine.Contains(':') || (firstLine.StartsWith('"') && !firstLine.Substring(1).Contains(':')))
            {
                context.ReadLine();
                return ParsePrimitiveToken(firstLine, type);
            }
        }

        // Check for root array or array type
        if ((depth == 0 && firstLine.StartsWith('[')) ||
            type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
        {
            return ParseRootArray(context, type);
        }

        // Parse as object
        return ParseObject(context, type, depth);
    }

    private static bool IsPrimitiveType(Type type)
    {
        return type.IsPrimitive || type == typeof(decimal) || type == typeof(string);
    }

    private static object? ParseRootArray(ParseContext context, Type type)
    {
        var line = context.ReadLine().TrimStart();

        // Parse header: [N]: or [N|]: or [N\t]: or [N]{fields}:
        var bracketStart = line.IndexOf('[');
        var bracketEnd = line.IndexOf(']');

        if (bracketStart < 0 || bracketEnd < 0)
        {
            throw new FormatException("Invalid array header");
        }

        var headerContent = line[(bracketStart + 1)..bracketEnd];
        
        // Extract delimiter and count
        char delimiter = ','; // default delimiter
        string countStr;
        
        if (headerContent.EndsWith('|'))
        {
            delimiter = '|';
            countStr = headerContent[..^1];
        }
        else if (headerContent.EndsWith('\t'))
        {
            delimiter = '\t';
            countStr = headerContent[..^1];
        }
        else
        {
            countStr = headerContent;
        }
        
        if (!int.TryParse(countStr, out var count))
        {
            throw new FormatException($"Invalid array count: {countStr}");
        }

        // Check for field list
        List<string>? fields = null;
        var braceStart = line.IndexOf('{', bracketEnd);
        var braceEnd = line.IndexOf('}', bracketEnd);
        if (braceStart > 0 && braceEnd > braceStart)
        {
            var fieldsStr = line[(braceStart + 1)..braceEnd];
            
            // Strict-mode: validate delimiter consistency between header and fields
            char fieldsDelimiter = DetectDelimiter(fieldsStr);
            if (fieldsDelimiter != delimiter && fieldsDelimiter != ',')
            {
                throw new FormatException($"Delimiter mismatch: header declares '{delimiter}' but fields use '{fieldsDelimiter}'");
            }
            
            fields = ParseDelimitedFields(fieldsStr, delimiter);
        }

        // Find the colon
        var colonPos = braceEnd > 0 ? line.IndexOf(':', braceEnd) : line.IndexOf(':', bracketEnd);
        if (colonPos < 0)
        {
            throw new FormatException("Missing colon in array header");
        }

        // Get element type
        Type elementType;
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
        }
        else if (type.IsGenericType)
        {
            elementType = type.GetGenericArguments()[0];
        }
        else
        {
            throw new FormatException($"Cannot determine element type for {type}");
        }

        // Check for inline values
        var remainder = line[(colonPos + 1)..].Trim();
        if (!string.IsNullOrEmpty(remainder))
        {
            // Inline primitive array
            var values = ParseDelimitedValues(remainder, delimiter);
            
            // Strict-mode: validate count matches
            if (values.Count != count)
            {
                throw new FormatException($"Array count mismatch: declared {count}, provided {values.Count}");
            }
            
            return ConvertToArray(values, elementType, type);
        }

        // Read items at next depth
        if (fields is not null)
        {
            // Tabular array
            var items = new List<object?>();
            for (int i = 0; i < count && context.HasMoreLines(); i++)
            {
                var rowLine = context.PeekLine();
                var rowDepth = GetDepth(rowLine, context.IndentSize);
                if (rowDepth <= 0)
                {
                    break;
                }
                
                // Check if this line is a key-value pair (colon before delimiter)
                // If so, stop reading tabular rows
                var trimmed = rowLine.Trim();
                var rowColonPos = FindUnquotedColon(trimmed);
                var rowDelimiterPos = trimmed.IndexOf(delimiter);
                if (rowColonPos >= 0 && (rowDelimiterPos < 0 || rowColonPos < rowDelimiterPos))
                {
                    // This is a key-value pair, not a table row
                    break;
                }

                context.ReadLine();
                var rowValues = ParseDelimitedValues(rowLine.Trim(), delimiter);
                
                // Strict-mode: validate row width matches field count
                if (fields != null && rowValues.Count != fields.Count)
                {
                    throw new FormatException($"Tabular row width mismatch: expected {fields.Count} fields, got {rowValues.Count}");
                }
                
                items.Add(CreateObjectFromFields(elementType, fields, rowValues));
            }
            
            // Note: Count validation is relaxed for tabular arrays to allow row/key disambiguation
            
            return ConvertToArray(items, elementType, type);
        }
        else
        {
            // List items
            var items = new List<object?>();
            for (int i = 0; i < count && context.HasMoreLines(); i++)
            {
                var itemLine = context.PeekLine();
                
                // Strict-mode: blank lines not allowed in arrays
                if (string.IsNullOrWhiteSpace(itemLine) && items.Count > 0 && i < count - 1)
                {
                    throw new FormatException("Blank line found inside array rows");
                }
                
                var itemDepth = GetDepth(itemLine, context.IndentSize);
                if (itemDepth <= 0)
                {
                    break;
                }

                context.ReadLine();
                var trimmed = itemLine.Trim();
                if (trimmed.StartsWith("- "))
                {
                    var itemContent = trimmed[2..];
                    items.Add(ParseListItem(context, itemContent, elementType, 1));
                }
                else
                {
                    items.Add(ParsePrimitiveToken(trimmed, elementType));
                }
            }
            
            // Strict-mode: validate count matches
            if (items.Count != count)
            {
                throw new FormatException($"Array count mismatch: declared {count}, provided {items.Count}");
            }
            
            return ConvertToArray(items, elementType, type);
        }
    }

    private static object? ParseListItem(ParseContext context, string content, Type elementType, int depth)
    {
        // Check for nested array
        if (content.StartsWith('['))
        {
            var bracketEnd = content.IndexOf(']');
            if (bracketEnd > 0)
            {
                var headerContent = content[1..bracketEnd];
                
                // Extract delimiter and count (same logic as ParseRootArray)
                char delimiter = ',';
                string countStr;
                
                if (headerContent.EndsWith('|'))
                {
                    delimiter = '|';
                    countStr = headerContent[..^1];
                }
                else if (headerContent.EndsWith('\t'))
                {
                    delimiter = '\t';
                    countStr = headerContent[..^1];
                }
                else
                {
                    countStr = headerContent;
                }
                
                if (int.TryParse(countStr, out var count))
                {
                    var colonPos = content.IndexOf(':', bracketEnd);
                    if (colonPos > 0)
                    {
                        var values = content[(colonPos + 1)..].Trim();
                        var parsed = ParseDelimitedValues(values, delimiter);
                        
                        // Validate count
                        if (parsed.Count != count)
                        {
                            throw new FormatException($"Nested array count mismatch: declared {count}, provided {parsed.Count}");
                        }
                        
                        if (elementType.IsArray)
                        {
                            var innerType = elementType.GetElementType()!;
                            var convertedItems = parsed.Select(v => ParsePrimitiveToken(v, innerType)).ToList();
                            return ConvertToArray(convertedItems, innerType, elementType);
                        }
                        return parsed.Select(v => ParsePrimitiveToken(v, typeof(object))).ToArray();
                    }
                }
            }
        }

        // Check for object (has colon)
        var colonIndex = FindUnquotedColon(content);
        if (colonIndex > 0)
        {
            return ParseObjectFromListItem(context, content, elementType, depth);
        }

        // Primitive
        return ParsePrimitiveToken(content, elementType);
    }

    private static object? ParseObjectFromListItem(ParseContext context, string firstLine, Type type, int depth)
    {
        var colonIndex = FindUnquotedColon(firstLine);
        var key = firstLine[..colonIndex].Trim();
        var value = firstLine[(colonIndex + 1)..].Trim();

        // Check if key contains an array header
        var bracketIndex = key.IndexOf('[');
        if (bracketIndex > 0)
        {
            // Extract property name before bracket
            var propName = key[..bracketIndex];
            if (propName.StartsWith('"') && propName.EndsWith('"'))
            {
                propName = UnescapeString(propName[1..^1]);
            }
            
            var obj = Activator.CreateInstance(type);
            if (obj is null)
            {
                return null;
            }

            var propsByName = GetSerializableProperties(type)
                .ToDictionary(p => GetPropertyName(p), p => p, StringComparer.Ordinal);

            // Parse array header for first property
            if (propsByName.TryGetValue(propName, out var arrayProp))
            {
                var arrayHeader = key[bracketIndex..] + ":" + value;
                var arrayValue = ParseArrayHeader(context, arrayHeader, arrayProp.PropertyType, depth);
                SetPropertyValue(obj, arrayProp, arrayValue);
            }

            // Read remaining properties at depth+1
            while (context.HasMoreLines())
            {
                var nextLine = context.PeekLine();
                var nextDepth = GetDepth(nextLine, context.IndentSize);
                if (nextDepth <= depth)
                {
                    break;
                }

                context.ReadLine();
                var trimmed = nextLine.Trim();
                var nextColonIndex = FindUnquotedColon(trimmed);
                if (nextColonIndex > 0)
                {
                    var nextKey = trimmed[..nextColonIndex].Trim();
                    var nextValue = trimmed[(nextColonIndex + 1)..].Trim();

                    if (nextKey.StartsWith('"') && nextKey.EndsWith('"'))
                    {
                        nextKey = UnescapeString(nextKey[1..^1]);
                    }

                    if (propsByName.TryGetValue(nextKey, out var prop))
                    {
                        var propValue = string.IsNullOrEmpty(nextValue)
                            ? null
                            : ParsePrimitiveToken(nextValue, prop.PropertyType);
                        SetPropertyValue(obj, prop, propValue);
                    }
                }
            }

            return obj;
        }
        
        if (key.StartsWith('"') && key.EndsWith('"'))
        {
            key = UnescapeString(key[1..^1]);
        }

        var objNormal = Activator.CreateInstance(type);
        if (objNormal is null)
        {
            return null;
        }

        var propsNormal = GetSerializableProperties(type)
            .ToDictionary(p => GetPropertyName(p), p => p, StringComparer.Ordinal);

        // Set first property
        if (propsNormal.TryGetValue(key, out var firstProp))
        {
            var propValue = string.IsNullOrEmpty(value) 
                ? null 
                : ParsePrimitiveToken(value, firstProp.PropertyType);
            SetPropertyValue(objNormal, firstProp, propValue);
        }

        // Read remaining properties at depth+1
        while (context.HasMoreLines())
        {
            var nextLine = context.PeekLine();
            var nextDepth = GetDepth(nextLine, context.IndentSize);
            if (nextDepth <= depth)
            {
                break;
            }

            context.ReadLine();
            var trimmed = nextLine.Trim();
            var nextColonIndex = FindUnquotedColon(trimmed);
            if (nextColonIndex > 0)
            {
                var nextKey = trimmed[..nextColonIndex].Trim();
                var nextValue = trimmed[(nextColonIndex + 1)..].Trim();

                if (nextKey.StartsWith('"') && nextKey.EndsWith('"'))
                {
                    nextKey = UnescapeString(nextKey[1..^1]);
                }

                if (propsNormal.TryGetValue(nextKey, out var prop))
                {
                    var propValue = string.IsNullOrEmpty(nextValue)
                        ? null
                        : ParsePrimitiveToken(nextValue, prop.PropertyType);
                    SetPropertyValue(objNormal, prop, propValue);
                }
            }
        }

        return objNormal;
    }

    private static object? ParseObject(ParseContext context, Type type, int depth)
    {
        var obj = Activator.CreateInstance(type);
        if (obj is null)
        {
            return null;
        }

        var propsByName = GetSerializableProperties(type)
            .ToDictionary(p => GetPropertyName(p), p => p, StringComparer.Ordinal);

        while (context.HasMoreLines())
        {
            var line = context.PeekLine();
            var lineDepth = GetDepth(line, context.IndentSize);

            if (lineDepth < depth)
            {
                break;
            }

            if (lineDepth > depth)
            {
                context.ReadLine();
                continue;
            }

            context.ReadLine();
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            // Check for array header
            var bracketStart = trimmed.IndexOf('[');
            if (bracketStart > 0)
            {
                var key = trimmed[..bracketStart];
                if (key.StartsWith('"') && key.EndsWith('"'))
                {
                    key = UnescapeString(key[1..^1]);
                }

                if (propsByName.TryGetValue(key, out var prop))
                {
                    var arrayValue = ParseArrayHeader(context, trimmed[bracketStart..], prop.PropertyType, depth);
                    SetPropertyValue(obj, prop, arrayValue);
                }
                continue;
            }

            // Parse key: value
            var colonIndex = FindUnquotedColon(trimmed);
            if (colonIndex < 0)
            {
                // Strict-mode: if line looks like it should be a key-value but has no colon, throw
                if (!trimmed.StartsWith('-') && !trimmed.StartsWith('[') && trimmed.Contains(' '))
                {
                    throw new FormatException($"Missing colon in key-value pair: {trimmed}");
                }
                continue;
            }

            var propKey = trimmed[..colonIndex].Trim();
            var propValue = trimmed[(colonIndex + 1)..].Trim();

            if (propKey.StartsWith('"') && propKey.EndsWith('"'))
            {
                propKey = UnescapeString(propKey[1..^1]);
            }

            if (propsByName.TryGetValue(propKey, out var property))
            {
                if (string.IsNullOrEmpty(propValue))
                {
                    // Nested object
                    var nestedValue = ParseObject(context, property.PropertyType, depth + 1);
                    SetPropertyValue(obj, property, nestedValue);
                }
                else
                {
                    var parsed = ParsePrimitiveToken(propValue, property.PropertyType);
                    SetPropertyValue(obj, property, parsed);
                }
            }
        }

        return obj;
    }

    private static object? ParseArrayHeader(ParseContext context, string header, Type type, int depth)
    {
        var bracketEnd = header.IndexOf(']');
        if (bracketEnd < 0)
        {
            throw new FormatException("Invalid array header");
        }

        var headerContent = header[1..bracketEnd];
        
        // Extract delimiter and count
        char delimiter = ','; // default delimiter
        string countStr;
        
        if (headerContent.EndsWith('|'))
        {
            delimiter = '|';
            countStr = headerContent[..^1];
        }
        else if (headerContent.EndsWith('\t'))
        {
            delimiter = '\t';
            countStr = headerContent[..^1];
        }
        else
        {
            countStr = headerContent;
        }
        
        if (!int.TryParse(countStr, out var count))
        {
            throw new FormatException($"Invalid array count: {countStr}");
        }

        // Get element type
        Type elementType;
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            elementType = type.GetGenericArguments()[0];
        }
        else
        {
            elementType = typeof(object);
        }

        // Check for field list
        List<string>? fields = null;
        var braceStart = header.IndexOf('{', bracketEnd);
        var braceEnd = header.IndexOf('}', bracketEnd);
        if (braceStart > 0 && braceEnd > braceStart)
        {
            var fieldsStr = header[(braceStart + 1)..braceEnd];
            
            // Strict-mode: validate delimiter consistency between header and fields
            char fieldsDelimiter = DetectDelimiter(fieldsStr);
            if (fieldsDelimiter != delimiter && fieldsDelimiter != ',')
            {
                throw new FormatException($"Delimiter mismatch: header declares '{delimiter}' but fields use '{fieldsDelimiter}'");
            }
            
            fields = ParseDelimitedFields(fieldsStr, delimiter);
        }

        // Find the colon
        var colonPos = braceEnd > 0 ? header.IndexOf(':', braceEnd) : header.IndexOf(':', bracketEnd);
        if (colonPos < 0)
        {
            throw new FormatException("Missing colon in array header");
        }

        // Check for inline values
        var remainder = header[(colonPos + 1)..].Trim();
        if (!string.IsNullOrEmpty(remainder))
        {
            // Inline primitive array
            var values = ParseDelimitedValues(remainder, delimiter);
            
            // Strict-mode: validate count matches
            if (values.Count != count)
            {
                throw new FormatException($"Array count mismatch: declared {count}, provided {values.Count}");
            }
            
            return ConvertToArray(values, elementType, type);
        }

        // Read items at next depth
        if (fields is not null)
        {
            // Tabular array
            var items = new List<object?>();
            for (int i = 0; i < count && context.HasMoreLines(); i++)
            {
                var rowLine = context.PeekLine();
                var rowDepth = GetDepth(rowLine, context.IndentSize);
                if (rowDepth <= depth)
                {
                    break;
                }

                // Check if this line is a key-value pair (colon before delimiter)
                var trimmed = rowLine.Trim();
                var rowColonPos = FindUnquotedColon(trimmed);
                var rowDelimiterPos = trimmed.IndexOf(delimiter);
                if (rowColonPos >= 0 && (rowDelimiterPos < 0 || rowColonPos < rowDelimiterPos))
                {
                    // This is a key-value pair, not a table row
                    break;
                }

                context.ReadLine();
                var rowValues = ParseDelimitedValues(rowLine.Trim(), delimiter);
                
                // Strict-mode: validate row width matches field count
                if (fields != null && rowValues.Count != fields.Count)
                {
                    throw new FormatException($"Tabular row width mismatch: expected {fields.Count} fields, got {rowValues.Count}");
                }
                
                items.Add(CreateObjectFromFields(elementType, fields, rowValues));
            }
            
            // Note: Count validation is relaxed for tabular arrays to allow row/key disambiguation
            
            return ConvertToArray(items, elementType, type);
        }
        else
        {
            // List items
            var items = new List<object?>();
            for (int i = 0; i < count && context.HasMoreLines(); i++)
            {
                var itemLine = context.PeekLine();
                
                // Strict-mode: blank lines not allowed in arrays
                if (string.IsNullOrWhiteSpace(itemLine) && items.Count > 0 && i < count - 1)
                {
                    throw new FormatException("Blank line found inside array rows");
                }
                
                var itemDepth = GetDepth(itemLine, context.IndentSize);
                if (itemDepth <= depth)
                {
                    break;
                }

                context.ReadLine();
                var trimmed = itemLine.Trim();
                if (trimmed.StartsWith("- "))
                {
                    var itemContent = trimmed[2..];
                    items.Add(ParseListItem(context, itemContent, elementType, depth + 1));
                }
                else
                {
                    items.Add(ParsePrimitiveToken(trimmed, elementType));
                }
            }
            
            // Strict-mode: validate count matches
            if (items.Count != count)
            {
                throw new FormatException($"Array count mismatch: declared {count}, provided {items.Count}");
            }
            
            return ConvertToArray(items, elementType, type);
        }
    }

    private static object? CreateObjectFromFields(Type type, List<string> fields, List<string> values)
    {
        var obj = Activator.CreateInstance(type);
        if (obj is null)
        {
            return null;
        }

        var propsByName = GetSerializableProperties(type)
            .ToDictionary(p => GetPropertyName(p), p => p, StringComparer.Ordinal);

        for (int i = 0; i < fields.Count && i < values.Count; i++)
        {
            if (propsByName.TryGetValue(fields[i], out var prop))
            {
                var value = ParsePrimitiveToken(values[i], prop.PropertyType);
                SetPropertyValue(obj, prop, value);
            }
        }

        return obj;
    }

    private static object? ConvertToArray(List<string> values, Type elementType, Type targetType)
    {
        var parsed = values.Select(v => ParsePrimitiveToken(v, elementType)).ToList();
        return ConvertToArray(parsed, elementType, targetType);
    }

    private static object? ConvertToArray(List<object?> items, Type elementType, Type targetType)
    {
        if (targetType.IsArray)
        {
            var array = Array.CreateInstance(elementType, items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                array.SetValue(items[i], i);
            }
            return array;
        }
        else if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var list = (IList)Activator.CreateInstance(targetType)!;
            foreach (var item in items)
            {
                list.Add(item);
            }
            return list;
        }
        return items.ToArray();
    }

    private static object? ParsePrimitiveToken(string token, Type targetType)
    {
        token = token.Trim();

        if (string.IsNullOrEmpty(token))
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        // Quoted string - validate and unescape
        if (token.StartsWith('"'))
        {
            if (!token.EndsWith('"') || token.Length < 2)
            {
                throw new FormatException($"Unterminated quoted string: {token}");
            }
            var str = UnescapeString(token[1..^1]);
            return ConvertTo(str, targetType);
        }

        // Null
        if (token == "null")
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        // Boolean
        if (token == "true")
        {
            return ConvertTo(true, targetType);
        }
        if (token == "false")
        {
            return ConvertTo(false, targetType);
        }

        // Number - try to parse
        if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
        {
            // Check for leading zeros (invalid as number)
            if (LeadingZeroPattern.IsMatch(token))
            {
                return ConvertTo(token, targetType);
            }
            return ConvertTo(num, targetType);
        }

        // String
        return ConvertTo(token, targetType);
    }

    private static object? ConvertTo(object value, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return value.ToString();
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value is double d)
        {
            if (underlyingType == typeof(int))
            {
                return (int)d;
            }
            if (underlyingType == typeof(long))
            {
                return (long)d;
            }
            if (underlyingType == typeof(float))
            {
                return (float)d;
            }
            if (underlyingType == typeof(double))
            {
                return d;
            }
            if (underlyingType == typeof(decimal))
            {
                return (decimal)d;
            }
            if (underlyingType == typeof(short))
            {
                return (short)d;
            }
            if (underlyingType == typeof(byte))
            {
                return (byte)d;
            }
        }

        if (value is bool b && underlyingType == typeof(bool))
        {
            return b;
        }

        if (value is string s)
        {
            if (underlyingType == typeof(string))
            {
                return s;
            }
            if (underlyingType.IsEnum)
            {
                return Enum.Parse(underlyingType, s, ignoreCase: true);
            }
        }

        return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
    }

    private static List<string> ParseDelimitedValues(string input, char delimiter)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        bool escaped = false;

        foreach (var c in input)
        {
            if (escaped)
            {
                current.Append(c);
                escaped = false;
                continue;
            }

            if (c == '\\' && inQuotes)
            {
                current.Append(c);
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                current.Append(c);
                inQuotes = !inQuotes;
                continue;
            }

            if (c == delimiter && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        values.Add(current.ToString().Trim());
        return values;
    }
    
    private static char DetectDelimiter(string input)
    {
        bool inQuotes = false;
        foreach (var c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (!inQuotes)
            {
                if (c == '|') return '|';
                if (c == '\t') return '\t';
                if (c == ',') return ',';
            }
        }
        return ','; // default
    }

    private static List<string> ParseDelimitedFields(string input, char delimiter)
    {
        var values = ParseDelimitedValues(input, delimiter);
        return values.Select(v =>
        {
            v = v.Trim();
            if (v.StartsWith('"') && v.EndsWith('"'))
            {
                return UnescapeString(v[1..^1]);
            }
            return v;
        }).ToList();
    }

    private static string UnescapeString(string value)
    {
        var sb = new StringBuilder(value.Length);
        bool escaped = false;

        foreach (var c in value)
        {
            if (escaped)
            {
                sb.Append(c switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    _ => throw new FormatException($"Invalid escape sequence: \\{c}")
                });
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            sb.Append(c);
        }

        if (escaped)
        {
            throw new FormatException("Unterminated escape sequence");
        }

        return sb.ToString();
    }

    private static int FindUnquotedColon(string line)
    {
        bool inQuotes = false;
        bool escaped = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\' && inQuotes)
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ':' && !inQuotes)
            {
                return i;
            }
        }

        return -1;
    }

    private static int GetDepth(string line, int indentSize)
    {
        int spaces = 0;
        foreach (var c in line)
        {
            if (c == ' ')
            {
                spaces++;
            }
            else if (c == '\t')
            {
                // Strict-mode: tabs not allowed for indentation
                throw new FormatException("Tab character used for indentation is not allowed");
            }
            else
            {
                break;
            }
        }
        
        // Strict-mode: indentation must be a multiple of indentSize
        if (spaces > 0 && spaces % indentSize != 0)
        {
            throw new FormatException($"Indentation must be a multiple of {indentSize}, got {spaces} spaces");
        }
        
        return spaces / indentSize;
    }

    #endregion

    #region Reflection Helpers

    private static IEnumerable<PropertyInfo> GetSerializableProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !p.GetCustomAttributes<ToonIgnoreAttribute>().Any())
            .Where(p => p.GetIndexParameters().Length == 0);
    }

    private static string GetPropertyName(PropertyInfo prop)
    {
        var attr = prop.GetCustomAttribute<ToonPropertyNameAttribute>();
        return attr?.Name ?? prop.Name;
    }

    private static void SetPropertyValue(object obj, PropertyInfo prop, object? value)
    {
        if (prop.CanWrite)
        {
            prop.SetValue(obj, value);
        }
    }

    private static bool IsPrimitive(Type type)
    {
        return type.IsPrimitive || type == typeof(decimal) ||
               Nullable.GetUnderlyingType(type)?.IsPrimitive == true ||
               Nullable.GetUnderlyingType(type) == typeof(decimal);
    }

    private static bool IsArrayLike(object value)
    {
        return value is IEnumerable && value is not string;
    }

    private static bool AllPrimitives(List<object?> items)
    {
        foreach (var item in items)
        {
            if (item is null)
            {
                continue;
            }
            if (item is string)
            {
                continue;
            }
            if (IsPrimitive(item.GetType()))
            {
                continue;
            }
            return false;
        }
        return true;
    }

    private static bool IsTabularArray(List<object?> items, out List<string> fields)
    {
        fields = [];

        if (items.Count == 0)
        {
            return false;
        }

        // All items must be non-null objects (not primitives or strings)
        HashSet<string>? commonKeys = null;
        foreach (var item in items)
        {
            if (item is null || item is string || IsPrimitive(item.GetType()) || IsArrayLike(item))
            {
                return false;
            }

            var props = GetSerializableProperties(item.GetType());
            var keys = new HashSet<string>(props.Select(GetPropertyName));

            if (commonKeys is null)
            {
                commonKeys = keys;
            }
            else if (!keys.SetEquals(commonKeys))
            {
                return false;
            }

            // Check all values are primitives
            foreach (var prop in props)
            {
                var value = prop.GetValue(item);
                if (value is not null && !IsPrimitive(value.GetType()) && value is not string)
                {
                    return false;
                }
            }
        }

        if (commonKeys is null || commonKeys.Count == 0)
        {
            return false;
        }

        // Get fields in order from first item
        var firstItem = items[0];
        if (firstItem is null)
        {
            return false;
        }

        fields = GetSerializableProperties(firstItem.GetType())
            .Select(GetPropertyName)
            .ToList();

        return true;
    }

    #endregion

    #region Parse Context

    private sealed class ParseContext
    {
        private readonly string[] _lines;
        private int _position;
        public int IndentSize { get; }

        public ParseContext(string[] lines, int indentSize)
        {
            _lines = lines;
            _position = 0;
            IndentSize = indentSize;
        }

        public bool HasMoreLines() => _position < _lines.Length;

        public string PeekLine()
        {
            if (_position >= _lines.Length)
            {
                return string.Empty;
            }
            return _lines[_position];
        }

        public string ReadLine()
        {
            if (_position >= _lines.Length)
            {
                return string.Empty;
            }
            return _lines[_position++];
        }
    }

    #endregion

    #region Regex

    [GeneratedRegex(@"^-?\d+(?:\.\d+)?(?:e[+-]?\d+)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NumericPatternRegex();

    [GeneratedRegex(@"^0\d+$", RegexOptions.Compiled)]
    private static partial Regex LeadingZeroPatternRegex();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_.]*$", RegexOptions.Compiled)]
    private static partial Regex UnquotedKeyPatternRegex();

    #endregion
}
