using System.Collections.Generic;

namespace CombinationTestGenerator.Generation;

/// <summary>
/// Simple recursive-descent JSON parser for netstandard2.0 that handles nested structures.
/// </summary>
public sealed class SimpleJsonParser
{
    private readonly string _json;
    private int _index;

    public SimpleJsonParser(string json)
    {
        _json = json;
        _index = 0;
    }

    public object? Parse()
    {
        SkipWhitespace();
        return ParseValue();
    }

    private object? ParseValue()
    {
        SkipWhitespace();

        if (_index >= _json.Length) return null;

        var c = _json[_index];

        if (c == '{') return ParseObject();
        if (c == '[') return ParseArray();
        if (c == '"') return ParseString();
        if (c == 't' || c == 'f') return ParseBool();
        if (c == 'n') return ParseNull();
        if (char.IsDigit(c) || c == '-') return ParseNumber();

        return null;
    }

    private Dictionary<string, object?> ParseObject()
    {
        var result = new Dictionary<string, object?>();
        _index++; // skip '{'
        SkipWhitespace();

        while (_index < _json.Length && _json[_index] != '}')
        {
            SkipWhitespace();
            var key = ParseString();
            SkipWhitespace();
            if (_index < _json.Length && _json[_index] == ':')
            {
                _index++; // skip ':'
            }
            SkipWhitespace();
            var value = ParseValue();
            result[key] = value;

            SkipWhitespace();
            if (_index < _json.Length && _json[_index] == ',')
            {
                _index++; // skip ','
            }
            SkipWhitespace();
        }

        if (_index < _json.Length) _index++; // skip '}'
        return result;
    }

    private List<object?> ParseArray()
    {
        var result = new List<object?>();
        _index++; // skip '['
        SkipWhitespace();

        while (_index < _json.Length && _json[_index] != ']')
        {
            var value = ParseValue();
            result.Add(value);

            SkipWhitespace();
            if (_index < _json.Length && _json[_index] == ',')
            {
                _index++; // skip ','
            }
            SkipWhitespace();
        }

        if (_index < _json.Length) _index++; // skip ']'
        return result;
    }

    private string ParseString()
    {
        _index++; // skip opening '"'
        var start = _index;

        while (_index < _json.Length)
        {
            var c = _json[_index];
            if (c == '"')
            {
                var str = _json.Substring(start, _index - start);
                _index++; // skip closing '"'
                return str;
            }
            if (c == '\\' && _index + 1 < _json.Length)
            {
                _index += 2; // skip escape sequence
            }
            else
            {
                _index++;
            }
        }

        return _json.Substring(start);
    }

    private bool ParseBool()
    {
        if (_json.Substring(_index).StartsWith("true"))
        {
            _index += 4;
            return true;
        }
        if (_json.Substring(_index).StartsWith("false"))
        {
            _index += 5;
            return false;
        }
        return false;
    }

    private object? ParseNull()
    {
        if (_json.Substring(_index).StartsWith("null"))
        {
            _index += 4;
            return null;
        }
        return null;
    }

    private int ParseNumber()
    {
        var start = _index;
        if (_json[_index] == '-') _index++;

        while (_index < _json.Length && (char.IsDigit(_json[_index]) || _json[_index] == '.'))
        {
            _index++;
        }

        var numStr = _json.Substring(start, _index - start);
        if (int.TryParse(numStr, out var intVal))
        {
            return intVal;
        }
        return 0;
    }

    private void SkipWhitespace()
    {
        while (_index < _json.Length && char.IsWhiteSpace(_json[_index]))
        {
            _index++;
        }
    }
}
