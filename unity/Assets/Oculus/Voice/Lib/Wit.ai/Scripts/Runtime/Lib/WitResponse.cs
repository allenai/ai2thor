//#define USE_SharpZipLib
#if !UNITY_WEBPLAYER
#define USE_FileIO
#endif

/* * * * *
 * A simple JSON Parser / builder
 * ------------------------------
 *
 * It mainly has been written as a simple JSON parser. It can build a JSON string
 * from the node-tree, or generate a node tree from any valid JSON string.
 *
 * If you want to use compression when saving to file / stream / B64 you have to include
 * SharpZipLib ( http://www.icsharpcode.net/opensource/sharpziplib/ ) in your project and
 * define "USE_SharpZipLib" at the top of the file
 *
 * Written by Bunny83
 * 2012-06-09
 *
 * Features / attributes:
 * - provides strongly typed node classes and lists / dictionaries
 * - provides easy access to class members / array items / data values
 * - the parser ignores data types. Each value is a string.
 * - only double quotes (") are used for quoting strings.
 * - values and names are not restricted to quoted strings. They simply add up and are trimmed.
 * - There are only 3 types: arrays(WitResponseArray), objects(WitResponseClass) and values(WitResponseData)
 * - provides "casting" properties to easily convert to / from those types:
 *   int / float / double / bool
 * - provides a common interface for each node so no explicit casting is required.
 * - the parser try to avoid errors, but if malformed JSON is parsed the result is undefined
 *
 *
 * 2012-12-17 Update:
 * - Added internal JSONLazyCreator class which simplifies the construction of a JSON tree
 *   Now you can simple reference any item that doesn't exist yet and it will return a
 *   JSONLazyCreator The class determines the required type by it's further use, creates the type
 *   and removes itself.
 * - Added binary serialization / deserialization.
 * - Added support for BZip2 zipped binary format. Requires the SharpZipLib
 *   ( http://www.icsharpcode.net/opensource/sharpziplib/ )
 *   The usage of the SharpZipLib library can be disabled by removing or commenting out the
 *   USE_SharpZipLib define at the top
 * - The serializer uses different types when it comes to store the values. Since my data values
 *   are all of type string, the serializer will "try" which format fits best. The order is: int,
 *   float, double, bool, string. It's not the most efficient way but for a moderate amount of data
 *   it should work on all platforms.
 *
 *  2021-26-5 Update:
 *  Renamed to avoid name collisions with other libraries that include a copy of SimpleJSON
 * * * * */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facebook.WitAi.Data.Entities;
using Facebook.WitAi.Data.Intents;


namespace Facebook.WitAi.Lib
{
    public enum JSONBinaryTag
    {
        Array = 1,
        Class = 2,
        Value = 3,
        IntValue = 4,
        DoubleValue = 5,
        BoolValue = 6,
        FloatValue = 7,
    }

    public class WitResponseNode
    {
        #region common interface

        public virtual void Add(string aKey, WitResponseNode aItem)
        {
        }

        public virtual WitResponseNode this[int aIndex]
        {
            get { return null; }
            set { }
        }

        public virtual WitResponseNode this[string aKey]
        {
            get { return null; }
            set { }
        }

        public virtual string Value
        {
            get { return ""; }
            set { }
        }

        public virtual int Count
        {
            get { return 0; }
        }

        public virtual void Add(WitResponseNode aItem)
        {
            Add("", aItem);
        }

        public virtual WitResponseNode Remove(string aKey)
        {
            return null;
        }

        public virtual WitResponseNode Remove(int aIndex)
        {
            return null;
        }

        public virtual WitResponseNode Remove(WitResponseNode aNode)
        {
            return aNode;
        }

        public virtual IEnumerable<WitResponseNode> Childs
        {
            get { yield break; }
        }

        public IEnumerable<WitResponseNode> DeepChilds
        {
            get
            {
                foreach (var C in Childs)
                foreach (var D in C.DeepChilds)
                    yield return D;
            }
        }

