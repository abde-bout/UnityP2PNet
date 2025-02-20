using P2PNet.Serialization;

namespace UnityP2PNet
{
    public struct NetworkPayload : ISerializable
    {
        public int GameobjectID { get; }
        public int ComponentIndex { get; }
        public byte[] Bytes { get; }

        public NetworkPayload(int gameobjectID, int componentIndex, byte[] bytes)
        {
            GameobjectID = gameobjectID;
            ComponentIndex = componentIndex;
            Bytes = bytes;
        }

        public byte[] Serialize(object obj)
        {
            var payload = (NetworkPayload)obj;
            return Serializer.SerializeItems(
                payload.GameobjectID,
                payload.ComponentIndex,
                payload.Bytes
            );
        }

        public object Deserialize(Serializer serializer)
        {
            return new NetworkPayload(
                serializer.GetNextItem<int>(),         // GameobjectID
                serializer.GetNextItem<int>(),         // ComponentIndex
                serializer.GetNextItem<byte[]>()         // Bytes
            );
        }
    }
}