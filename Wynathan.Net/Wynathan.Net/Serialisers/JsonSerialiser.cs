using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Wynathan.Net.Serialisers
{
    public class JsonSerialiser
    {
        public static string Serialise<T>(T instance)
            where T : class, new()
        {
            return Serialise(instance, Encoding.UTF8);
        }

        /// <summary>
        /// Serialises an object to JSON
        /// </summary>
        public static string Serialise<T>(T instance, Encoding encoding)
            where T : class, new()
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, instance);
                return encoding.GetString(stream.ToArray());
            }
        }

        public static T Deserialise<T>(string json)
            where T : class, new()
        {
            return Deserialise<T>(json, Encoding.UTF8);
        }

        /// <summary>
        /// Deserialises an object from JSON
        /// </summary>
        public static T Deserialise<T>(string json, Encoding encoding)
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