        public override string ToString()
        {
            return "JSONNode";
        }

        public virtual string ToString(string aPrefix)
        {
            return "JSONNode";
        }

        #endregion common interface

        #region typecasting properties

        public virtual int AsInt
        {
            get
            {
                if (int.TryParse(Value, out int v))
                    return v;
                return 0;
            }
            set { Value = value.ToString(); }
        }

        public virtual float AsFloat
        {
            get
            {
                if (float.TryParse(Value, out float v))
                    return v;
                return 0.0f;
            }
            set { Value = value.ToString(); }
        }

        public virtual double AsDouble
        {
            get
            {
                if (double.TryParse(Value, out double v))
                    return v;
                return 0.0;
            }
            set { Value = value.ToString(); }
        }

        public virtual bool AsBool
        {
            get
            {
                if (bool.TryParse(Value, out bool v))
                    return v;
                return !string.IsNullOrEmpty(Value);
            }
            set { Value = (value) ? "true" : "false"; }
        }

        public virtual WitResponseArray AsArray
        {
            get { return this as WitResponseArray; }
        }

        public virtual string[] AsStringArray
        {
            get
            {
                string[] array = new string[0];
                var jsonArray = AsArray;
                if (null != jsonArray)
                {
                    array = new string[jsonArray.Count];
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = jsonArray[i].Value;
                    }
                }

                return array;
            }
        }

        public virtual WitResponseClass AsObject
        {
            get { return this as WitResponseClass; }
        }

        public virtual WitEntityData AsWitEntity => new WitEntityData(this);
        public virtual WitEntityFloatData AsWitFloatEntity => new WitEntityFloatData(this);
        public virtual WitEntityIntData AsWitIntEntity => new WitEntityIntData(this);

        public virtual WitIntentData AsWitIntent => new WitIntentData(this);

        #endregion typecasting properties

        #region operators

        public static implicit operator WitResponseNode(string s)
        {
            return new WitResponseData(s);
        }

        public static implicit operator string(WitResponseNode d)
        {
            return d?.Value;
        }

        public static bool operator ==(WitResponseNode a, object b)
        {
            if (b == null && a is WitResponseLazyCreator)
                return true;
            return System.Object.ReferenceEquals(a, b);
        }

        public static bool operator !=(WitResponseNode a, object b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return System.Object.ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }


        #endregion operators

        internal static string Escape(string aText)
        {
            string result = "";
            foreach (char c in aText)
            {
                switch (c)
                {
                    case '\\':
                        result += "\\\\";
                        break;
                    case '\"':
                        result += "\\\"";
                        break;
                    case '\n':
                        result += "\\n";
                        break;
                    case '\r':
                        result += "\\r";
                        break;
                    case '\t':
                        result += "\\t";
                        break;
                    case '\b':
                        result += "\\b";
                        break;
                    case '\f':
                        result += "\\f";
                        break;
                    default:
                        result += c;
                        break;
                }
            }

            return result;
        }

        public static WitResponseNode Parse(string aJSON)
        {
            Stack<WitResponseNode> stack = new Stack<WitResponseNode>();
            WitResponseNode ctx = null;
            int i = 0;
            string Token = "";
            string TokenName = "";
            bool QuoteMode = false;
            while (i < aJSON.Length)
            {
                switch (aJSON[i])
                {
                    case '{':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }

                        stack.Push(new WitResponseClass());
                        if (ctx != null)
                        {
                            TokenName = TokenName.Trim();
                            if (ctx is WitResponseArray)
                                ctx.Add(stack.Peek());
                            else if (TokenName != "")
                                ctx.Add(TokenName, stack.Peek());
                        }

                        TokenName = "";
                        Token = "";
                        ctx = stack.Peek();
                        break;

                    case '[':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }

                        stack.Push(new WitResponseArray());
                        if (ctx != null)
                        {
                            TokenName = TokenName.Trim();
                            if (ctx is WitResponseArray)
                                ctx.Add(stack.Peek());
                            else if (TokenName != "")
                                ctx.Add(TokenName, stack.Peek());
                        }

                        TokenName = "";
                        Token = "";
                        ctx = stack.Peek();
                        break;

                    case '}':
                    case ']':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }

