using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Wynathan.Net.Serializers
{
    public class JsonSerializer
    {
        public static string Serialize<T>(T instance)
            where T : class, new()
        {
            return Serialize(instance, Encoding.UTF8);
        }

        /// <summary>
        /// Serializes an object to JSON
        /// </summary>
        public static string Serialize<T>(T instance, Encoding encoding)
            where T : class, new()
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, instance);
                return encoding.GetString(stream.ToArray());
            }
        }

        public static T Deserialize<T>(string json)
            where T : class, new()
        {
            return Deserialize<T>(json, Encoding.UTF8);
        }

        /// <summary>
        /// Deserializes an object from JSON
        /// </summary>
        public static T Deserialize<T>(string json, Encoding encoding)
            where T : class, new()
        {
            using (var stream = new MemoryStream(encoding.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return serializer.ReadObject(stream) as T;
            }
        }
    }
}
