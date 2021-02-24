using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using MessagePack.Internal;
/*
  This resolver is necessary because the MessagePack library does not allow modification to the FormatMap within
  the UnityResolver and we want our output to match the json output for Vector3.

*/
namespace MessagePack.Resolvers
{
 public class NavMeshPathFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.AI.NavMeshPath>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.AI.NavMeshPath value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteMapHeader(2);
            writer.Write("corners");
            writer.WriteArrayHeader(value.corners.Length);
            Vector3Formatter f = new Vector3Formatter();
            foreach(Vector3 c in value.corners) {
                f.Serialize(ref writer, c, options);
            }
            writer.Write("status");
            writer.Write(value.status.ToString());
        }
         public global::UnityEngine.AI.NavMeshPath Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options){
             throw new System.NotImplementedException();
         }

    }
 public class Vector3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector3>
    {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Vector3 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteMapHeader(3);
            writer.Write("x");
            writer.Write(value.x);
            writer.Write("y");
            writer.Write(value.y);
            writer.Write("z");
            writer.Write(value.z);
        }
         public global::UnityEngine.Vector3 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options){
             throw new System.NotImplementedException();
         }

    }
    
    public class ThorContractlessStandardResolver : IFormatterResolver
    {
        public static readonly MessagePackSerializerOptions Options;
        public static readonly ThorContractlessStandardResolver Instance;

        private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]{
            BuiltinResolver.Instance, // Try Builtin
            AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]
            UnityResolver.Instance,
            DynamicEnumResolver.Instance, // Try Enum
            DynamicGenericResolver.Instance, // Try Array, Tuple, Collection, Enum(Generic Fallback)
            DynamicUnionResolver.Instance, // Try Union(Interface)
            DynamicObjectResolver.Instance, // Try Object
            DynamicContractlessObjectResolver.Instance // Serializes keys as strings
        };

        static ThorContractlessStandardResolver()
        {
            Instance = new ThorContractlessStandardResolver();
            Options = new MessagePackSerializerOptions(Instance);
        }

        private ThorContractlessStandardResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                if (typeof(T) == typeof(object))
                {
                    // final fallback
#if !ENABLE_IL2CPP
                    Formatter = (IMessagePackFormatter<T>)DynamicObjectTypeFallbackFormatter.Instance;
#else
                    Formatter = PrimitiveObjectResolver.Instance.GetFormatter<T>();
#endif
                }
                else
                {
                    foreach (IFormatterResolver item in Resolvers)
                    {
                        IMessagePackFormatter<T> f = item.GetFormatter<T>();
                        if (f != null)
                        {
                            Formatter = f;
                            return;
                        }
                    }
                }
            }
        }
  }
}

 public class UnityResolver : IFormatterResolver
    {
        public static readonly UnityResolver Instance = new UnityResolver();

        private UnityResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;
            private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>()
            {
                // standard
                { typeof(Vector3), new Vector3Formatter() },
                { typeof(NavMeshPath), new NavMeshPathFormatter() }

            };

            static FormatterCache()
            {
                Formatter = (IMessagePackFormatter<T>)GetFormatter(typeof(T));
            }

            static object GetFormatter(Type t)
            {
                object formatter;
                if (FormatterMap.TryGetValue(t, out formatter))
                {
                    return formatter;
                }

            return null;
            }
        }
    }
