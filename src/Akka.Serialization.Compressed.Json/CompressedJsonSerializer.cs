//-----------------------------------------------------------------------
// <copyright file="NewtonSoftJsonSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Akka.Actor;
using Akka.Configuration;
using Akka.Util;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Akka.Serialization.Compressed.Json
{
    /// <summary>
    /// A typed settings for a <see cref="CompressedJsonSerializer"/> class.
    /// </summary>
    public sealed class CompressedJsonSerializerSettings
    {
        /// <summary>
        /// A default instance of <see cref="CompressedJsonSerializerSettings"/> used when no custom configuration has been provided.
        /// </summary>
        public static readonly CompressedJsonSerializerSettings Default = new CompressedJsonSerializerSettings(
            encodeTypeNames: true,
            preserveObjectReferences: true,
            converters: Enumerable.Empty<Type>(),
            usePooledStringBuilder:true,
            stringBuilderMinSize:2048,
            stringBuilderMaxSize:32768);

        /// <summary>
        /// Creates a new instance of the <see cref="CompressedJsonSerializerSettings"/> based on a provided <paramref name="config"/>.
        /// Config may define several key-values:
        /// <ul>
        /// <li>`encode-type-names` (boolean) mapped to <see cref="EncodeTypeNames"/></li>
        /// <li>`preserve-object-references` (boolean) mapped to <see cref="PreserveObjectReferences"/></li>
        /// <li>`converters` (type list) mapped to <see cref="Converters"/>. They must implement <see cref="JsonConverter"/> and define either default constructor or constructor taking <see cref="ExtendedActorSystem"/> as its only parameter.</li>
        /// </ul>
        /// </summary>
        /// <exception cref="ArgumentNullException">Raised when no <paramref name="config"/> was provided.</exception>
        /// <exception cref="ArgumentException">Raised when types defined in `converters` list didn't inherit <see cref="JsonConverter"/>.</exception>
        public static CompressedJsonSerializerSettings Create(Config config)
        {
            if (config.IsNullOrEmpty())
                throw ConfigurationException.NullOrEmptyConfig<CompressedJsonSerializerSettings>();

            return new CompressedJsonSerializerSettings(
                encodeTypeNames: config.GetBoolean("encode-type-names", true),
                preserveObjectReferences: config.GetBoolean(
                    "preserve-object-references", true),
                converters: GetConverterTypes(config),
                usePooledStringBuilder: config.GetBoolean("use-pooled-string-builder", true),
                stringBuilderMinSize:config.GetInt("pooled-string-builder-minsize", 2048),
                stringBuilderMaxSize:
                    config.GetInt("pooled-string-builder-maxsize",
                        32768)
            );
        }

        private static IEnumerable<Type> GetConverterTypes(Config config)
        {
            var converterNames = config.GetStringList("converters", new string[] { });

            if (converterNames == null) 
                yield break;
            
            foreach (var converterName in converterNames)
            {
                var type = Type.GetType(converterName, true);
                if(type is null)
                    throw new ArgumentException($"Type {type} could not be loaded.");
                if (!typeof(JsonConverter).IsAssignableFrom(type))
                    throw new ArgumentException($"Type {type} doesn't inherit from a {typeof(JsonConverter)}.");

                yield return type;
            }
        }

        /// <summary>
        /// When true, serializer will encode a type names into serialized json $type field. This must be true 
        /// if <see cref="CompressedJsonSerializer"/> is a default serializer in order to support polymorphic 
        /// deserialization.
        /// </summary>
        public bool EncodeTypeNames { get; }

        /// <summary>
        /// When true, serializer will track a reference dependencies in serialized object graph. This must be 
        /// true if <see cref="CompressedJsonSerializer"/>.
        /// </summary>
        public bool PreserveObjectReferences { get; }

        /// <summary>
        /// A collection of an additional converter types to be applied to a <see cref="CompressedJsonSerializer"/>.
        /// Converters must inherit from <see cref="JsonConverter"/> class and implement a default constructor.
        /// </summary>
        public IEnumerable<Type> Converters { get; }
        
        /// <summary>
        /// The Starting size used for Pooled StringBuilders, if <see cref="UsePooledStringBuilder"/> is -true-
        /// </summary>
        public int StringBuilderMinSize { get; }
        /// <summary>
        /// The Max Retained size for Pooled StringBuilders, if <see cref="UsePooledStringBuilder"/> is -true-
        /// </summary>
        public int StringBuilderMaxSize { get; }
        /// <summary>
        /// If -true-, string builders are pooled and reused for serialization to lower memory pressure.
        /// </summary>
        public bool UsePooledStringBuilder { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="CompressedJsonSerializerSettings"/>.
        /// </summary>
        /// <param name="encodeTypeNames">Determines if a special `$type` field should be emitted into serialized JSON. Must be true if corresponding serializer is used as default.</param>
        /// <param name="preserveObjectReferences">Determines if object references should be tracked within serialized object graph. Must be true if corresponding serialize is used as default.</param>
        /// <param name="converters">A list of types implementing a <see cref="JsonConverter"/> to support custom types serialization.</param>
        /// <param name="usePooledStringBuilder">Determines if string builders will be used from a pool to lower memory usage</param>
        /// <param name="stringBuilderMinSize">Starting size used for pooled string builders if enabled</param>
        /// <param name="stringBuilderMaxSize">Max retained size used for pooled string builders if enabled</param>
        public CompressedJsonSerializerSettings(
            bool encodeTypeNames,
            bool preserveObjectReferences,
            IEnumerable<Type> converters,
            bool usePooledStringBuilder,
            int stringBuilderMinSize,
            int stringBuilderMaxSize)
        {
            Converters = converters ?? throw new ArgumentNullException(nameof(converters), $"{nameof(CompressedJsonSerializerSettings)} requires a sequence of converters.");
            EncodeTypeNames = encodeTypeNames;
            PreserveObjectReferences = preserveObjectReferences;
            UsePooledStringBuilder = usePooledStringBuilder;
            StringBuilderMinSize = stringBuilderMinSize;
            StringBuilderMaxSize = stringBuilderMaxSize;
        }
    }

    /// <summary>
    /// This is a special <see cref="Serializer"/> that serializes and deserializes javascript objects only.
    /// These objects need to be in the JavaScript Object Notation (JSON) format.
    /// </summary>
    public class CompressedJsonSerializer : Serializer
    {
        private readonly JsonSerializer _serializer;
        
        private readonly ObjectPool<StringBuilder>? _sbPool;
        /// <summary>
        /// TBD
        /// </summary>
        public JsonSerializerSettings Settings { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public object Serializer => _serializer;

        public override int Identifier => 35;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressedJsonSerializer" /> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public CompressedJsonSerializer(ExtendedActorSystem? system)
            : this(system, CompressedJsonSerializerSettings.Default)
        {
        }

        public CompressedJsonSerializer(ExtendedActorSystem? system, Config config)
            : this(system, CompressedJsonSerializerSettings.Create(config))
        {
        }
        
        public CompressedJsonSerializer(ExtendedActorSystem? system, CompressedJsonSerializerSettings settings)
            : base(system)
        {
            if (settings.UsePooledStringBuilder)
            {
                _sbPool = new DefaultObjectPoolProvider()
                    .CreateStringBuilderPool(settings.StringBuilderMinSize,settings.StringBuilderMaxSize);
            }
            Settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = settings.PreserveObjectReferences
                    ? PreserveReferencesHandling.Objects
                    : PreserveReferencesHandling.None,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                TypeNameHandling = settings.EncodeTypeNames
                    ? TypeNameHandling.All
                    : TypeNameHandling.None,
            };

            if (system is { })
            {
                var settingsSetup = system.Settings.Setup.Get<CompressedJsonSerializerSetup>().Value;
                settingsSetup?.ApplySettings(Settings);
            }

            var converters = settings.Converters
                .Select(type => CreateConverter(type, system))
                .ToList();

            converters.Add(new SurrogateConverter(this));
            converters.Add(new DiscriminatedUnionConverter());

            foreach (var converter in converters)
            {
                Settings.Converters.Add(converter);
            }

            // important: if reuse, the serializer will overwrite properties in default references,
            // e.g. Props.DefaultDeploy or Props.noArgs
            Settings.ObjectCreationHandling = ObjectCreationHandling.Replace; 
            Settings.ContractResolver = new AkkaContractResolver();

            _serializer = JsonSerializer.Create(Settings);
        }

        private static JsonConverter CreateConverter(Type converterType, ExtendedActorSystem? actorSystem)
        {
            if (converterType.GetConstructor(Array.Empty<Type>()) is { })
                return (JsonConverter) Activator.CreateInstance(converterType);
            
            if(converterType.GetConstructor(new[] { typeof(ExtendedActorSystem) }) is { })
                return (JsonConverter) Activator.CreateInstance(converterType, actorSystem);

            throw new ConfigurationException(
                $"Converter {converterType.Name} have to have a public empty constructor or a public constructor " +
                $"with one {nameof(ExtendedActorSystem)} parameter");
        }

        private class AkkaContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);

                if (prop.Writable || member is not PropertyInfo property) 
                    return prop;
                
                // Property has a private setter
                if(property.GetSetMethod(true) is { })
                {
                    prop.Writable = true;
                    return prop;
                }
                
                // Property does not have a private setter
                var fieldInfo = prop.DeclaringType?.GetField($"<{prop.PropertyName}>k__BackingField",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo is { })
                {
                    prop.ValueProvider = new ReflectionValueProvider(fieldInfo);
                    prop.Writable = true;
                }

                return prop;
            }
        }

        /// <summary>
        /// Returns whether this serializer needs a manifest in the fromBinary method
        /// </summary>
        public override bool IncludeManifest => false;

        /// <summary>
        /// Serializes the given object into a byte array
        /// </summary>
        /// <param name="obj">The object to serialize </param>
        /// <returns>A byte array containing the serialized object</returns>
        public override byte[] ToBinary(object obj)
        {
            return _sbPool is { } 
                ? toBinary_PooledBuilder(obj) 
                : toBinary_NewBuilder(obj);
        }

        private byte[] toBinary_NewBuilder(object obj)
        {
            var data = JsonConvert.SerializeObject(obj, Formatting.None, Settings);
            var bytes = Encoding.UTF8.GetBytes(data);
            return Compress(bytes);
        }

        private byte[] toBinary_PooledBuilder(object obj)
        {
            //Don't try to opt with
            //StringBuilder sb = _sbPool.Get()
            //Or removing null check
            //Both are necessary to avoid leaking on thread aborts etc
            StringBuilder? sb = null;
            try
            {
                sb = _sbPool!.Get();

                using var tw = new StringWriter(sb, CultureInfo.InvariantCulture);
                var ser = JsonSerializer.CreateDefault(Settings);
                ser.Formatting = Formatting.None;
                using (var jw = new JsonTextWriter(tw))
                {
                    ser.Serialize(jw, obj);
                }
                var bytes = Encoding.UTF8.GetBytes(tw.ToString());
                return Compress(bytes);
            }
            finally
            {
                if (sb != null)
                {
                    _sbPool?.Return(sb);    
                }
            }
        }

        /// <summary>
        /// Deserializes a byte array into an object of type <paramref name="type"/>.
        /// </summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <param name="type">The type of object contained in the array</param>
        /// <returns>The object contained in the array</returns>
        public override object? FromBinary(byte[] bytes, Type type)
        {
            var data = Encoding.UTF8.GetString(Decompress(bytes));
            var res = JsonConvert.DeserializeObject(data, Settings);
            return TranslateSurrogate(res, this, type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object? TranslateSurrogate(object? deserializedValue, CompressedJsonSerializer parent, Type type)
        {
            return deserializedValue switch
            {
                //The JObject represents a special akka.net wrapper for primitives (int,float,decimal) to preserve correct type when deserializing
                JObject j when j["$"] != null => GetValue(j["$"]!.Value<string>()),
                
                //The JObject is not of our concern, let Json.NET deserialize it.
                JObject j => j.ToObject(type, parent._serializer),
                
                //The deserialized object is a surrogate, unwrap it
                ISurrogate surrogate => surrogate.FromSurrogate(parent.system),
                
                _ => deserializedValue
            };
        }

        private static object GetValue(string? value)
        {
            if(value is null)
                throw new NotSupportedException("Null is not supported");
            
            var t = value[..1];
            var v = value[1..];
            return t switch
            {
                "I" => int.Parse(v, NumberFormatInfo.InvariantInfo),
                "F" => float.Parse(v, NumberFormatInfo.InvariantInfo),
                "M" => decimal.Parse(v, NumberFormatInfo.InvariantInfo),
                _ => throw new NotSupportedException()
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] Compress(byte[] data)
        {
            using var compressedStream = new MemoryStream();
            using var compressor = new GZipStream(compressedStream, CompressionMode.Compress);
            compressor.Write(data, 0, data.Length);
            compressor.Flush(); // It is critical to flush here
            return compressedStream.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] Decompress(byte[] raw)
        {
            using var compressedStream = new MemoryStream(raw);
            using var compressor = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var uncompressedStream = new MemoryStream();
            compressor.CopyTo(uncompressedStream);
            return uncompressedStream.ToArray();
        }

        /// <summary>
        /// TBD
        /// </summary>
        private class SurrogateConverter : JsonConverter
        {
            private readonly CompressedJsonSerializer _parent;
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="parent">TBD</param>
            public SurrogateConverter(CompressedJsonSerializer parent)
            {
                _parent = parent;
            }

            private static readonly Type Int32Type = typeof(int);
            private static readonly Type FloatType = typeof(float);
            private static readonly Type DecimalType = typeof(decimal);
            private static readonly Type SurrogatedType = typeof(ISurrogated);
            private static readonly Type ObjectType = typeof(object);
            
            /// <summary>
            ///     Determines whether this instance can convert the specified object type.
            /// </summary>
            /// <param name="objectType">Type of the object.</param>
            /// <returns><c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.</returns>
            public override bool CanConvert(Type objectType)
            {
                return objectType == Int32Type
                       || objectType == FloatType
                       || objectType == DecimalType
                       || SurrogatedType.IsAssignableFrom(objectType)
                       || objectType == ObjectType;    
            }

            /// <summary>
            /// Reads the JSON representation of the object.
            /// </summary>
            /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
            /// <param name="objectType">Type of the object.</param>
            /// <param name="existingValue">The existing value of object being read.</param>
            /// <param name="serializer">The calling serializer.</param>
            /// <returns>The object value.</returns>
            public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
                JsonSerializer serializer)
            {
                return DeserializeFromReader(reader, serializer, objectType);
            }

            private object? DeserializeFromReader(JsonReader reader, JsonSerializer serializer, Type objectType)
            {
                var surrogate = serializer.Deserialize(reader);
                return TranslateSurrogate(surrogate, _parent, objectType);
            }

            /// <summary>
            /// Writes the JSON representation of the object.
            /// </summary>
            /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
            /// <param name="value">The value.</param>
            /// <param name="serializer">The calling serializer.</param>
            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
                if (value is int or decimal or float)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("$");
                    writer.WriteValue(GetString(value));
                    writer.WriteEndObject();
                }
                else
                {
                    if (value is ISurrogated surrogated)
                    {
                        var surrogate = surrogated.ToSurrogate(_parent.system);
                        serializer.Serialize(writer, surrogate);
                    }
                    else
                    {
                        serializer.Serialize(writer, value);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static object GetString(object value)
            {
                return value switch
                {
                    int i => "I" + i.ToString(NumberFormatInfo.InvariantInfo),
                    float f => "F" + f.ToString(NumberFormatInfo.InvariantInfo),
                    decimal value1 => "M" + value1.ToString(NumberFormatInfo.InvariantInfo),
                    _ => throw new NotSupportedException()
                };
            }
        }
    }
}
