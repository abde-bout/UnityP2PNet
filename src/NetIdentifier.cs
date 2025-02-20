using System;
using UnityEngine;
using AbdeUnity.Attributes;
using P2PNet;
using UnityEditor;

namespace UnityP2PNet
{
    [DisallowMultipleComponent]
    public class NetIdentifier : MonoBehaviour
    {
        public int GameobjectID => _gameobjectId;
        public bool IsOwner => _owner;
        public int ClientID => _clientId;

        [Header("Net-Id")]
        [SerializeField, ReadOnly] int _gameobjectId = -1;
        [SerializeField, ReadOnly] bool _owner;
        [SerializeField, ReadOnly] int _clientId;
        [SerializeField, ReadOnly] NetMonoBehaviour[] netMonos;

        private void Start()
        {
            if (_gameobjectId == -1)
            {
                throw new InvalidOperationException($"Net Identifier was not properly initialized.\n" +
                    $"Net-gameobjects needs to be instantiated using {nameof(NetMaster.InstantiateNetPrefab)}.\n" +
                    $"Gameobject: {name}");
            }
        }

        internal void InitNetIdentity(int gameobjectId, bool owner, int clientId)
        {
            _gameobjectId = gameobjectId;
            _owner = owner;
            _clientId = clientId;

            foreach (var netMono in netMonos)
            {
                netMono.InitNetIdentity();
            }
        }

        public void BroadcastMethod(NetMonoBehaviour netMono, string methodName, int clientId, object[] parameters)
        {
            NetMaster.Current.PublishBroadcastMethod(netMono, this, methodName, clientId, parameters);
        }

        public void BroadcastMethod(NetMonoBehaviour netMono, string methodName, object[] parameters, Filter filter)
        {
            NetMaster.Current.PublishBroadcastMethod(netMono, this, methodName, parameters, filter);
        }

        public void RequestBroadcastMethod(NetMonoBehaviour netMono, string methodName, object[] parameters)
        {
            NetMaster.Current.PublishRequestBroadcastMethod(netMono, this, methodName, parameters);
        }

        public bool ContainsNetMonoIndex(int index)
        {
            return index >= 0 && index < netMonos.Length;
        }

        public NetMonoBehaviour GetNetMono(int index)
        {
            return netMonos[index];
        }

        public void LinqNetMonos()
        {
            netMonos = GetComponentsInChildren<NetMonoBehaviour>();

            for (int i = 0; i < netMonos.Length; i++)
            {
                netMonos[i].NetMonoIndex = i;
            }
        }
    }
}
