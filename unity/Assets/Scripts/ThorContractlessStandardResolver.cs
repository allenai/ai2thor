#if PLATFORM_CLOUD_RENDERING
#undef ENABLE_IL2CPP
#endif
// CloudRendering does not set ENABLE_IL2CPP correctly when using Mono
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
namespace MessagePack.Resolvers {
    public class NavMeshPathFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.AI.NavMeshPath> {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.AI.NavMeshPath value, global::MessagePack.MessagePackSerializerOptions options) {
            writer.WriteMapHeader(2);
            writer.Write("corners");
            writer.WriteArrayHeader(value.corners.Length);
            Vector3Formatter f = new Vector3Formatter();
            foreach (Vector3 c in value.corners) {
                f.Serialize(ref writer, c, options);
            }
            writer.Write("status");
            writer.Write(value.status.ToString());
        }
        public global::UnityEngine.AI.NavMeshPath Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options) {
            throw new System.NotImplementedException();
        }

    }
    
    // MessagePack doesn't support serializing sub-types that have been assigned to a parameter with the
    // base type, such as the case for droneAgent and droneObjectMetadata.  This is purely for performance (on msgpack's part)
    // The following two formatters examine the types of the values to be serialized and use the appropriate formatter.
    public class ObjectMetadataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<ObjectMetadata[]> {
        private IMessagePackFormatter<ObjectMetadata[]> formatter = DynamicGenericResolver.Instance.GetFormatter<ObjectMetadata[]>();
        private IMessagePackFormatter<DroneObjectMetadata> droneFormatter = DynamicObjectResolver.Instance.GetFormatter<DroneObjectMetadata>();
        public void Serialize(ref MessagePackWriter writer, ObjectMetadata[] value, global::MessagePack.MessagePackSerializerOptions options) {
            if (value == null) {
                writer.WriteNil();
                return;
            }
            if (value.Length > 0 && value[0].GetType() == typeof(DroneObjectMetadata)) {
                writer.WriteArrayHeader(value.Length);
                foreach (var v in value) {
                    droneFormatter.Serialize(ref writer, (DroneObjectMetadata)v, options);
                }
            } else {
                formatter.Serialize(ref writer, value, options);
            }
        }
        public ObjectMetadata[] Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options) {
            throw new System.NotImplementedException();
        }
    }

    public class Vector4Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector4> {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Vector4 value, global::MessagePack.MessagePackSerializerOptions options) {
            writer.WriteMapHeader(4);
            writer.Write("x");
            writer.Write(value.x);
            writer.Write("y");
            writer.Write(value.y);
            writer.Write("z");
            writer.Write(value.z);
            writer.Write("w");
            writer.Write(value.w);
        }
        public global::UnityEngine.Vector4 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options) {
             if (reader.TryReadNil()) {
                throw new InvalidOperationException("Cannot deserialize a nil value to a Vector3.");
            }

            int mapLength = reader.ReadMapHeader();
            float x = 0, y = 0, z = 0, w = 0; 

            for (int i = 0; i < mapLength; i++) {
                string property = reader.ReadString();

                switch (property) {
                    case "x":
                        x = reader.ReadSingle();
                        break;
                    case "y":
                        y = reader.ReadSingle();
                        break;
                    case "z":
                        z = reader.ReadSingle();
                        break;
                     case "w":
                        w = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip(); // Skip unknown fields
                        break;
                }
            }

            return new global::UnityEngine.Vector4(x, y, z, w);
        }
    

    }
    
    public class AgentMetadataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<AgentMetadata> {
        private IMessagePackFormatter<AgentMetadata> formatter = DynamicObjectResolver.Instance.GetFormatter<AgentMetadata>();
        private IMessagePackFormatter<DroneAgentMetadata> droneFormatter = DynamicObjectResolver.Instance.GetFormatter<DroneAgentMetadata>();
        public void Serialize(ref MessagePackWriter writer, AgentMetadata value, global::MessagePack.MessagePackSerializerOptions options) {
            if (value == null) {
                writer.WriteNil();
                return;
            }
            Type type = value.GetType();
            if (type == typeof(DroneAgentMetadata)) {
                droneFormatter.Serialize(ref writer, (DroneAgentMetadata)value, options);
            } else {
                formatter.Serialize(ref writer, value, options);
            }
        }
        public AgentMetadata Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options) {
            throw new System.NotImplementedException();
        }

    }
    
    
    public class Vector3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector3> {
        public void Serialize(ref MessagePackWriter writer, global::UnityEngine.Vector3 value, global::MessagePack.MessagePackSerializerOptions options) {
            writer.WriteMapHeader(3);
            writer.Write("x");
            writer.Write(value.x);
            writer.Write("y");
            writer.Write(value.y);
            writer.Write("z");
            writer.Write(value.z);
        }
        public global::UnityEngine.Vector3 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options) {
            if (reader.TryReadNil()) {
                throw new InvalidOperationException("Cannot deserialize a nil value to a Vector3.");
            }

            int mapLength = reader.ReadMapHeader();
            float x = 0, y = 0, z = 0;

            for (int i = 0; i < mapLength; i++) {
                string property = reader.ReadString();

                switch (property) {
                    case "x":
                        x = reader.ReadSingle();
                        break;
                    case "y":
                        y = reader.ReadSingle();
                        break;
                    case "z":
                        z = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip(); // Skip unknown fields
                        break;
                }
            }

            return new global::UnityEngine.Vector3(x, y, z);
        }

    }

    public class ThorContractlessStandardResolver : IFormatterResolver {
        public static readonly MessagePackSerializerOptions Options;
        public static readonly ThorContractlessStandardResolver Instance;

        private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]{
            ThorUnityResolver.Instance,
            BuiltinResolver.Instance, // Try Builtin
            #if !ENABLE_IL2CPP 
            DynamicEnumResolver.Instance, // Try Enum
            DynamicGenericResolver.Instance, // Try Array, Tuple, Collection, Enum(Generic Fallback)
            DynamicUnionResolver.Instance, // Try Union(Interface)
            DynamicObjectResolver.Instance, // Try Object
            DynamicContractlessObjectResolver.Instance // Serializes keys as strings
            #endif
        };

        static ThorContractlessStandardResolver() {
            Instance = new ThorContractlessStandardResolver();
            Options = MessagePackSerializerOptions.Standard.WithResolver(Instance);
        }

        private ThorContractlessStandardResolver() {
        }

        public IMessagePackFormatter<T> GetFormatter<T>() {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T> {
            public static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache() {
                if (typeof(T) == typeof(object)) {
                    // final fallback
#if !ENABLE_IL2CPP
                    Formatter = (IMessagePackFormatter<T>)DynamicObjectTypeFallbackFormatter.Instance;
#else
                    Formatter = PrimitiveObjectResolver.Instance.GetFormatter<T>();
#endif
                } else {
                    foreach (IFormatterResolver item in Resolvers) {
                        IMessagePackFormatter<T> f = item.GetFormatter<T>();
                        if (f != null) {
                            Formatter = f;
                            return;
                        }
                    }
                }
            }
        }
    }
}

public class ThorUnityResolver : IFormatterResolver {
    public static readonly ThorUnityResolver Instance = new ThorUnityResolver();

    private ThorUnityResolver() {
    }

    public IMessagePackFormatter<T> GetFormatter<T>() {
        return FormatterCache<T>.Formatter;
    }

    private static class FormatterCache<T> {
        public static readonly IMessagePackFormatter<T> Formatter;
        private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>()
        {
                // standard
                { typeof(Vector3), new Vector3Formatter() },
                { typeof(NavMeshPath), new NavMeshPathFormatter() },
                { typeof(Vector4), new Vector4Formatter() },
                { typeof(AgentMetadata), new AgentMetadataFormatter() },
                { typeof(ObjectMetadata[]), new ObjectMetadataFormatter() }
            };

        static FormatterCache() {
            Formatter = (IMessagePackFormatter<T>)GetFormatter(typeof(T));
        }

        static object GetFormatter(Type t) {
            object formatter;
            if (FormatterMap.TryGetValue(t, out formatter)) {
                return formatter;
            }

            return null;
        }
    }
}
