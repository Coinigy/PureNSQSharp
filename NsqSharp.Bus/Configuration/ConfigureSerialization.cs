﻿using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Serialization configuration
    /// </summary>
    public interface IConfigureSerialization
    {
        /// <summary>
        /// Attempts to find a compatible version of Newtonsoft.Json.
        /// </summary>
        void Json();

        /// <summary>
        /// Attempts to find a compatible version of Newtonsoft.Json.
        /// </summary>
        void Json(Assembly jsonAssembly);

        /// <summary>
        /// Sets the default serialization/deserialization methods.
        /// </summary>
        /// <param name="serializer">The default serialization method.</param>
        /// <param name="deserializer">The default deserialization method.</param>
        void SetDefault(Func<object, byte[]> serializer, Func<byte[], object> deserializer);

        /// <summary>
        /// Serializes the <paramref name="value"/> using the default serialization method.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialized value.</returns>
        byte[] DefaultSerialize(object value);

        /// <summary>
        /// Deserializes the <paramref name="value"/> using the default deserialization method.
        /// </summary>
        /// <param name="value">The value to deserialize.</param>
        /// <returns>The deserialized value.</returns>
        object DefaultDeserialize(byte[] value);
    }

    internal class ConfigureSerialization : IConfigureSerialization
    {
        internal Func<object, byte[]> _defaultSerializer;
        internal Func<byte[], object> _defaultDeserializer;

        public void Json()
        {
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] files = Directory.GetFiles(dir, "Newtonsoft.Json.dll");
            if (files.Length == 1)
            {
                var jsonAssembly = Assembly.LoadFrom(files[0]);
                Json(jsonAssembly);
            }
            else
            {
                throw new Exception(string.Format("Newtonsoft.Json.dll not found in directory {0}. " +
                    "Try specifying the assembly directly with 'typeof(JsonConvert).Assembly'", dir));
            }
        }

        public void Json(Assembly jsonAssembly)
        {
            if (jsonAssembly == null)
                throw new ArgumentNullException("jsonAssembly");

            var jsonConvertType = jsonAssembly.GetType("Newtonsoft.Json.JsonConvert", throwOnError: true);
            var jsonConvertMethods = jsonConvertType.GetMethods(BindingFlags.Public | BindingFlags.Static);

            Func<object, byte[]> serializer = null;
            Func<byte[], object> deserializer = null;

            foreach (var method in jsonConvertMethods)
            {
                bool isSerializeObject = (method.Name == "SerializeObject");
                bool isDeserializeObject = (method.Name == "DeserializeObject");

                if (isSerializeObject || isDeserializeObject)
                {
                    var genericArgs = method.GetGenericArguments();
                    var parameters = method.GetParameters();

                    if (genericArgs.Length == 0 && parameters.Length == 1)
                    {
                        if (isSerializeObject)
                        {
                            var serializeMethod = method;
                            serializer = o => Encoding.UTF8.GetBytes((string)serializeMethod.Invoke(null, new[] { o }));
                        }
                        else
                        {
                            var deserializeMethod = method;
                            deserializer = byteArray =>
                                deserializeMethod.Invoke(null, new object[] { Encoding.UTF8.GetString(byteArray) });
                        }
                    }
                }
            }

            if (serializer == null)
                throw new Exception("Cannot find Newtonsoft.Json.JsonConvert.SerializeObject static method");
            if (deserializer == null)
                throw new Exception("Cannot find Newtonsoft.Json.JsonConvert.DeserializeObject static method");

            SetDefault(serializer, deserializer);
        }

        public void SetDefault(Func<object, byte[]> serializer, Func<byte[], object> deserializer)
        {
            if (serializer == null)
                throw new ArgumentNullException("serializer");
            if (deserializer == null)
                throw new ArgumentNullException("deserializer");

            _defaultSerializer = serializer;
            _defaultDeserializer = deserializer;
        }

        public byte[] DefaultSerialize(object value)
        {
            if (_defaultSerializer == null)
                throw new Exception("Default serializer not set.");

            return _defaultSerializer(value);
        }

        public object DefaultDeserialize(byte[] value)
        {
            if (_defaultDeserializer == null)
                throw new Exception("Default deserializer not set.");

            return _defaultDeserializer(value);
        }
    }
}