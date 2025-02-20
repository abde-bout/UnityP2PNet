using P2PNet.Serialization;
using Unity.VisualScripting;
using UnityEngine;

namespace UnityP2PNet
{
    public struct SQuaternion : ISerializable
    {
        public float x;
        public float y;
        public float z;

        public SQuaternion(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public SQuaternion(Quaternion quaternion) : this(quaternion.eulerAngles.x, quaternion.eulerAngles.y, quaternion.eulerAngles.z) { }

        public byte[] Serialize(object obj)
        {
            var sQuaternion = (SQuaternion)obj;
            return Serializer.SerializeItems(sQuaternion.x, sQuaternion.y, sQuaternion.z);
        }

        public object Deserialize(Serializer serializer)
        {
            return new SQuaternion(
                serializer.GetNextItem<float>(),  // x
                serializer.GetNextItem<float>(),  // y
                serializer.GetNextItem<float>()   // z
            );
        }

        public static implicit operator Quaternion(SQuaternion s) => Quaternion.Euler(s.x, s.y, s.z);
        public static implicit operator SQuaternion(Quaternion q) => new SQuaternion(q);
    }
}