                        if (stack.Count == 0)
                            throw new JSONParseException("JSON Parse: Too many closing brackets");

                        stack.Pop();
                        if (Token != "")
                        {
                            TokenName = TokenName.Trim();
                            if (ctx is WitResponseArray)
                                ctx.Add(Token);
                            else if (TokenName != "")
                                ctx.Add(TokenName, Token);
                        }

                        TokenName = "";
                        Token = "";
                        if (stack.Count > 0)
                            ctx = stack.Peek();
                        break;

                    case ':':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }

                        TokenName = Token;
                        Token = "";
                        break;

                    case '"':
                        QuoteMode ^= true;
                        break;

                    case ',':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }

                        if (Token != "")
                        {
                            if (ctx is WitResponseArray)
                                ctx.Add(Token);
                            else if (TokenName != "")
                                ctx.Add(TokenName, Token);
                        }

                        TokenName = "";
                        Token = "";
                        break;

                    case '\r':
                    case '\n':
                        break;

                    case ' ':
                    case '\t':
                        if (QuoteMode)
                            Token += aJSON[i];
                        break;

                    case '\\':
                        ++i;
                        if (QuoteMode)
                        {
                            char C = aJSON[i];
                            switch (C)
                            {
                                case 't':
                                    Token += '\t';
                                    break;
                                case 'r':
                                    Token += '\r';
                                    break;
                                case 'n':
                                    Token += '\n';
                                    break;
                                case 'b':
                                    Token += '\b';
                                    break;
                                case 'f':
                                    Token += '\f';
                                    break;
                                case 'u':
                                {
                                    string s = aJSON.Substring(i + 1, 4);
                                    Token += (char) int.Parse(s,
                                        System.Globalization.NumberStyles.AllowHexSpecifier);
                                    i += 4;
                                    break;
                                }
                                default:
                                    Token += C;
                                    break;
                            }
                        }

                        break;

                    default:
                        Token += aJSON[i];
                        break;
                }

                ++i;
            }

            if (QuoteMode)
            {
                throw new JSONParseException("JSON Parse: Quotation marks seems to be messed up.");
            }

            return ctx;
        }

        public virtual void Serialize(System.IO.BinaryWriter aWriter)
        {
        }

        public void SaveToStream(System.IO.Stream aData)
        {
            var W = new System.IO.BinaryWriter(aData);
            Serialize(W);
        }

#if USE_SharpZipLib
        public void SaveToCompressedStream(System.IO.Stream aData)
        {
            using (var gzipOut = new ICSharpCode.SharpZipLib.BZip2.BZip2OutputStream(aData))
            {
                gzipOut.IsStreamOwner = false;
                SaveToStream(gzipOut);
                gzipOut.Close();
            }
        }

        public void SaveToCompressedFile(string aFileName)
        {
            #if USE_FileIO
            System.IO.Directory.CreateDirectory((new System.IO.FileInfo(aFileName)).Directory.FullName);
            using(var F = System.IO.File.OpenWrite(aFileName))
            {
                SaveToCompressedStream(F);
            }
            #else
            throw new Exception("Can't use File IO stuff in webplayer");
            #endif
        }
        public string SaveToCompressedBase64()
        {
            using (var stream = new System.IO.MemoryStream())
            {
                SaveToCompressedStream(stream);
                stream.Position = 0;
                return System.Convert.ToBase64String(stream.ToArray());
            }
        }

