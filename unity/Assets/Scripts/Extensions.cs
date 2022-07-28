using System.Collections.Generic;
using System.Linq;
using System;
 using System.Runtime.Serialization.Formatters.Binary;
 using System.IO;
using MessagePack.Resolvers;
using MessagePack.Formatters;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

public static class ExtensionMethods {
    public static T DeepClone<T>(this T obj)
    {
        // Don't serialize a null object, simply return the default for that object
        if (ReferenceEquals(obj, null)) {
            return default;
        } 

        // initialize inner objects individually
        // for example in default constructor some list property initialized with some values,
        // but in 'source' these items are cleaned -
        // without ObjectCreationHandling.Replace default constructor values will be added to result
        var deserializeSettings = new JsonSerializerSettings {ObjectCreationHandling = ObjectCreationHandling.Replace};

        var jsonResolver = new ShouldSerializeContractResolver();
        var str = Newtonsoft.Json.JsonConvert.SerializeObject(
            obj,
            Newtonsoft.Json.Formatting.None,
            new Newtonsoft.Json.JsonSerializerSettings() {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                ContractResolver = jsonResolver
            }
        );

        var jObj = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(str);

        return jObj.ToObject<T>();
    }

    public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
    {
        TValue value;
        return dictionary.TryGetValue(key, out value) ? value : defaultValue;
    }

    public static int AddCount<TKey>(this Dictionary<TKey, int> dictionary, TKey key, int count = 1)
    {
        int value;
        dictionary.TryGetValue(key, out value);
        if (dictionary.ContainsKey(key)) {
            dictionary[key] = dictionary[key] + count;
        }
        else {
            dictionary[key] = count;
        }
        return dictionary[key];
    }

    public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences) {
        IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
        return sequences.Aggregate(
            emptyProduct,
            (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Concat(new[] { item })
            );
    }
}