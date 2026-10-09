using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace CampusLife.Core.Serialization
{
    /// <summary>
    /// Small, deterministic JSON serializer for save data. It maps the public
    /// instance fields of plain classes (the same convention as Unity's
    /// JsonUtility) but also supports top-level lists, nulls and 64-bit
    /// integers without precision loss, and runs identically inside Unity and
    /// in plain .NET test runs. Supported field types: string, bool, int,
    /// long, float, double, enums (written by name), List&lt;T&gt;, T[] and
    /// nested classes with a public parameterless constructor.
    /// </summary>
    /// <summary>A JSON number kept as its original text to avoid precision loss.</summary>
    public sealed class JsonNumber
    {
        public string Text;
        public JsonNumber(string text) { Text = text; }
        public static JsonNumber Of(long value) => new JsonNumber(value.ToString(CultureInfo.InvariantCulture));
        public long ToInt64() => long.Parse(Text, NumberStyles.Integer, CultureInfo.InvariantCulture);
        public double ToDouble() => double.Parse(Text, NumberStyles.Float, CultureInfo.InvariantCulture);
        public override string ToString() => Text;
    }

    public static class FieldJson
    {
        public static string Serialize(object value)
        {
            var sb = new StringBuilder(256);
            WriteValue(sb, value);
            return sb.ToString();
        }

        public static T Deserialize<T>(string json) => (T)Deserialize(json, typeof(T));

        /// <summary>
        /// Parses JSON into a mutable tree of Dictionary&lt;string, object&gt;,
        /// List&lt;object&gt;, string, bool, <see cref="JsonNumber"/> and null.
        /// Used by save migrations, which rewrite old data before binding.
        /// </summary>
        public static object ParseTree(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            return new Parser(json).ParseDocument();
        }

        public static T FromTree<T>(object tree) => (T)Bind(tree, typeof(T), "$");

        public static object Deserialize(string json, Type type)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var parser = new Parser(json);
            object tree = parser.ParseDocument();
            return Bind(tree, type, "$");
        }

        // ---------------------------------------------------------------- write

        static void WriteValue(StringBuilder sb, object value)
        {
            if (value == null) { sb.Append("null"); return; }
            var type = value.GetType();
            switch (value)
            {
                case string s: WriteString(sb, s); return;
                case bool b: sb.Append(b ? "true" : "false"); return;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); return;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); return;
                case float f: WriteFloating(sb, f.ToString("R", CultureInfo.InvariantCulture), float.IsNaN(f) || float.IsInfinity(f)); return;
                case double d: WriteFloating(sb, d.ToString("R", CultureInfo.InvariantCulture), double.IsNaN(d) || double.IsInfinity(d)); return;
                case JsonNumber n: sb.Append(n.Text); return;
                case Dictionary<string, object> map:
                {
                    var keys = new List<string>(map.Keys);
                    keys.Sort(StringComparer.Ordinal);
                    sb.Append('{');
                    for (int i = 0; i < keys.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        WriteString(sb, keys[i]);
                        sb.Append(':');
                        WriteValue(sb, map[keys[i]]);
                    }
                    sb.Append('}');
                    return;
                }
            }
            if (type.IsEnum) { WriteString(sb, value.ToString()); return; }
            if (value is IList list)
            {
                sb.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    WriteValue(sb, list[i]);
                }
                sb.Append(']');
                return;
            }
            if (type.IsPrimitive) throw new NotSupportedException($"Unsupported primitive type {type}.");

            sb.Append('{');
            bool first = true;
            foreach (var field in GetFields(type))
            {
                if (!first) sb.Append(',');
                first = false;
                WriteString(sb, field.Name);
                sb.Append(':');
                WriteValue(sb, field.GetValue(value));
            }
            sb.Append('}');
        }

        static void WriteFloating(StringBuilder sb, string text, bool invalid)
        {
            if (invalid) throw new NotSupportedException("NaN and Infinity cannot be stored in save data.");
            sb.Append(text);
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        static readonly Dictionary<Type, FieldInfo[]> FieldCache = new Dictionary<Type, FieldInfo[]>();

        static FieldInfo[] GetFields(Type type)
        {
            lock (FieldCache)
            {
                if (FieldCache.TryGetValue(type, out var cached)) return cached;
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                // Declaration order is not guaranteed by reflection; sort for stable output.
                Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));
                var result = Array.FindAll(fields, f => !f.IsInitOnly && !f.IsLiteral && !f.IsDefined(typeof(NonSerializedAttribute), false));
                FieldCache[type] = result;
                return result;
            }
        }

        // ----------------------------------------------------------------- bind


        static object Bind(object node, Type type, string path)
        {
            if (node == null)
            {
                if (type.IsValueType) throw new FormatException($"{path}: null is not valid for {type.Name}.");
                return null;
            }
            if (type == typeof(string))
                return node as string ?? throw new FormatException($"{path}: expected string.");
            if (type == typeof(bool))
                return node is bool b ? b : throw new FormatException($"{path}: expected boolean.");
            if (type.IsEnum)
            {
                if (node is string name)
                {
                    try { return Enum.Parse(type, name, false); }
                    catch (ArgumentException) { throw new FormatException($"{path}: '{name}' is not a valid {type.Name}."); }
                }
                throw new FormatException($"{path}: expected enum name.");
            }
            if (type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double))
            {
                if (!(node is JsonNumber num)) throw new FormatException($"{path}: expected number.");
                try
                {
                    if (type == typeof(int)) return int.Parse(num.Text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    if (type == typeof(long)) return long.Parse(num.Text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    if (type == typeof(float)) return float.Parse(num.Text, NumberStyles.Float, CultureInfo.InvariantCulture);
                    return double.Parse(num.Text, NumberStyles.Float, CultureInfo.InvariantCulture);
                }
                catch (Exception e) when (e is FormatException || e is OverflowException)
                {
                    throw new FormatException($"{path}: '{num.Text}' is not a valid {type.Name}.");
                }
            }
            if (type.IsArray)
            {
                var items = node as List<object> ?? throw new FormatException($"{path}: expected array.");
                var elementType = type.GetElementType();
                var array = Array.CreateInstance(elementType, items.Count);
                for (int i = 0; i < items.Count; i++) array.SetValue(Bind(items[i], elementType, $"{path}[{i}]"), i);
                return array;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var items = node as List<object> ?? throw new FormatException($"{path}: expected array.");
                var elementType = type.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(type);
                for (int i = 0; i < items.Count; i++) list.Add(Bind(items[i], elementType, $"{path}[{i}]"));
                return list;
            }
            if (type.IsPrimitive) throw new NotSupportedException($"Unsupported primitive type {type}.");

            var obj = node as Dictionary<string, object> ?? throw new FormatException($"{path}: expected object.");
            object instance = Activator.CreateInstance(type);
            foreach (var field in GetFields(type))
            {
                // Missing fields keep their constructor defaults, which lets
                // older saves load into newer types before migration.
                if (obj.TryGetValue(field.Name, out var child))
                    field.SetValue(instance, Bind(child, field.FieldType, path + "." + field.Name));
            }
            return instance;
        }

        // ---------------------------------------------------------------- parse

        sealed class Parser
        {
            readonly string _s;
            int _i;
            int _depth;
            const int MaxDepth = 64;

            public Parser(string s) { _s = s; }

            public object ParseDocument()
            {
                SkipWhitespace();
                var value = ParseValue();
                SkipWhitespace();
                if (_i != _s.Length) throw Error("Unexpected trailing characters");
                return value;
            }

            FormatException Error(string message) => new FormatException($"Invalid JSON at position {_i}: {message}.");

            void SkipWhitespace()
            {
                while (_i < _s.Length && (_s[_i] == ' ' || _s[_i] == '\t' || _s[_i] == '\n' || _s[_i] == '\r')) _i++;
            }

            object ParseValue()
            {
                if (_i >= _s.Length) throw Error("Unexpected end of input");
                char c = _s[_i];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber();
                        throw Error($"Unexpected character '{c}'");
                }
            }

            void Expect(string literal)
            {
                if (string.CompareOrdinal(_s, _i, literal, 0, literal.Length) != 0) throw Error($"Expected '{literal}'");
                _i += literal.Length;
            }

            Dictionary<string, object> ParseObject()
            {
                if (++_depth > MaxDepth) throw Error("Nesting too deep");
                var result = new Dictionary<string, object>(StringComparer.Ordinal);
                _i++;
                SkipWhitespace();
                if (_i < _s.Length && _s[_i] == '}') { _i++; _depth--; return result; }
                while (true)
                {
                    SkipWhitespace();
                    if (_i >= _s.Length || _s[_i] != '"') throw Error("Expected property name");
                    string key = ParseString();
                    SkipWhitespace();
                    if (_i >= _s.Length || _s[_i] != ':') throw Error("Expected ':'");
                    _i++;
                    SkipWhitespace();
                    result[key] = ParseValue();
                    SkipWhitespace();
                    if (_i >= _s.Length) throw Error("Unterminated object");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == '}') { _i++; break; }
                    throw Error("Expected ',' or '}'");
                }
                _depth--;
                return result;
            }

            List<object> ParseArray()
            {
                if (++_depth > MaxDepth) throw Error("Nesting too deep");
                var result = new List<object>();
                _i++;
                SkipWhitespace();
                if (_i < _s.Length && _s[_i] == ']') { _i++; _depth--; return result; }
                while (true)
                {
                    SkipWhitespace();
                    result.Add(ParseValue());
                    SkipWhitespace();
                    if (_i >= _s.Length) throw Error("Unterminated array");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == ']') { _i++; break; }
                    throw Error("Expected ',' or ']'");
                }
                _depth--;
                return result;
            }

            string ParseString()
            {
                var sb = new StringBuilder();
                _i++; // opening quote
                while (true)
                {
                    if (_i >= _s.Length) throw Error("Unterminated string");
                    char c = _s[_i++];
                    if (c == '"') break;
                    if (c != '\\') { sb.Append(c); continue; }
                    if (_i >= _s.Length) throw Error("Unterminated escape");
                    char e = _s[_i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_i + 4 > _s.Length) throw Error("Invalid unicode escape");
                            if (!int.TryParse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                                throw Error("Invalid unicode escape");
                            sb.Append((char)code);
                            _i += 4;
                            break;
                        default: throw Error($"Invalid escape '\\{e}'");
                    }
                }
                return sb.ToString();
            }

            JsonNumber ParseNumber()
            {
                int start = _i;
                if (_s[_i] == '-') _i++;
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') _i++;
                    else break;
                }
                return new JsonNumber(_s.Substring(start, _i - start));
            }
        }
    }
}
