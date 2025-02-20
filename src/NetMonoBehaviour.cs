using AbdeUnity.Attributes;
using P2PNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;

namespace UnityP2PNet
{
    /// <summary>
    /// Base class for <see cref="MonoBehaviour"/>s that need to communicate between clients
    /// </summary>
    [RequireComponent(typeof(NetIdentifier))]
    public abstract class NetMonoBehaviour : MonoBehaviour
    {
        public int ClientID => _netIdentifier.ClientID;
        public IReadOnlyClient OwnerClient => NetMaster.Current.Service.GetClient(_netIdentifier.ClientID);
        public bool IsOwner => _netIdentifier.IsOwner;
        public int NetMonoIndex { get => netMonoIndex; set => netMonoIndex = value; }
        protected NetIdentifier netIdentifier => _netIdentifier;

        [Header("Net-Mono")]
        [SerializeField, ReadOnly] int netMonoIndex = -1;

        NetIdentifier _netIdentifier;

        protected virtual void Awake()
        {
            _netIdentifier = GetComponent<NetIdentifier>();

            if (_netIdentifier == null)
            {
                Debug.LogError($"Net Identifier is missing. '{name}'");
            }
        }

        protected virtual void Start()
        {
            if (netMonoIndex == -1)
            {
                Debug.LogError($"'{name}' was not properly initialized. " +
                    $"Make sure this p2pMono is registered in its {typeof(NetIdentifier)}. " +
                    $"Make sure it was instantiated using {nameof(NetMaster.InstantiateNetPrefab)} {name}");
            }
        }

        /// <summary>
        /// Called when <see cref="NetMonoBehaviour"/> is ready to broadcast methods
        /// </summary>
        public virtual void InitNetIdentity()
        {
        }

        protected void BroadcastMethod(string methodName, int clientId, params object[] parameters)
        {
            _netIdentifier.BroadcastMethod(this, methodName, clientId, parameters);
        }

        /// <summary>
        /// Broadcasts method to filtered clients.
        /// </summary>
        /// <remarks>Broadcast will be denied without required permissions.</remarks>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        protected void BroadcastMethod(string methodName, Filter filter, params object[] parameters)
        {
            _netIdentifier.BroadcastMethod(this, methodName, parameters, filter);
        }

        /// <summary>
        /// Request to broadcast a method on the owner.
        /// </summary>
        /// <remarks>The request will be denied without required permissions.</remarks>
        /// <param name="methodName"></param>
        /// <param name="filter"></param>
        /// <param name="parameters"></param>
        public void RequestBroadcastMethod(string methodName, params object[] parameters)
        {
            _netIdentifier.RequestBroadcastMethod(this, methodName, parameters);
        }
    }
}

