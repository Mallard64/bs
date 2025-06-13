using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;


// Simple JSON parser - much smaller than MiniJSON
public static class SimpleJSON
{
    public class JSONNode
    {
        public virtual JSONNode this[string key] { get { return null; } set { } }
        public virtual JSONNode this[int index] { get { return null; } set { } }
        public virtual string Value { get { return ""; } set { } }
        public virtual int AsInt { get { return 0; } }
        public virtual JSONArray AsArray { get { return this as JSONArray; } }
        public virtual JSONObject AsObject { get { return this as JSONObject; } }
    }

    public class JSONObject : JSONNode, IEnumerable<KeyValuePair<string, JSONNode>>
    {
        private Dictionary<string, JSONNode> dict = new Dictionary<string, JSONNode>();

        public override JSONNode this[string key]
        {
            get { return dict.ContainsKey(key) ? dict[key] : null; }
            set { dict[key] = value; }
        }

        public IEnumerator<KeyValuePair<string, JSONNode>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class JSONArray : JSONNode, IEnumerable<JSONNode>
    {
        private List<JSONNode> list = new List<JSONNode>();

        public override JSONNode this[int index]
        {
            get { return index >= 0 && index < list.Count ? list[index] : null; }
            set
            {
                if (index >= 0 && index < list.Count)
                    list[index] = value;
            }
        }

        public void Add(JSONNode item)
        {
            list.Add(item);
        }

        public IEnumerator<JSONNode> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class JSONString : JSONNode
    {
        private string value;

        public JSONString(string value)
        {
            this.value = value;
        }

        public override string Value
        {
            get { return value; }
            set { this.value = value; }
        }
    }

    public class JSONNumber : JSONNode
    {
        private double value;

        public JSONNumber(double value)
        {
            this.value = value;
        }

        public override string Value
        {
            get { return value.ToString(); }
            set { double.TryParse(value, out this.value); }
        }

        public override int AsInt
        {
            get { return (int)value; }
        }
    }

    public static JSONNode Parse(string json)
    {
        int index = 0;
        return ParseValue(json, ref index);
    }

    private static JSONNode ParseValue(string json, ref int index)
    {
        SkipWhitespace(json, ref index);

        if (index >= json.Length) return null;

        char c = json[index];

        if (c == '{') return ParseObject(json, ref index);
        if (c == '[') return ParseArray(json, ref index);
        if (c == '"') return ParseString(json, ref index);
        if (c == '-' || char.IsDigit(c)) return ParseNumber(json, ref index);
        if (json.Substring(index).StartsWith("true")) { index += 4; return new JSONString("true"); }
        if (json.Substring(index).StartsWith("false")) { index += 5; return new JSONString("false"); }
        if (json.Substring(index).StartsWith("null")) { index += 4; return null; }

        return null;
    }

    private static JSONObject ParseObject(string json, ref int index)
    {
        JSONObject obj = new JSONObject();
        index++; // Skip '{'

        while (true)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index] == '}') break;

            if (json[index] == ',') { index++; continue; }

            var key = ParseString(json, ref index);
            if (key == null) break;

            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index] != ':') break;
            index++; // Skip ':'

            var value = ParseValue(json, ref index);
            obj[key.Value] = value;
        }

        if (index < json.Length) index++; // Skip '}'
        return obj;
    }

    private static JSONArray ParseArray(string json, ref int index)
    {
        JSONArray arr = new JSONArray();
        index++; // Skip '['

        while (true)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index] == ']') break;

            if (json[index] == ',') { index++; continue; }

            var value = ParseValue(json, ref index);
            if (value != null) arr.Add(value);
        }

        if (index < json.Length) index++; // Skip ']'
        return arr;
    }

    private static JSONString ParseString(string json, ref int index)
    {
        index++; // Skip opening '"'
        int start = index;

        while (index < json.Length && json[index] != '"')
        {
            if (json[index] == '\\') index++; // Skip escaped character
            index++;
        }

        string value = json.Substring(start, index - start);
        index++; // Skip closing '"'

        return new JSONString(value);
    }

    private static JSONNumber ParseNumber(string json, ref int index)
    {
        int start = index;

        if (json[index] == '-') index++;

        while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.'))
        {
            index++;
        }

        double value;
        double.TryParse(json.Substring(start, index - start), out value);

        return new JSONNumber(value);
    }

    private static void SkipWhitespace(string json, ref int index)
    {
        while (index < json.Length && char.IsWhiteSpace(json[index]))
        {
            index++;
        }
    }
}