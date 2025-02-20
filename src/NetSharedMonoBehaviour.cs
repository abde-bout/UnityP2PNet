using AbdeUnity.Attributes;
using P2PNet;
using System;
using System.Net.Sockets;
using UnityEngine;

namespace UnityP2PNet
{
    public class NetSharedMonoBehaviour : NetMonoBehaviour
    {
        [Header("Shared-Mono"), SerializeField, ReadOnly] int sharedId;

        protected override void Awake()
        {
            base.Awake();

            //register shared net identifier
            var hostClient = NetMaster.Current.Service.GetClient(c => c.Host);
            NetMaster.Current.RegisterNetIdentifier(NetMaster.Current.GetSharedOffsetID(sharedId), NetMaster.Current.IsHost, hostClient.ID, netIdentifier);
        }

        public void SetSharedId(int sharedId) => this.sharedId = sharedId;
    }
}