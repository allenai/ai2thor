
// CloudRendering does not set ENABLE_IL2CPP correctly when using Mono
using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Internal;
using MessagePack.Resolvers;
using UnityEngine;
using UnityEngine.AI;
using MessagePack.Unity;
using Thor.Procedural.Data;

/*
  This resolver is necessary because the MessagePack library does not allow modification to the FormatMap within
  the UnityResolver and we want our output to match the json output for Vector3.

*/
namespace MessagePack.Resolvers {

    public static class MessagePackInit
    {
        private static bool registered = false;
        public static void Register()
        {
            // if (!registered) {
            MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard
                .WithResolver(CompositeResolver.Create(
                    ThorWebGLSafeStandardResolver.Instance,
                    BuiltinResolver.Instance,
                    UnityResolver.Instance,
                    StandardResolver.Instance
                ));
            
            MessagePackSerializer.DefaultOptions = options;
            registered = true;
            // }
        }
    }

    public class SerializableColliderFormatter : IMessagePackFormatter<SerializableCollider>
{
    public void Serialize(ref MessagePackWriter writer, SerializableCollider value, MessagePackSerializerOptions options)
    {
        writer.WriteMapHeader(2);
        writer.Write(nameof(value.vertices));
        options.Resolver.GetFormatterWithVerify<Vector3[]>().Serialize(ref writer, value.vertices, options);
        writer.Write(nameof(value.triangles));
        options.Resolver.GetFormatterWithVerify<int[]>().Serialize(ref writer, value.triangles, options);
    }

    public SerializableCollider Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var count = reader.ReadMapHeader();
        var result = new SerializableCollider();

        for (int i = 0; i < count; i++)
        {
            var propertyName = reader.ReadString();
            switch (propertyName)
            {
                case nameof(result.vertices):
                    result.vertices = options.Resolver.GetFormatterWithVerify<Vector3[]>().Deserialize(ref reader, options);
                    break;
                case nameof(result.triangles):
                    result.triangles = options.Resolver.GetFormatterWithVerify<int[]>().Deserialize(ref reader, options);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return result;
    }
}

public class PhysicalPropertiesFormatter : IMessagePackFormatter<PhysicalProperties>
{
    public void Serialize(ref MessagePackWriter writer, PhysicalProperties value, MessagePackSerializerOptions options)
    {
        writer.WriteMapHeader(5);
        writer.Write(nameof(value.mass));
        writer.Write(value.mass);
        writer.Write(nameof(value.drag));
        writer.Write(value.drag);
        writer.Write(nameof(value.angularDrag));
        writer.Write(value.angularDrag);
        writer.Write(nameof(value.useGravity));
        writer.Write(value.useGravity);
        writer.Write(nameof(value.isKinematic));
        writer.Write(value.isKinematic);
    }

    public PhysicalProperties Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var count = reader.ReadMapHeader();
        var result = new PhysicalProperties();

        for (int i = 0; i < count; i++)
        {
            var propertyName = reader.ReadString();
            switch (propertyName)
            {
                case nameof(result.mass): result.mass = reader.ReadSingle(); break;
                case nameof(result.drag): result.drag = reader.ReadSingle(); break;
                case nameof(result.angularDrag): result.angularDrag = reader.ReadSingle(); break;
                case nameof(result.useGravity): result.useGravity = reader.ReadBoolean(); break;
                case nameof(result.isKinematic): result.isKinematic = reader.ReadBoolean(); break;
                default: reader.Skip(); break;
            }
        }

        return result;
    }
}

public class ObjectAnnotationsFormatter : IMessagePackFormatter<ObjectAnnotations>
{
    public void Serialize(ref MessagePackWriter writer, ObjectAnnotations value, MessagePackSerializerOptions options)
    {
        writer.WriteMapHeader(3);
        writer.Write(nameof(value.objectType));
        writer.Write(value.objectType);
        writer.Write(nameof(value.primaryProperty));
        writer.Write(value.primaryProperty);
        writer.Write(nameof(value.secondaryProperties));
        options.Resolver.GetFormatterWithVerify<string[]>().Serialize(ref writer, value.secondaryProperties, options);
    }

    public ObjectAnnotations Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var count = reader.ReadMapHeader();
        var result = new ObjectAnnotations();

        for (int i = 0; i < count; i++)
        {
            var propertyName = reader.ReadString();
            switch (propertyName)
            {
                case nameof(result.objectType): result.objectType = reader.ReadString(); break;
                case nameof(result.primaryProperty): result.primaryProperty = reader.ReadString(); break;
                case nameof(result.secondaryProperties):
                    result.secondaryProperties = options.Resolver.GetFormatterWithVerify<string[]>().Deserialize(ref reader, options);
                    break;
                default: reader.Skip(); break;
            }
        }

        return result;
    }
}

