using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Wynathan.Net.Serialisers
{
    public class XmlSerialiser
    {
        private const string xmlnsInstance = "xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"";
        private const string xmlnsInstancePaddedLeftSingleSpace = " " + xmlnsInstance;

        public static string Serialise<T>(T instance)
            where T : class, new()
        {
            return Serialise(instance, false);
        }

        public static string Serialise<T>(T instance, bool removeXmlnsInstance)
            where T : class, new()
        {
            return Serialise(instance, Encoding.UTF8, removeXmlnsInstance);
        }

        /// <summary>
        /// Serialises an object to XML
        /// </summary>
        public static string Serialise<T>(T instance, Encoding encoding, bool removeXmlnsInstance)
            where T : class, new()
        {
            var serialiser = new DataContractSerializer(typeof(T));
            using (var stream = new MemoryStream())
            {
                serialiser.WriteObject(stream, instance);
                var result = encoding.GetString(stream.ToArray());
                return removeXmlnsInstance
                    ? result.Replace(xmlnsInstancePaddedLeftSingleSpace, string.Empty)
                        .Replace(xmlnsInstance, string.Empty)
                    : result;
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
