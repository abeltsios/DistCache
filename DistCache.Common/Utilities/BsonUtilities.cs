using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.IO;
using System.IO;

namespace DistCache.Common.Utilities
{
    public static class BsonUtilities
    {

        public static byte[] Serialise<T>(T t)
        {
            using (var memstream = new MemoryStreamPool())
            {
                using (var serialiser = new BsonBinaryWriter(memstream.Stream))
                {
                    BsonSerializer.Serialize(serialiser, typeof(T), t);
                    return memstream.Stream.ToArray();
                }
            }
        }

        public static T Deserialise<T>(byte[] data)
        {
            return (T)BsonSerializer.Deserialize(data, typeof(T));
        }
    }
}
