#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

#endregion

namespace Virtuoso.Client.Core
{
    public static class JSONSerializer
    {
        public static string SerializeToJsonString(object objectToSerialize, List<Type> knownTypes = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer = null;
                if (knownTypes == null)
                {
                    serializer = new DataContractJsonSerializer(objectToSerialize.GetType());
                }
                else
                {
                    serializer = new DataContractJsonSerializer(objectToSerialize.GetType(), knownTypes);
                }

                serializer.WriteObject(ms, objectToSerialize);
                ms.Position = 0;
                using (StreamReader reader = new StreamReader(ms))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static T Deserialize<T>(string jsonString, List<Type> knownTypes = null)
        {
            using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                DataContractJsonSerializer serializer = null;
                if (knownTypes == null)
                {
                    serializer = new DataContractJsonSerializer(typeof(T));
                }
                else
                {
                    serializer = new DataContractJsonSerializer(typeof(T), knownTypes);
                }

                return (T)serializer.ReadObject(ms);
            }
        }
    }
}