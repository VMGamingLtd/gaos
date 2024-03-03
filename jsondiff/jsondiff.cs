#pragma warning disable 8600, 8601, 8618, 8602, 8604, 8603

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace jsondiff
{
    public enum DiffType
    {
        Add,
        Remove,
        Replace,
        MergeObjects,
        MergeArrays
    }

    public class DiffValue
    {
        public DiffType Type { get; set; }
        public JToken Value { get; set; }
        public List<DiffValueAtProperty> ObjectDiff { get; set; }
        public List<DiffValueAtIndex> ArrayDiff { get; set; }
    }

    public class DiffValueAtProperty
    {
        public string Property { get; set; }
        public DiffValue Diff { get; set; }
    }

    public class DiffValueAtIndex
    {
        public int Index { get; set; }
        public DiffValue Diff { get; set; }
    }

    // custom converter for DiffValueAtProperty

    public class DiffValueAtPropertyConverter : JsonConverter<DiffValueAtProperty>
    {
        public override DiffValueAtProperty ReadJson(JsonReader reader, Type objectType, DiffValueAtProperty existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var diffValueAtProperty = new DiffValueAtProperty();
            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonSerializationException("DiffValueAtProperty: expecting start object");
            }

            while (true)
            {
                reader.Read();
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType != JsonToken.PropertyName)
                {
                    throw new JsonSerializationException("DiffValueAtProperty: expecting property name");
                }
                diffValueAtProperty.Property = reader.Value.ToString();
               

                reader.Read();
                diffValueAtProperty.Diff = serializer.Deserialize<DiffValue>(reader);

            }

            return diffValueAtProperty;
        }



        public override void WriteJson(JsonWriter writer, DiffValueAtProperty value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(value.Property);
            serializer.Serialize(writer, value.Diff);

            writer.WriteEndObject();
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
    }

    // create custom converter for DiffValueAtIndex

    public class DiffValueAtIndexConverter : JsonConverter<DiffValueAtIndex>
    {
        public override DiffValueAtIndex ReadJson(JsonReader reader, Type objectType, DiffValueAtIndex existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var diffValueAtIndex = new DiffValueAtIndex();
            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonSerializationException("DiffValueAtIndex: expecting start object");
            }

            while (true)
            {
                reader.Read();
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType != JsonToken.PropertyName)
                {
                    throw new JsonSerializationException("DiffValueAtIndex: expecting property name");
                }
                var propertyName = reader.Value.ToString();
                try
                {
                    diffValueAtIndex.Index = int.Parse(propertyName);
                } catch {
                    throw new JsonSerializationException("DiffValueAtIndex: Invalid index");
                }

                reader.Read();
                diffValueAtIndex.Diff = serializer.Deserialize<DiffValue>(reader);

            }

            return diffValueAtIndex;
        }

        public override void WriteJson(JsonWriter writer, DiffValueAtIndex value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(value.Index.ToString());
            serializer.Serialize(writer, value.Diff);

            writer.WriteEndObject();
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
    }

    public class DiffValueConverter : JsonConverter<DiffValue>
    {
        private string DiffTypeToString(DiffType diffType)
        {
            switch (diffType)
            {
                case DiffType.Add:
                    return "ad";
                case DiffType.Remove:
                    return "rm";
                case DiffType.Replace:
                    return "rp";
                case DiffType.MergeObjects:
                    return "mo";
                case DiffType.MergeArrays:
                    return "ma";
                default:
                    throw new JsonSerializationException("Invalid DiffType");
            }
        }

        private DiffType StringToDiffType(string diffType)
        {
            switch (diffType)
            {
                case "ad":
                    return DiffType.Add;
                case "rm":
                    return DiffType.Remove;
                case "rp":
                    return DiffType.Replace;
                case "mo":
                    return DiffType.MergeObjects;
                case "ma":
                    return DiffType.MergeArrays;
                default:
                    throw new JsonSerializationException("Invalid DiffType");
            }
        }

        public override DiffValue ReadJson(JsonReader reader, Type objectType, DiffValue existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var diffValue = new DiffValue();
            if (reader.TokenType == JsonToken.StartObject)
            {
                reader.Read();


                if (reader.TokenType != JsonToken.PropertyName)
                {
                    throw new JsonSerializationException("error while reading DiffValue: expecting property name");
                }

                var propertyName = reader.Value.ToString();
                diffValue.Type = StringToDiffType(propertyName);

                switch (diffValue.Type)
                {
                    case DiffType.Add:
                        reader.Read();
                        diffValue.Value = serializer.Deserialize<JToken>(reader);
                        break;
                    case DiffType.Remove:
                        reader.Read();
                        diffValue.Value = serializer.Deserialize<JToken>(reader);
                        break;
                    case DiffType.Replace:
                        reader.Read();
                        diffValue.Value = serializer.Deserialize<JToken>(reader);
                        break;
                    case DiffType.MergeObjects:
                        reader.Read();
                        diffValue.ObjectDiff = serializer.Deserialize<List<DiffValueAtProperty>>(reader);
                        break;
                    case DiffType.MergeArrays:
                        reader.Read();
                        diffValue.ArrayDiff = serializer.Deserialize<List<DiffValueAtIndex>>(reader);
                        break;
                    default:
                        throw new JsonSerializationException($"Invalid DiffType: {propertyName}");
                }

                reader.Read();
                if (reader.TokenType != JsonToken.EndObject)
                {
                    throw new JsonSerializationException("error while reading DiffValue: expecting end object");
                }
            } 
            else
            {
                throw new JsonSerializationException("error while reading DiffValue: expecting start object");
            }
            return diffValue;
        }

        public override void WriteJson(JsonWriter writer, DiffValue value, JsonSerializer serializer)
        {
            if (value.Type == DiffType.Add || value.Type == DiffType.Replace)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(DiffTypeToString(value.Type));
                serializer.Serialize(writer, value.Value);
                writer.WriteEndObject();
            }
            else if (value.Type == DiffType.Remove)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(DiffTypeToString(value.Type));
                serializer.Serialize(writer, null);
                writer.WriteEndObject();
            }
            else if (value.Type == DiffType.MergeObjects)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(DiffTypeToString(value.Type));
                serializer.Serialize(writer, value.ObjectDiff);
                writer.WriteEndObject();
            }
            else if (value.Type == DiffType.MergeArrays)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(DiffTypeToString(value.Type));
                serializer.Serialize(writer, value.ArrayDiff);
                writer.WriteEndObject();
            }
            else
            {
                throw new JsonSerializationException("Invalid DiffType");
            }

        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
    }

    public class Difference
    {
        private static DiffValueAtProperty CompareObjectValueAtProperty(JToken valA, JToken valB, string prop)
        {
            var objA = (JObject)valA;
            var objB = (JObject)valB;

            if (objA[prop] == null)
            {
                var diff = new DiffValue
                {
                    Type = DiffType.Add,
                    Value = objB[prop]
                };
                return new DiffValueAtProperty { Property = prop, Diff = diff };
            }

            if (objB[prop] == null)
            {
                var diff = new DiffValue
                {
                    Type = DiffType.Remove,
                };
                return new DiffValueAtProperty { Property = prop, Diff = diff };
            }

            if (objA[prop].Type != objB[prop].Type)
            {
                var diff = new DiffValue
                {
                    Type = DiffType.Replace,
                    Value = objB[prop],
                };
                return new DiffValueAtProperty { Property = prop, Diff = diff };
            }
            else
            {
                if (objA[prop].Type == JTokenType.Object)
                {
                    var diffs = CompareObjectValues(objA[prop], objB[prop]);
                    if (diffs == null)
                    {
                        return null;
                    }
                    var diff = new DiffValue
                    {
                        Type = DiffType.MergeObjects,
                        ObjectDiff = diffs

                    };
                    return new DiffValueAtProperty { Property = prop, Diff = diff };
                }
                else if (objA[prop].Type == JTokenType.Array)
                {
                    var diffs = CompareArrayValues(objA[prop], objB[prop]);
                    if (diffs == null)
                    {
                        return null;
                    }
                    var diff = new DiffValue
                    {
                        Type = DiffType.MergeArrays,
                        ArrayDiff = diffs

                    };
                    return new DiffValueAtProperty { Property = prop, Diff = diff };
                }
                else
                {
                    if (objA[prop].ToString() != objB[prop].ToString())
                    {
                        var diff = new DiffValue
                        {
                            Type = DiffType.Replace,
                            Value = objB[prop],
                        };
                        return new DiffValueAtProperty { Property = prop, Diff = diff };
                    }
                    else
                    {
                        return null;
                    }
                }
            }

        }

        private static DiffValueAtIndex CompareArrayValueAtIndex(JToken valA, JToken valB, int index)
        {
            var arrA = (JArray)valA;
            var arrB = (JArray)valB;

            if (index >= arrA.Count)
            {
                var diff = new DiffValue
                {
                    Type = DiffType.Add,
                    Value = arrB[index]
                };
                return new DiffValueAtIndex { Index = index, Diff = diff };
            }
            else
            {
                if (index >= arrB.Count)
                {
                    var diff = new DiffValue
                    {
                        Type = DiffType.Remove,
                    };
                    return new DiffValueAtIndex { Index = index, Diff = diff };
                }
                else
                {
                    if (arrA[index].Type != arrB[index].Type)
                    {
                        var diff = new DiffValue
                        {
                            Type = DiffType.Replace,
                            Value = arrB[index],
                        };
                        return new DiffValueAtIndex { Index = index, Diff = diff };
                    }
                    else
                    {
                        if (arrA[index].Type == JTokenType.Object)
                        {
                            var diffs = CompareObjectValues(arrA[index], arrB[index]);
                            if (diffs == null)
                            {
                                return null;
                            }
                            var diff = new DiffValue
                            {
                                Type = DiffType.MergeObjects,
                                ObjectDiff = diffs

                            };
                            return new DiffValueAtIndex { Index = index, Diff = diff };
                        }
                        else if (arrA[index].Type == JTokenType.Array)
                        {
                            var diffs = CompareArrayValues(arrA[index], arrB[index]);
                            if (diffs == null)
                            {
                                return null;
                            }
                            var diff = new DiffValue
                            {
                                Type = DiffType.MergeArrays,
                                ArrayDiff = diffs

                            };
                            return new DiffValueAtIndex { Index = index, Diff = diff };
                        }
                        else
                        {
                            if (arrA[index].ToString() != arrB[index].ToString())
                            {
                                var diff = new DiffValue
                                {
                                    Type = DiffType.Replace,
                                    Value = arrB[index],
                                };
                                return new DiffValueAtIndex { Index = index, Diff = diff };
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
            }

        }

        private static List<DiffValueAtProperty> CompareObjectValues(JToken valA, JToken valB)
        {
            List<DiffValueAtProperty> diffs = new List<DiffValueAtProperty>();


            var objA = (JObject)valA;
            var objB = (JObject)valB;

            var propertiesUnion = objA.Properties().Union(objB.Properties());
            // remove duplicates
            var properties = propertiesUnion.GroupBy(x => x.Name).Select(x => x.First());


            foreach (var property in properties)
            {
                var diff = CompareObjectValueAtProperty(valA, valB, property.Name);
                if (diff != null)
                {
                    diffs.Add(diff);
                }

            }

            if (diffs.Count == 0)
            {
                return null;
            }

            return diffs;
        }

        private static List<DiffValueAtIndex> CompareArrayValues(JToken valA, JToken valB)
        {
            List<DiffValueAtIndex> diffs = new List<DiffValueAtIndex>();

            var arrA = (JArray)valA;
            var arrB = (JArray)valB;

            // Make a list of the properties in either object
            var count = Math.Max(arrA.Count, arrB.Count);

            for (int i = 0; i < count; i++)
            {
                var diff = CompareArrayValueAtIndex(valA, valB, i);
                if (diff != null)
                {
                    diffs.Add(diff);
                }
            }

            if (diffs.Count == 0)
            {
                return null;
            }

            return diffs;
        }

        // Compute diff = A - B
        public static DiffValue CompareValues(JToken valA, JToken valB)
        {
            if (valA.Type != valB.Type)
            {
                var diff = new DiffValue
                {
                    Type = DiffType.Replace,
                    Value = valB,
                };
                return diff;
            }
            else
            {
                if (valA.Type == JTokenType.Object)
                {
                    var diffs = CompareObjectValues(valA, valB);
                    if (diffs == null)
                    {
                        return null;
                    }
                    var diff = new DiffValue
                    {
                        Type = DiffType.MergeObjects,
                        ObjectDiff = diffs

                    };
                    return diff;
                }
                else if (valA.Type == JTokenType.Array)
                {
                    var diffs = CompareArrayValues(valA, valB);
                    if (diffs == null)
                    {
                        return null;
                    }
                    var diff = new DiffValue
                    {
                        Type = DiffType.MergeArrays,
                        ArrayDiff = diffs

                    };
                    return diff;
                }
                else
                {
                    if (valA.ToString() != valB.ToString())
                    {
                        var diff = new DiffValue
                        {
                            Type = DiffType.Replace,
                            Value = valB,
                        };
                        return diff;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public class IsEqualResult
        {
            public bool IsEqual { get; set; }
            public string PropertyPath { get; set; }
        }

        private static IsEqualResult IsEqualArrayValues(JToken valA, JToken valB, string propertyPath)
        {
            var arrA = (JArray)valA;
            var arrB = (JArray)valB;

            var count = Math.Max(arrA.Count, arrB.Count);

            for (int i = 0; i < count; i++)
            {
                if (i >= arrA.Count)
                {
                    return new IsEqualResult { IsEqual = false, PropertyPath = propertyPath + $"[{i}]" };
                }
                if (i >= arrB.Count)
                {
                    return new IsEqualResult { IsEqual = false, PropertyPath = propertyPath + $"[{i}]" };
                }
                if (arrA[i].Type != arrB[i].Type)
                {
                    return new IsEqualResult { IsEqual = false, PropertyPath = propertyPath + $"[{i}]" };
                }
                if (arrA[i].Type == JTokenType.Object)
                {
                    var result = IsEqualObjectValues(arrA[i], arrB[i], propertyPath + $"[{i}]");
                    if (!result.IsEqual)
                    {
                        return result;
                    }
                }
                else if (arrA[i].Type == JTokenType.Array)
                {
                    var result = IsEqualArrayValues(arrA[i], arrB[i], propertyPath + $"[{i}]");
                    if (!result.IsEqual)
                    {
                        return result;
                    }
                }
                else
                {
                    if (arrA[i].ToString() != arrB[i].ToString())
                    {
                        return new IsEqualResult { IsEqual = false, PropertyPath = propertyPath + $"[{i}]" };
                    }
                }
            }
            return new IsEqualResult { IsEqual = true, PropertyPath = propertyPath };
        }

        private static IsEqualResult IsEqualObjectValues(JToken valA, JToken valB, string propertyPath)
        {

            var objA = (JObject)valA;
            var objB = (JObject)valB;

            var objAProperties = objA.Properties();
            foreach (var property in objAProperties)
            {
                if (objB[property.Name] == null)
                {
                    return new IsEqualResult { IsEqual = false, PropertyPath = propertyPath + "." + property.Name };
                }
                if (objA[property.Name].Type != objB[property.Name].Type)
                {
                    return new IsEqualResult { IsEqual = false, PropertyPath = propertyPath + "." + property.Name };
                }
                if (objA[property.Name].Type == JTokenType.Object)
                {
                    var result = IsEqualObjectValues(objA[property.Name], objB[property.Name], propertyPath + "." + property.Name);
                    if (!result.IsEqual)
                    {
                        return result;
                    }
                }
                else if (objA[property.Name].Type == JTokenType.Array)
                {
                    var result = IsEqualArrayValues(objA[property.Name], objB[property.Name], propertyPath + "." + property.Name);
                    if (!result.IsEqual)
                    {
                        return result;
                    }
                }
                else
                {
                    if (objA[property.Name].ToString() != objB[property.Name].ToString())
                    {
                        return new IsEqualResult { IsEqual = false, PropertyPath = propertyPath + "." + property.Name };
                    }
                }
            }

            var objBProperties = objB.Properties();
            foreach (var property in objBProperties)
            {
                if (objA[property.Name] == null)
                {
                    return new IsEqualResult { IsEqual = false, PropertyPath = propertyPath + "." + property.Name };
                }
            }

            return new IsEqualResult { IsEqual = true, PropertyPath = propertyPath };
        }

        private static IsEqualResult IsEqualValues_(JToken valA, JToken valB, string propertyPath)
        {
            if (valA.Type != valB.Type)
            {
                return new IsEqualResult { IsEqual = false, PropertyPath = propertyPath };
            }
            else
            {
                if (valA.Type == JTokenType.Object)
                {
                    var result = IsEqualObjectValues(valA, valB, propertyPath);
                    return result;
                }
                else if (valA.Type == JTokenType.Array)
                {
                    var result = IsEqualArrayValues(valA, valB, propertyPath);
                    return result;
                }
                else
                {
                    if (valA.ToString() != valB.ToString())
                    {
                        return new IsEqualResult { IsEqual = false, PropertyPath = propertyPath };
                    }
                    else
                    {
                        return new IsEqualResult { IsEqual = true, PropertyPath = propertyPath };
                    }
                }
            }
        }

        public static IsEqualResult IsEqualValues(JToken valA, JToken valB)
        {
            var result = IsEqualValues_(valA, valB, "");
            return result;
        }

        private static JToken MergeArrays(JToken val, List<DiffValueAtIndex> diffs)
        {
            if (val.Type != JTokenType.Array)
            {
                throw new Exception("MergeArrays: expecting array");
            }
            var arr = (JArray)val;
            foreach (var diff in diffs)
            {
                if (diff.Diff.Type == DiffType.Add)
                {
                    arr.Insert(diff.Index, diff.Diff.Value);
                }
                else if (diff.Diff.Type == DiffType.Remove)
                {
                    arr.RemoveAt(diff.Index);
                }
                else if (diff.Diff.Type == DiffType.Replace)
                {
                    arr[diff.Index] = diff.Diff.Value;
                }
                else if (diff.Diff.Type == DiffType.MergeObjects)
                {
                    arr[diff.Index] = MergeObjects(arr[diff.Index], diff.Diff.ObjectDiff);
                }
                else if (diff.Diff.Type == DiffType.MergeArrays)
                {
                    arr[diff.Index] = MergeArrays(arr[diff.Index], diff.Diff.ArrayDiff);
                }
                else
                {
                    throw new Exception("MergeArrays: invalid diff type");
                }
            }
            return arr;
        }


        private static JToken MergeObjects(JToken val, List<DiffValueAtProperty> diffs)
        {
            if (val.Type != JTokenType.Object)
            {
                throw new Exception("MergeObjects: expecting object");
            }

            var obj = (JObject)val;
            foreach (var diff in diffs)
            {
                if (diff.Diff.Type == DiffType.Add)
                {
                    obj[diff.Property] = diff.Diff.Value;
                }
                else if (diff.Diff.Type == DiffType.Remove)
                {
                    obj.Remove(diff.Property);
                }
                else if (diff.Diff.Type == DiffType.Replace)
                {
                    obj[diff.Property] = diff.Diff.Value;
                }
                else if (diff.Diff.Type == DiffType.MergeObjects)
                {
                    obj[diff.Property] = MergeObjects(obj[diff.Property], diff.Diff.ObjectDiff);
                }
                else if (diff.Diff.Type == DiffType.MergeArrays)
                {
                    obj[diff.Property] = MergeArrays(obj[diff.Property], diff.Diff.ArrayDiff);
                }
                else
                {
                    throw new Exception("MergeObjects: invalid diff type");
                }
            }
            return obj;
        }

        public static JToken AddDiff(JToken val, DiffValue diff)
        {
            if (diff == null)
            {
                return val;
            } else if (diff.Type == DiffType.Add)
            {
                return diff.Value;
            }
            else if (diff.Type == DiffType.Remove)
            {
                return null;
            }
            else if (diff.Type == DiffType.Replace)
            {
                return diff.Value;
            }
            else if (diff.Type == DiffType.MergeObjects)
            {
                return MergeObjects(val, diff.ObjectDiff);
            }
            else if (diff.Type == DiffType.MergeArrays)
            {
                return MergeArrays(val, diff.ArrayDiff);
            }
            else
            {
                throw new Exception("AddDiff: invalid diff type");
            }
        }

        public static JsonSerializerSettings GetJsonSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new DiffValueConverter(), new DiffValueAtPropertyConverter(), new DiffValueAtIndexConverter() }
            };
        }


    }

}
