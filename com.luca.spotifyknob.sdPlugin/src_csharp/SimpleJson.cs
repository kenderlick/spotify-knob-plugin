using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SpotifyKnob
{
    public static class SimpleJson
    {
        public class JsonObject
        {
            public Dictionary<string, object> Fields = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            public string GetString(string key, string fallback = "")
            {
                object v;
                if (Fields.TryGetValue(key, out v) && v != null)
                {
                    return v.ToString();
                }
                return fallback;
            }

            public int GetInt(string key, int fallback = 0)
            {
                object v;
                if (Fields.TryGetValue(key, out v) && v != null)
                {
                    if (v is int) return (int)v;
                    if (v is long) return (int)(long)v;
                    if (v is double) return (int)(double)v;
                    int parsed;
                    if (int.TryParse(v.ToString(), out parsed)) return parsed;
                }
                return fallback;
            }

            public bool GetBool(string key, bool fallback = false)
            {
                object v;
                if (Fields.TryGetValue(key, out v) && v != null)
                {
                    if (v is bool) return (bool)v;
                    bool parsed;
                    if (bool.TryParse(v.ToString(), out parsed)) return parsed;
                }
                return fallback;
            }

            public JsonObject GetObject(string key)
            {
                object v;
                if (Fields.TryGetValue(key, out v) && v is JsonObject)
                {
                    return (JsonObject)v;
                }
                return null;
            }
        }

        public static JsonObject Parse(string json)
        {
            int index = 0;
            return ParseObject(json, ref index);
        }

        private static void SkipWhiteSpace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }

        private static JsonObject ParseObject(string json, ref int index)
        {
            SkipWhiteSpace(json, ref index);
            if (index >= json.Length || json[index] != '{') return null;
            index++; // skip '{'

            var obj = new JsonObject();
            while (index < json.Length)
            {
                SkipWhiteSpace(json, ref index);
                if (index >= json.Length) break;
                if (json[index] == '}')
                {
                    index++;
                    break;
                }

                if (json[index] == ',')
                {
                    index++;
                    continue;
                }

                string key = ParseString(json, ref index);
                SkipWhiteSpace(json, ref index);
                if (index < json.Length && json[index] == ':')
                {
                    index++;
                }

                object val = ParseValue(json, ref index);
                obj.Fields[key] = val;
            }
            return obj;
        }

        private static object ParseValue(string json, ref int index)
        {
            SkipWhiteSpace(json, ref index);
            if (index >= json.Length) return null;

            char c = json[index];
            if (c == '"')
            {
                return ParseString(json, ref index);
            }
            if (c == '{')
            {
                return ParseObject(json, ref index);
            }
            if (c == '[')
            {
                return ParseArray(json, ref index);
            }
            if (char.IsDigit(c) || c == '-')
            {
                return ParseNumber(json, ref index);
            }
            if (json.Substring(index).StartsWith("true", StringComparison.OrdinalIgnoreCase))
            {
                index += 4;
                return true;
            }
            if (json.Substring(index).StartsWith("false", StringComparison.OrdinalIgnoreCase))
            {
                index += 5;
                return false;
            }
            if (json.Substring(index).StartsWith("null", StringComparison.OrdinalIgnoreCase))
            {
                index += 4;
                return null;
            }

            index++;
            return null;
        }

        private static List<object> ParseArray(string json, ref int index)
        {
            var list = new List<object>();
            index++; // skip '['
            while (index < json.Length)
            {
                SkipWhiteSpace(json, ref index);
                if (index >= json.Length) break;
                if (json[index] == ']')
                {
                    index++;
                    break;
                }
                if (json[index] == ',')
                {
                    index++;
                    continue;
                }
                list.Add(ParseValue(json, ref index));
            }
            return list;
        }

        private static string ParseString(string json, ref int index)
        {
            SkipWhiteSpace(json, ref index);
            if (index >= json.Length || json[index] != '"') return "";
            index++; // skip opening '"'

            var sb = new StringBuilder();
            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"')
                {
                    break;
                }
                if (c == '\\' && index < json.Length)
                {
                    char esc = json[index++];
                    switch (esc)
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
                            if (index + 4 <= json.Length)
                            {
                                string hex = json.Substring(index, 4);
                                int unicodeChar;
                                if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out unicodeChar))
                                {
                                    sb.Append((char)unicodeChar);
                                    index += 4;
                                }
                            }
                            break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static object ParseNumber(string json, ref int index)
        {
            int start = index;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == '-' || json[index] == '+' || json[index] == 'e' || json[index] == 'E'))
            {
                index++;
            }
            string numStr = json.Substring(start, index - start);
            int intVal;
            if (int.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out intVal))
            {
                return intVal;
            }
            double dblVal;
            if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out dblVal))
            {
                return dblVal;
            }
            return 0;
        }
    }
}
