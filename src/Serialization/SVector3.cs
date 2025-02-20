using P2PNet.Serialization;
using UnityEngine;

namespace UnityP2PNet
{
    public struct SVector3 : ISerializable
    {
        public float x;
        public float y;
        public float z;

        public SVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public SVector3(Vector3 vector) : this(vector.x, vector.y, vector.z) { }

        public byte[] Serialize(object obj)
        {
            var sVector = (SVector3)obj;
            return Serializer.SerializeItems(sVector.x, sVector.y, sVector.z);
        }

        public object Deserialize(Serializer serializer)
        {
            return new SVector3(
                serializer.GetNextItem<float>(),  // x
                serializer.GetNextItem<float>(),  // y
                serializer.GetNextItem<float>()   // z
            );
        }

        public static implicit operator Vector3(SVector3 s) => new Vector3(s.x, s.y, s.z);
        public static implicit operator SVector3(Vector3 q) => new SVector3(q);
    }
}