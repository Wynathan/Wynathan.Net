using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Wynathan.Net.Serialisers
{
    public class XmlSerialiser
    {
        public static string Serialise<T>(T instance)
            where T : class, new()
        {
            return Serialise(instance, Encoding.UTF8);
        }

        /// <summary>
        /// Serialises an object to XML
        /// </summary>
        public static string Serialise<T>(T instance, Encoding encoding)
            where T : class, new()
        {
            var serialiser = new DataContractSerializer(typeof(T));
            using (var stream = new MemoryStream())
            {
                serialiser.WriteObject(stream, instance);
                return encoding.GetString(stream.ToArray());
            }
        }

        public static T Deserialise<T>(string xml)
            where T : class, new()
        {
            return Deserialise<T>(xml, Encoding.UTF8);
        }

        /// <summary>
        /// Deserialises an object from XML
        /// </summary>
        public static T Deserialise<T>(string xml, Encoding encoding)
            where T : class, new()
        {
            using (var stream = new MemoryStream(encoding.GetBytes(xml)))
            {
                var serialiser = new DataContractSerializer(typeof(T));
                return serialiser.ReadObject(stream) as T;
            }
        }
    }
}
