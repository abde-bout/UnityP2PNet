using AbdeUnity.Framework;
using AbdeUnity.Miscs;
using P2PNet;
using P2PNet.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using static UnityP2PNet.NetSettingsData;

namespace UnityP2PNet
{
    public class NetMaster : PersistentSingleton<NetMaster>
    {
        public static readonly Code INSTANTIATE_GO_CODE = Code.Register(106, ProtocolType.Tcp);
        public static readonly Code DESTROY_GO_CODE = Code.Register(107, ProtocolType.Tcp);
        public static readonly Code BROADCAST_METHOD_TCP_CODE = Code.Register(108, ProtocolType.Tcp);
        public static readonly Code BROADCAST_METHOD_UDP_CODE = Code.Register(109, ProtocolType.Udp);

        public int ClientID => Service.SelfClient != null ? Service.SelfClient.ID : -1;
        public bool IsHost => Service.SelfClient != null ? Service.SelfClient.Host : false;
        public P2PService Service => _service;

        [Header("Settings")]
        [SerializeField] private NetSettingsData settings;

        P2PService _service;

        Queue<int> _localGameobjectIDPool;
        Dictionary<int, NetIdentifier> _netIdentifierMap;
        Queue<Action> _mainThreadBuffer;
        Dictionary<int, Action<Packet>> _codeCallbackMap;

        //cache
        NetSettingsDataCache _settingsCache;

        protected override void OnSingletonLoaded()
        {
            _settingsCache = new NetSettingsDataCache(settings);
            _mainThreadBuffer = new();
            _service = new();
            _localGameobjectIDPool = new(); for (int i = 0; i < settings.MaxGameObjects; i++) _localGameobjectIDPool.Enqueue(i);
            _netIdentifierMap = new();
            _codeCallbackMap = new()
            {
                { INSTANTIATE_GO_CODE, OnInstantiateNetPrefab },
                { DESTROY_GO_CODE, OnDestroyNetGameobject },
                { BROADCAST_METHOD_TCP_CODE, OnBroadcastMethod },
                { BROADCAST_METHOD_UDP_CODE, OnBroadcastMethod },
            };

            _service.OnPacketReception += OnPaquetReception;
            _service.OnFailToSend += OnFailToSend;
        }

        void Update()
        {
            if (_mainThreadBuffer.Count > 0)
            {
                lock (_mainThreadBuffer)
                {
                    while (_mainThreadBuffer.Count > 0)
                    {
                        _mainThreadBuffer.Dequeue()();
                    }
                }
            }
        }

        public void EnqueueMainThreadBuffer(Action action)
        {
            lock (_mainThreadBuffer)
            {
                _mainThreadBuffer.Enqueue(action);
            }
        }

        void OnPaquetReception(Packet packet)
        {
            EnqueueMainThreadBuffer(() =>
            {
                if (_codeCallbackMap.TryGetValue(packet.Code, out var callback))
                {
                    callback(packet);
                }
            });
        }

        void OnFailToSend(IPEndPoint ip, Exception e, Packet packet)
        {
            EnqueueMainThreadBuffer(() => 
            {
                Debug.LogWarning($"Failed to send: {ip}\n" +
                    $"{packet}\n" +
                    $"{e}");
            });
        }

        void OnBroadcastMethod(Packet packet)
        {
            var networkPayload = Serializer.DeserializeItem<NetworkPayload>(packet.Bytes);

            //gameobject id could not be found (could be a destroyed netidentifier)
            if (!_netIdentifierMap.TryGetValue(networkPayload.GameobjectID, out var netIdentifier))
            {
                Debug.LogWarning($"NetIdentifier with id {networkPayload.GameobjectID} could not be found.");
                return;
            }

            //componenet index out of bounds
            if (!netIdentifier.ContainsNetMonoIndex(networkPayload.ComponentIndex))
                throw new IndexOutOfRangeException($"p2pComponentIndex is out of range: {networkPayload.ComponentIndex}");

            using Serializer serializer = new(networkPayload.Bytes);

            //retrive method info
            var methodInfoIndex = serializer.GetNextItem<int>();
            var methodInfo = _settingsCache.GetMethodInfo(methodInfoIndex);

            //invoke method
            var parameters = ReflectionHelper.DeserializeParameters(methodInfo, serializer);
            var netMono = netIdentifier.GetNetMono(networkPayload.ComponentIndex);
            methodInfo.Invoke(netMono, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, null, parameters, CultureInfo.CurrentCulture);
        }