#else
        public void SaveToCompressedStream(System.IO.Stream aData)
        {
            throw new Exception(
                "Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }

        public void SaveToCompressedFile(string aFileName)
        {
            throw new Exception(
                "Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }

        public string SaveToCompressedBase64()
        {
            throw new Exception(
                "Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
#endif

        public void SaveToFile(string aFileName)
        {
#if USE_FileIO
            System.IO.Directory.CreateDirectory((new System.IO.FileInfo(aFileName)).Directory
                .FullName);
            using (var F = System.IO.File.OpenWrite(aFileName))
            {
                SaveToStream(F);
            }
#else
            throw new Exception("Can't use File IO stuff in webplayer");
#endif
        }

        public string SaveToBase64()
        {
            using (var stream = new System.IO.MemoryStream())
            {
                SaveToStream(stream);
                stream.Position = 0;
                return System.Convert.ToBase64String(stream.ToArray());
            }
        }

        public static WitResponseNode Deserialize(System.IO.BinaryReader aReader)
        {
            JSONBinaryTag type = (JSONBinaryTag) aReader.ReadByte();
            switch (type)
            {
                case JSONBinaryTag.Array:
                {
                    int count = aReader.ReadInt32();
                    WitResponseArray tmp = new WitResponseArray();
                    for (int i = 0; i < count; i++)
                        tmp.Add(Deserialize(aReader));
                    return tmp;
                }
                case JSONBinaryTag.Class:
                {
                    int count = aReader.ReadInt32();
                    WitResponseClass tmp = new WitResponseClass();
                    for (int i = 0; i < count; i++)
                    {
                        string key = aReader.ReadString();
                        var val = Deserialize(aReader);
                        tmp.Add(key, val);
                    }

                    return tmp;
                }
                case JSONBinaryTag.Value:
                {
                    return new WitResponseData(aReader.ReadString());
                }
                case JSONBinaryTag.IntValue:
                {
                    return new WitResponseData(aReader.ReadInt32());
                }
                case JSONBinaryTag.DoubleValue:
                {
                    return new WitResponseData(aReader.ReadDouble());
                }
                case JSONBinaryTag.BoolValue:
                {
                    return new WitResponseData(aReader.ReadBoolean());
                }
                case JSONBinaryTag.FloatValue:
                {
                    return new WitResponseData(aReader.ReadSingle());
                }

                default:
                {
                    throw new JSONParseException("Error deserializing JSON. Unknown tag: " + type);
                }
            }
        }

#if USE_SharpZipLib
        public static JSONNode LoadFromCompressedStream(System.IO.Stream aData)
        {
            var zin = new ICSharpCode.SharpZipLib.BZip2.BZip2InputStream(aData);
            return LoadFromStream(zin);
        }
        public static JSONNode LoadFromCompressedFile(string aFileName)
        {
            #if USE_FileIO
            using(var F = System.IO.File.OpenRead(aFileName))
            {
                return LoadFromCompressedStream(F);
            }
            #else
            throw new Exception("Can't use File IO stuff in webplayer");
            #endif
        }
        public static JSONNode LoadFromCompressedBase64(string aBase64)
        {
            var tmp = System.Convert.FromBase64String(aBase64);
            var stream = new System.IO.MemoryStream(tmp);
            stream.Position = 0;
            return LoadFromCompressedStream(stream);
        }
#else
        public static WitResponseNode LoadFromCompressedFile(string aFileName)
        {
            throw new Exception(
                "Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }

        public static WitResponseNode LoadFromCompressedStream(System.IO.Stream aData)
        {
            throw new Exception(
                "Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }

        public static WitResponseNode LoadFromCompressedBase64(string aBase64)
        {
            throw new Exception(
                "Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
#endif

        public static WitResponseNode LoadFromStream(System.IO.Stream aData)
        {
            using (var R = new System.IO.BinaryReader(aData))
            {
                return Deserialize(R);
            }
        }

        public static WitResponseNode LoadFromFile(string aFileName)
        {
#if USE_FileIO
            using (var F = System.IO.File.OpenRead(aFileName))
            {
                return LoadFromStream(F);
            }
#else
            throw new Exception("Can't use File IO stuff in webplayer");
#endif
        }

        public static WitResponseNode LoadFromBase64(string aBase64)
        {
            var tmp = System.Convert.FromBase64String(aBase64);
            var stream = new System.IO.MemoryStream(tmp)
            {
                Position = 0
            };
            return LoadFromStream(stream);
        }
    } // End of JSONNode

    public class WitResponseArray : WitResponseNode, IEnumerable
    {
        private List<WitResponseNode> m_List = new List<WitResponseNode>();

        public override WitResponseNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= m_List.Count)
                    return new WitResponseLazyCreator(this);
                return m_List[aIndex];
            }
            set
            {
                if (aIndex < 0 || aIndex >= m_List.Count)
                    m_List.Add(value);
                else
                    m_List[aIndex] = value;
            }
        }

        public override WitResponseNode this[string aKey]
        {
            get { return new WitResponseLazyCreator(this); }
            set { m_List.Add(value); }
        }

        public override int Count
        {
            get { return m_List.Count; }
        }

        public override void Add(string aKey, WitResponseNode aItem)
        {
            m_List.Add(aItem);
        }

        public override WitResponseNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= m_List.Count)
                return null;
            WitResponseNode tmp = m_List[aIndex];
            m_List.RemoveAt(aIndex);
            return tmp;
        }

        public override WitResponseNode Remove(WitResponseNode aNode)
        {
            m_List.Remove(aNode);
            return aNode;
        }

        public override IEnumerable<WitResponseNode> Childs
        {
            get
            {
                foreach (WitResponseNode N in m_List)
                    yield return N;
            }
        }

        public IEnumerator GetEnumerator()
        {
            foreach (WitResponseNode N in m_List)
                yield return N;
        }

        public override string ToString()
        {
            string result = "[ ";
            foreach (WitResponseNode N in m_List)
            {
                if (result.Length > 2)
                    result += ", ";
                result += N.ToString();
            }

            result += " ]";
            return result;
        }

        public override string ToString(string aPrefix)
        {
            string result = "[ ";
            foreach (WitResponseNode N in m_List)
            {
                if (result.Length > 3)
                    result += ", ";
                result += "\n" + aPrefix + "   ";
                result += N.ToString(aPrefix + "   ");
            }

            result += "\n" + aPrefix + "]";
            return result;
        }

        public override void Serialize(System.IO.BinaryWriter aWriter)
        {
            aWriter.Write((byte) JSONBinaryTag.Array);
            aWriter.Write(m_List.Count);
            for (int i = 0; i < m_List.Count; i++)
            {
                m_List[i].Serialize(aWriter);
            }
        }
    } // End of JSONArray

    public class WitResponseClass : WitResponseNode, IEnumerable
    {
        private Dictionary<string, WitResponseNode> m_Dict = new Dictionary<string, WitResponseNode>();

        public string[] ChildNodeNames => m_Dict.Keys.ToArray();

        public bool HasChild(string child) => m_Dict.ContainsKey(child);

        public override WitResponseNode this[string aKey]
        {
            get
            {
                if (m_Dict.ContainsKey(aKey))
                    return m_Dict[aKey];
                else
                    return new WitResponseLazyCreator(this, aKey);
            }
            set
            {
                if (m_Dict.ContainsKey(aKey))
                    m_Dict[aKey] = value;
                else
                    m_Dict.Add(aKey, value);
            }
        }

        public override WitResponseNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= m_Dict.Count)
                    return null;
                return m_Dict.ElementAt(aIndex).Value;
            }
            set
            {
                if (aIndex < 0 || aIndex >= m_Dict.Count)
                    return;
                string key = m_Dict.ElementAt(aIndex).Key;
                m_Dict[key] = value;
            }
        }

        public override int Count
        {
            get { return m_Dict.Count; }
        }


        public override void Add(string aKey, WitResponseNode aItem)
        {
            if (!string.IsNullOrEmpty(aKey))
            {
                if (m_Dict.ContainsKey(aKey))
                    m_Dict[aKey] = aItem;
                else
                    m_Dict.Add(aKey, aItem);
            }
            else
                m_Dict.Add(Guid.NewGuid().ToString(), aItem);
        }

        public override WitResponseNode Remove(string aKey)
        {
            if (!m_Dict.ContainsKey(aKey))
                return null;
            WitResponseNode tmp = m_Dict[aKey];
            m_Dict.Remove(aKey);
            return tmp;
        }

        public override WitResponseNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= m_Dict.Count)
                return null;
            var item = m_Dict.ElementAt(aIndex);
            m_Dict.Remove(item.Key);
            return item.Value;
        }

        public override WitResponseNode Remove(WitResponseNode aNode)
        {
            try
            {
                var item = m_Dict.Where(k => k.Value == aNode).First();
                m_Dict.Remove(item.Key);
                return aNode;
            }
            catch
            {
                return null;
            }
        }

        public override IEnumerable<WitResponseNode> Childs
        {
            get
            {
                foreach (KeyValuePair<string, WitResponseNode> N in m_Dict)
                    yield return N.Value;
            }
        }

        public IEnumerator GetEnumerator()
        {
            foreach (KeyValuePair<string, WitResponseNode> N in m_Dict)
                yield return N;
        }

        public override string ToString()
        {
            string result = "{";
            foreach (KeyValuePair<string, WitResponseNode> N in m_Dict)
            {
                if (result.Length > 2)
                    result += ", ";
                result += "\"" + Escape(N.Key) + "\":" + N.Value.ToString();
            }

            result += "}";
            return result;
        }

        public override string ToString(string aPrefix)
        {
            string result = "{ ";
            foreach (KeyValuePair<string, WitResponseNode> N in m_Dict)
            {
                if (result.Length > 3)
                    result += ", ";
                result += "\n" + aPrefix + "   ";
                result += "\"" + Escape(N.Key) + "\" : " + N.Value.ToString(aPrefix + "   ");
            }

            result += "\n" + aPrefix + "}";
            return result;
        }

        public override void Serialize(System.IO.BinaryWriter aWriter)
        {
            aWriter.Write((byte) JSONBinaryTag.Class);
            aWriter.Write(m_Dict.Count);
            foreach (string K in m_Dict.Keys)
            {
                aWriter.Write(K);
                m_Dict[K].Serialize(aWriter);
            }
        }
    } // End of JSONClass

    public class WitResponseData : WitResponseNode
    {
        private string m_Data;

        public override string Value
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        public WitResponseData(string aData)
        {
            m_Data = aData;
        }

        public WitResponseData(float aData)
        {
            AsFloat = aData;
        }

        public WitResponseData(double aData)
        {
            AsDouble = aData;
        }

        public WitResponseData(bool aData)
        {
            AsBool = aData;
        }

        public WitResponseData(int aData)
        {
            AsInt = aData;
        }

        public override string ToString()
        {
            return "\"" + Escape(m_Data) + "\"";
        }

        public override string ToString(string aPrefix)
        {
            return "\"" + Escape(m_Data) + "\"";
        }

        public override void Serialize(System.IO.BinaryWriter aWriter)
        {
            var tmp = new WitResponseData("")
            {
                AsInt = AsInt
            };
            if (tmp.m_Data == this.m_Data)
            {
                aWriter.Write((byte) JSONBinaryTag.IntValue);
                aWriter.Write(AsInt);
                return;
            }

            tmp.AsFloat = AsFloat;
            if (tmp.m_Data == this.m_Data)
            {
                aWriter.Write((byte) JSONBinaryTag.FloatValue);
                aWriter.Write(AsFloat);
                return;
            }

            tmp.AsDouble = AsDouble;
            if (tmp.m_Data == this.m_Data)
            {
                aWriter.Write((byte) JSONBinaryTag.DoubleValue);
                aWriter.Write(AsDouble);
                return;
            }

            tmp.AsBool = AsBool;
            if (tmp.m_Data == this.m_Data)
            {
                aWriter.Write((byte) JSONBinaryTag.BoolValue);
                aWriter.Write(AsBool);
                return;
            }

            aWriter.Write((byte) JSONBinaryTag.Value);
            aWriter.Write(m_Data);
        }
    } // End of JSONData

    internal class WitResponseLazyCreator : WitResponseNode
    {
        private WitResponseNode m_Node = null;
        private string m_Key = null;

        public WitResponseLazyCreator(WitResponseNode aNode)
        {
            m_Node = aNode;
            m_Key = null;
        }

        public WitResponseLazyCreator(WitResponseNode aNode, string aKey)
        {
            m_Node = aNode;
            m_Key = aKey;
        }

        private void Set(WitResponseNode aVal)
        {
            if (m_Key == null)
            {
                m_Node.Add(aVal);
            }
            else
            {
                m_Node.Add(m_Key, aVal);
            }
            m_Node = null; // Be GC friendly.
        }

        public override WitResponseNode this[int aIndex]
        {
            get { return new WitResponseLazyCreator(this); }
            set
            {
                var tmp = new WitResponseArray
                {
                    value
                };
                Set(tmp);
            }
        }

        public override WitResponseNode this[string aKey]
        {
            get { return new WitResponseLazyCreator(this, aKey); }
            set
            {
                var tmp = new WitResponseClass
                {
                    { aKey, value }
                };
                Set(tmp);
            }
        }

        public override void Add(WitResponseNode aItem)
        {
            var tmp = new WitResponseArray
            {
                aItem
            };
            Set(tmp);
        }

        public override void Add(string aKey, WitResponseNode aItem)
        {
            var tmp = new WitResponseClass
            {
                { aKey, aItem }
            };
            Set(tmp);
        }

        public static bool operator ==(WitResponseLazyCreator a, object b)
        {
            if (b == null)
                return true;
            return System.Object.ReferenceEquals(a, b);
        }

        public static bool operator !=(WitResponseLazyCreator a, object b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return true;
            return System.Object.ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return "";
        }

        public override string ToString(string aPrefix)
        {
            return "";
        }

        public override int AsInt
        {
            get
            {
                WitResponseData tmp = new WitResponseData(0);
                Set(tmp);
                return 0;
            }
            set
            {
                WitResponseData tmp = new WitResponseData(value);
                Set(tmp);
            }
        }

        public override float AsFloat
        {
            get
            {
                WitResponseData tmp = new WitResponseData(0.0f);
                Set(tmp);
                return 0.0f;
            }
            set
            {
                WitResponseData tmp = new WitResponseData(value);
                Set(tmp);
            }
        }

        public override double AsDouble
        {
            get
            {
                WitResponseData tmp = new WitResponseData(0.0);
                Set(tmp);
                return 0.0;
            }
            set
            {
                WitResponseData tmp = new WitResponseData(value);
                Set(tmp);
            }
        }

        public override bool AsBool
        {
            get
            {
                WitResponseData tmp = new WitResponseData(false);
                Set(tmp);
                return false;
            }
            set
            {
                WitResponseData tmp = new WitResponseData(value);
                Set(tmp);
            }
        }

        public override WitResponseArray AsArray
        {
            get
            {
                WitResponseArray tmp = new WitResponseArray();
                Set(tmp);
                return tmp;
            }
        }

        public override WitResponseClass AsObject
        {
            get
            {
                WitResponseClass tmp = new WitResponseClass();
                Set(tmp);
                return tmp;
            }
        }
    } // End of JSONLazyCreator

    public static class WitResponseJson
    {
        public static WitResponseNode Parse(string aJSON)
        {
            return WitResponseNode.Parse(aJSON);
        }
    }

    public class JSONParseException : Exception
    {
        public JSONParseException(string message) : base(message) { }
    }
}