public class ProceduralTexturesFormatter : IMessagePackFormatter<ProceduralTextures>
{
    public void Serialize(ref MessagePackWriter writer, ProceduralTextures value, MessagePackSerializerOptions options)
    {
        writer.WriteMapHeader(4);
        writer.Write(nameof(value.albedoBase64JPG)); writer.Write(value.albedoBase64JPG);
        writer.Write(nameof(value.metallicSmoothnessBase64JPG)); writer.Write(value.metallicSmoothnessBase64JPG);
        writer.Write(nameof(value.normalBase64JPG)); writer.Write(value.normalBase64JPG);
        writer.Write(nameof(value.emissionBase64JPG)); writer.Write(value.emissionBase64JPG);
    }

    public ProceduralTextures Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var count = reader.ReadMapHeader();
        var result = new ProceduralTextures();

        for (int i = 0; i < count; i++)
        {
            var propertyName = reader.ReadString();
            switch (propertyName)
            {
                case nameof(result.albedoBase64JPG): result.albedoBase64JPG = reader.ReadString(); break;
                case nameof(result.metallicSmoothnessBase64JPG): result.metallicSmoothnessBase64JPG = reader.ReadString(); break;
                case nameof(result.normalBase64JPG): result.normalBase64JPG = reader.ReadString(); break;
                case nameof(result.emissionBase64JPG): result.emissionBase64JPG = reader.ReadString(); break;
                default: reader.Skip(); break;
            }
        }