        internal void RegisterNetIdentifier(int netGameobjectId, bool owner, int clientId, NetIdentifier netIdentifier)
        {
            _netIdentifierMap.Add(netGameobjectId, netIdentifier);
            netIdentifier.InitNetIdentity(netGameobjectId, owner, clientId);
        }

        public bool TryGetNetIdentifier(int goId, out NetIdentifier netIdentifier)
        {
            if (_netIdentifierMap.TryGetValue(goId, out netIdentifier))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public GameObject InstantiateNetPrefab(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return InsstantiateNetPrefab(_settingsCache.GetPrefabIndex(prefab), position, rotation);
        }

        public GameObject InsstantiateNetPrefab(int prefabIndex, Vector3 position, Quaternion rotation)
        {
            var instance = Instantiate(_settingsCache.GetPrefab(prefabIndex), position, rotation);

            var netIdentifier = instance.GetComponent<NetIdentifier>();

            var goId = NextGameobjectNetID();

            _service.Send(INSTANTIATE_GO_CODE, Filter.AllExceptSelf, new object[] 
            {
                goId, prefabIndex, new SVector3(position), new SQuaternion(rotation)
            });

            RegisterNetIdentifier(goId, true, Service.SelfClient.ID, netIdentifier);

            return instance;
        }

        void OnInstantiateNetPrefab(Packet packet)
        {
            Serializer serializer = new(packet.Bytes);
            var netGameobjectId = serializer.GetNextItem<int>();
            var prefabIndex = serializer.GetNextItem<int>();
            var position = serializer.GetNextItem<SVector3>();
            var rotation = serializer.GetNextItem<SQuaternion>();

            var instance = Instantiate(_settingsCache.GetPrefab(prefabIndex), position, rotation);

            var netIdentifier = instance.GetComponent<NetIdentifier>();

            var senderCLient = _service.GetClient(packet);

            RegisterNetIdentifier(netGameobjectId, false, senderCLient.ID, netIdentifier);
        }

        public void DestroyNetGameobject(int goID)
        {
            //ignore if goid not found since two requests destory can come at the same time

            if (_netIdentifierMap.TryGetValue(goID, out var netIdentifier))
                DestroyNetGameobject(netIdentifier);
            else
                Debug.LogWarning($"Gameobject id ({goID}) is not registered.");
        }
        public void DestroyNetGameobject(GameObject go)
        {
            if (!go.TryGetComponent<NetIdentifier>(out var netIdentifier))
                throw new NullReferenceException("Cannot destroy go without a " + typeof(NetIdentifier));

            DestroyNetGameobject(netIdentifier);
        }
        public void DestroyNetGameobject(NetIdentifier netIdentifier)
        {
            if (!netIdentifier.IsOwner)
                throw new AccessViolationException("Cannot destory non-owned gamebojects");

            FreeGameobjectNetID(netIdentifier.GameobjectID);

            _netIdentifierMap.Remove(netIdentifier.GameobjectID);
            Destroy(netIdentifier.gameObject);

            _service.Send(DESTROY_GO_CODE, Filter.AllExceptSelf, netIdentifier.GameobjectID);
        }

        void OnDestroyNetGameobject(Packet paquet)
        {
            var gameobjectID = Serializer.DeserializeItem<int>(paquet.Bytes);

            if (_netIdentifierMap.TryGetValue(gameobjectID, out var netIdentifier))
            {
                _netIdentifierMap.Remove(netIdentifier.GameobjectID);
                Destroy(netIdentifier.gameObject);
            }
            else
            {
                throw new NullReferenceException("Gameobject_id could not be found to destroy go " + gameobjectID);
            }
        }

        public void PublishBroadcastMethod(NetMonoBehaviour netMono, NetIdentifier netIdentifier, string methodName, int clientId, object[] methodParameters)
        {
            SendBroadcastMethod(netMono, netIdentifier, methodName, clientId, methodParameters);
        }

        public void PublishBroadcastMethod(NetMonoBehaviour netMono, NetIdentifier netIdentifier, string methodName, object[] methodParameters, Filter filter)
        {
            SendBroadcastMethod(netMono, netIdentifier, methodName, methodParameters, filter);
        }

        void SendBroadcastMethod(NetMonoBehaviour netMono, NetIdentifier netIdentifier, string methodName, object[] methodParameters, Filter filter)
        {
            var metadata = GetMethodMetadata(netMono, methodName, methodParameters, out var methodInfoIndex);

            if (netIdentifier.IsOwner)
            {
                var netPayload = GetNetworkPayload(netMono, netIdentifier, methodInfoIndex, methodParameters);
                _service.Send(GetBroadcastMethodCode(metadata.Protocole), filter, netPayload);
            }
            else
            {
                throw new AccessViolationException($"Restricted access to broadcast methods on this object ({netMono.name})");
            }
        }

        void SendBroadcastMethod(NetMonoBehaviour netMono, NetIdentifier netIdentifier, string methodName, int clientId, object[] methodParameters)
        {
            var metadata = GetMethodMetadata(netMono, methodName, methodParameters, out var methodInfoIndex);

            if (netIdentifier.IsOwner)
            {
                var netPayload = GetNetworkPayload(netMono, netIdentifier, methodInfoIndex, methodParameters);
                _service.Send(GetBroadcastMethodCode(metadata.Protocole), clientId, netPayload);
            }
            else
            {
                throw new AccessViolationException($"Restricted access to broadcast methods on this object ({netMono.name})");
            }
        }

        public void PublishRequestBroadcastMethod(NetMonoBehaviour netMono, NetIdentifier netIdentifier, string methodName, object[] methodParameters)
        {
            SendRequestBroadcastMethod(netMono, netIdentifier, methodName, methodParameters);
        }

        void SendRequestBroadcastMethod(NetMonoBehaviour netMono, NetIdentifier netIdentifier, string methodName, object[] methodParameters)
        {
            var metadata = GetMethodMetadata(netMono, methodName, methodParameters, out var methodInfoIndex);

            if (netIdentifier.IsOwner || metadata.BroadcastPermission == BroadcastPermission.Request)
            {
                var netPayload = GetNetworkPayload(netMono, netIdentifier, methodInfoIndex, methodParameters);
                _service.Send(GetBroadcastMethodCode(metadata.Protocole), netMono.OwnerClient, netPayload);
            }
            else
            {
                throw new AccessViolationException($"Restricted access to request broadcast methods on this object ({netMono.name})");
            }
        }

        Code GetBroadcastMethodCode(ProtocolType protocole)
        {
            return protocole == ProtocolType.Tcp ? BROADCAST_METHOD_TCP_CODE : BROADCAST_METHOD_UDP_CODE;
        }

        NetworkPayload GetNetworkPayload(NetMonoBehaviour netMono, NetIdentifier netIdentifier, int methodInfoIndex, object[] methodParameters)
        {
            using Serializer serializer = new();
            serializer.SerializeItem(methodInfoIndex);
            for (int i = 0; i < methodParameters.Length; i++) serializer.SerializeItem(methodParameters[i]);

            return new NetworkPayload(netIdentifier.GameobjectID, netMono.NetMonoIndex, serializer.Bytes);
        }

        MethodMetadata GetMethodMetadata(object obj, string methodName, object[] methodParameters, out int methodInfoIndex)
        {
            methodInfoIndex = _settingsCache.GetMethodInfoIndex(_settingsCache.GetMethodInfo(obj, methodName, methodParameters));
            return _settingsCache.GetMethodMetadata(methodInfoIndex);
        }

        int NextGameobjectNetID()
        {
            if (_localGameobjectIDPool.Count <= 0)
            {
                throw new InvalidOperationException("Reached max net gameobjects count");
            }

            return _localGameobjectIDPool.Dequeue() + (Service.SelfClient.ID * settings.MaxGameObjects);
        }

        void FreeGameobjectNetID(int id)
        {
            _localGameobjectIDPool.Enqueue(id - (Service.SelfClient.ID * settings.MaxGameObjects));
        }

        internal int GetSharedOffsetID(int sharedId)
        {
            return sharedId + _service.MaxClient * settings.MaxGameObjects;
        }

        void OnApplicationQuit()
        {
            _service?.Stop();
        }
    }
}