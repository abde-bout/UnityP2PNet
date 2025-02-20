using P2PNet;
using System;
using System.Collections;
using System.Net.Sockets;
using UnityEngine;

namespace UnityP2PNet
{
    [RequireComponent(typeof(Animator))]
    public class SyncAnimator : NetMonoBehaviour
    {
        [Header("Sync Animator")]
        [SerializeField] private float syncTcpRate = 0.5f;
        [SerializeField] private float smoothing = 0.1f; // Cross-fade smoothing time

        private Animator _animator;

        private int[] _lastStateHashes;
        private float[] _lastNormalizedTimes;

        private void OnEnable()
        {
            if (IsOwner)
            {
                StartCoroutine(SyncRoutine());
            }
        }

        public override void InitNetIdentity()
        {
            base.InitNetIdentity();

            _animator = GetComponent<Animator>();
            int layerCount = _animator.layerCount;

            _lastStateHashes = new int[layerCount];
            _lastNormalizedTimes = new float[layerCount];
        }

        IEnumerator SyncRoutine()
        {
            WaitForSeconds wait = new(syncTcpRate);

            while (enabled)
            {
                CheckAndSyncState();
                yield return wait;
            }
        }

        private void CheckAndSyncState()
        {
            for (int layer = 0; layer < _animator.layerCount; layer++)
            {
                var stateInfo = _animator.GetCurrentAnimatorStateInfo(layer);
                int currentStateHash = stateInfo.fullPathHash;
                float currentNormalizedTime = stateInfo.normalizedTime;

                if (currentStateHash != _lastStateHashes[layer])
                {
                    // Register state change and broadcast
                    _lastStateHashes[layer] = currentStateHash;
                    _lastNormalizedTimes[layer] = currentNormalizedTime;

                    BroadcastMethod(nameof(SyncState), Filter.AllExceptSelf, layer, currentStateHash, currentNormalizedTime);
                }
            }
        }

        private void Update()
        {
            if (!IsOwner)
            {
                for (int layer = 0; layer < _animator.layerCount; layer++)
                {
                    int currentStateHash = _animator.GetCurrentAnimatorStateInfo(layer).fullPathHash;
                    if (currentStateHash != _lastStateHashes[layer])
                    {
                        // Smoothly transition to the new state
                        _animator.CrossFade(_lastStateHashes[layer], smoothing, layer, _lastNormalizedTimes[layer]);
                    }
                }
            }
        }

        [Sync(ProtocolType.Tcp)]
        void SyncState(int layer, int stateHash, float normalizedTime)
        {
            if (layer >= _animator.layerCount)
                return; // Ignore invalid layers

            _lastStateHashes[layer] = stateHash;
            _lastNormalizedTimes[layer] = normalizedTime;

            // Smoothly transition to the new state
            _animator.CrossFade(stateHash, smoothing, layer, normalizedTime);
        }
    }
}
