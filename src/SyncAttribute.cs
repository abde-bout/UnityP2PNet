using P2PNet;
using System;
using System.Net.Sockets;

namespace UnityP2PNet
{
    public class SyncAttribute : Attribute
    {
        public ProtocolType Protocole { get; }
        public BroadcastPermission BroadcastPermission { get; }

        public SyncAttribute() : this(ProtocolType.Tcp, BroadcastPermission.Owner) { }
        public SyncAttribute(ProtocolType transport) : this(transport, BroadcastPermission.Owner) { }
        public SyncAttribute(BroadcastPermission broadcastPermission) : this(ProtocolType.Tcp, broadcastPermission) { }
        public SyncAttribute (ProtocolType protocole, BroadcastPermission broadcastPermission)
        {
            Protocole = protocole;
            BroadcastPermission = broadcastPermission;
        }
    }
}