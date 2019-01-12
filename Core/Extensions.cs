using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace StorageManagementKit.Core
{
    public static class DataExtensions
    {
        public static string ObjectToXml(this object obj)
        {
            XmlSerializer sr = new XmlSerializer(obj.GetType());

            using (MemoryStream stm = new MemoryStream())
            {
                sr.Serialize(stm, obj);
                stm.Position = 0;

                return Encoding.Default.GetString(stm.ToArray());
            }
        }

        public static void WriteToXmlFileUtf8(this object obj, string filename)
        {
            File.WriteAllText(filename, obj.ObjectToXmlUtf8());
        }

        public static string ObjectToXmlUtf8(this object obj)
        {
            XmlSerializer sr = new XmlSerializer(obj.GetType());

            using (MemoryStream stm = new MemoryStream())
            {
                sr.Serialize(stm, obj);
                stm.Position = 0;

                return Encoding.UTF8.GetString(stm.ToArray());
            }
        }

        public static T XmlToObject<T>(this string text)
        {
            XmlSerializer sr = new XmlSerializer(typeof(T));

            using (TextReader reader = new StringReader(text))
                return (T)sr.Deserialize(reader);
        }

        public static byte[] ToBytes(this object obj)
        {
            using (var stm = new MemoryStream())
            {
                XmlSerializer sr = new XmlSerializer(obj.GetType());
                sr.Serialize(stm, obj);
                stm.Position = 0;
                return stm.ToArray();
            }
        }
    }
}