        return result;
    }
}




    public class ProceduralAssetFormatter : IMessagePackFormatter<ProceduralAsset>
    {
    public void Serialize(ref MessagePackWriter writer, ProceduralAsset value, MessagePackSerializerOptions options)
    {
        writer.WriteMapHeader(17);

        writer.Write(nameof(value.vertices));
        MessagePackSerializer.Serialize(ref writer, value.vertices, options);

        writer.Write(nameof(value.normals));
        MessagePackSerializer.Serialize(ref writer, value.normals, options);

        writer.Write(nameof(value.name));
        writer.Write(value.name);

        writer.Write(nameof(value.triangles));
        MessagePackSerializer.Serialize(ref writer, value.triangles, options);

        writer.Write(nameof(value.uvs));
        MessagePackSerializer.Serialize(ref writer, value.uvs, options);

        writer.Write(nameof(value.albedoTexturePath));
        writer.Write(value.albedoTexturePath);

        writer.Write(nameof(value.metallicSmoothnessTexturePath));
        writer.Write(value.metallicSmoothnessTexturePath);

        writer.Write(nameof(value.normalTexturePath));
        writer.Write(value.normalTexturePath);

        writer.Write(nameof(value.emissionTexturePath));
        writer.Write(value.emissionTexturePath);

        writer.Write(nameof(value.colliders));
        MessagePackSerializer.Serialize(ref writer, value.colliders, options);

        writer.Write(nameof(value.physicalProperties));
        MessagePackSerializer.Serialize(ref writer, value.physicalProperties, options);

        writer.Write(nameof(value.visibilityPoints));
        MessagePackSerializer.Serialize(ref writer, value.visibilityPoints, options);

        writer.Write(nameof(value.annotations));
        MessagePackSerializer.Serialize(ref writer, value.annotations, options);

        writer.Write(nameof(value.receptacleCandidate));
        writer.Write(value.receptacleCandidate);

        writer.Write(nameof(value.yRotOffset));
        writer.Write(value.yRotOffset);

        writer.Write(nameof(value.serializable));
        writer.Write(value.serializable);

        writer.Write(nameof(value.parentTexturesDir));
        writer.Write(value.parentTexturesDir);
    }

    public ProceduralAsset Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var count = reader.ReadMapHeader();
        var asset = new ProceduralAsset();

        for (int i = 0; i < count; i++)
        {
            var key = reader.ReadString();
            switch (key)
            {
                case nameof(ProceduralAsset.vertices):
                    asset.vertices = MessagePackSerializer.Deserialize<Vector3[]>(ref reader, options);
                    break;
                case nameof(ProceduralAsset.normals):
                    asset.normals = MessagePackSerializer.Deserialize<Vector3[]>(ref reader, options);
                    break;
                case nameof(ProceduralAsset.name):
                    asset.name = reader.ReadString();
                    break;
                case nameof(ProceduralAsset.triangles):
                    asset.triangles = MessagePackSerializer.Deserialize<int[]>(ref reader, options);
                    break;
                case nameof(ProceduralAsset.uvs):
                    asset.uvs = MessagePackSerializer.Deserialize<Vector2[]>(ref reader, options);
                    break;
                case nameof(ProceduralAsset.albedoTexturePath):
                    asset.albedoTexturePath = reader.ReadString();
                    break;
                case nameof(ProceduralAsset.metallicSmoothnessTexturePath):
                    asset.metallicSmoothnessTexturePath = reader.ReadString();
                    break;
                case nameof(ProceduralAsset.normalTexturePath):
                    asset.normalTexturePath = reader.ReadString();
                    break;
                case nameof(ProceduralAsset.emissionTexturePath):
                    asset.emissionTexturePath = reader.ReadString();
                    break;
                case nameof(ProceduralAsset.colliders):
                    asset.colliders = MessagePackSerializer.Deserialize<SerializableCollider[]>(ref reader, options);
                    break;
                case nameof(ProceduralAsset.physicalProperties):
                    asset.physicalProperties = MessagePackSerializer.Deserialize<PhysicalProperties>(ref reader, options);
                    break;
                case nameof(ProceduralAsset.visibilityPoints):
                    asset.visibilityPoints = MessagePackSerializer.Deserialize<Vector3[]>(ref reader, options);
                    break;
                case nameof(ProceduralAsset.annotations):
                    asset.annotations = MessagePackSerializer.Deserialize<ObjectAnnotations>(ref reader, options);
                    break;
                case nameof(ProceduralAsset.receptacleCandidate):
                    asset.receptacleCandidate = reader.ReadBoolean();
                    break;
                case nameof(ProceduralAsset.yRotOffset):
                    asset.yRotOffset = reader.ReadSingle();
                    break;
                case nameof(ProceduralAsset.serializable):
                    asset.serializable = reader.ReadBoolean();
                    break;
                case nameof(ProceduralAsset.parentTexturesDir):
                    asset.parentTexturesDir = reader.ReadString();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return asset;
    }
}


public class Vector2Formatter
        : global::MessagePack.Formatters.IMessagePackFormatter<global::UnityEngine.Vector2> {
        public void Serialize(
            ref MessagePackWriter writer,
            global::UnityEngine.Vector2 value,
            global::MessagePack.MessagePackSerializerOptions options
        ) {
            writer.WriteMapHeader(2);
            writer.Write("x");
            writer.Write(value.x);
            writer.Write("y");
            writer.Write(value.y);
        }

        public global::UnityEngine.Vector2 Deserialize(
            ref MessagePackReader reader,
            global::MessagePack.MessagePackSerializerOptions options
        ) {
            if (reader.TryReadNil()) {
                throw new InvalidOperationException("Cannot deserialize a nil value to a Vector3.");
            }

            int mapLength = reader.ReadMapHeader();
            float x = 0,
                y = 0,
                z = 0;

            for (int i = 0; i < mapLength; i++) {
                string property = reader.ReadString();

                switch (property) {
                    case "x":
                        x = reader.ReadSingle();
                        break;
                    case "y":
                        y = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip(); // Skip unknown fields
                        break;
                }
            }

            return new global::UnityEngine.Vector2(x, y);
        }
    }



    public class ThorWebGLSafeStandardResolver : IFormatterResolver {
        public static readonly MessagePackSerializerOptions Options;
        public static readonly ThorWebGLSafeStandardResolver Instance;

        private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]
        {
            ThorWebGLSafeUnityResolver.Instance,
            BuiltinResolver.Instance, // Try Builtin
        };

        static ThorWebGLSafeStandardResolver() {
            Instance = new ThorWebGLSafeStandardResolver();
            Options = MessagePackSerializerOptions.Standard.WithResolver(Instance);
        }

        private ThorWebGLSafeStandardResolver() { }

        public IMessagePackFormatter<T> GetFormatter<T>() {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T> {
            public static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache() {
                if (typeof(T) == typeof(object)) {
                    // final fallback
                    Formatter = PrimitiveObjectResolver.Instance.GetFormatter<T>();

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

public class ThorWebGLSafeUnityResolver : IFormatterResolver {
    public static readonly ThorWebGLSafeUnityResolver Instance = new ThorWebGLSafeUnityResolver();

    private ThorWebGLSafeUnityResolver() { }

    public IMessagePackFormatter<T> GetFormatter<T>() {
        return FormatterCache<T>.Formatter;
    }

    private static class FormatterCache<T> {
        public static readonly IMessagePackFormatter<T> Formatter;
        private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>()
        {
            // Primitives & Unity types
            { typeof(Vector2), new MessagePack.Resolvers.Vector2Formatter() },
            { typeof(Vector3), new MessagePack.Resolvers.Vector3Formatter() },
            { typeof(Vector4), new MessagePack.Resolvers.Vector4Formatter() },

            // Arrays
            { typeof(Vector2[]), new ArrayFormatter<Vector2>() },
            { typeof(Vector3[]), new ArrayFormatter<Vector3>() },
            { typeof(Vector4[]), new ArrayFormatter<Vector4>() },
            { typeof(int[]), new ArrayFormatter<int>() },
            { typeof(string[]), new ArrayFormatter<string>() },
            { typeof(SerializableCollider[]), new ArrayFormatter<SerializableCollider>() },

            // Custom types
            { typeof(SerializableCollider), new SerializableColliderFormatter() },
            { typeof(PhysicalProperties), new PhysicalPropertiesFormatter() },
            { typeof(ObjectAnnotations), new ObjectAnnotationsFormatter() },
            { typeof(ProceduralTextures), new ProceduralTexturesFormatter() },
            { typeof(ProceduralAsset), new ProceduralAssetFormatter() },
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
