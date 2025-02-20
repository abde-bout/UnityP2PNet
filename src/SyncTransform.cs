using P2PNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UIElements;
using UnityP2PNet;

namespace Zones
{
    public class SyncTransform : NetMonoBehaviour
    {
        [Header("Sync Transform")]
        [SerializeField] private float syncUdpRate = 0.1f;
        [SerializeField] private float forceSyncUdpRate = 1f; //correction for possible udp paquet loss
        [SerializeField] private float interpolationDemp = 8f;

        Vector3 _position;
        Quaternion _rotation;
        Vector3 _scale;

        private void OnEnable()
        {
            if (IsOwner)
            {
                StartCoroutine(SyncRoutine(false, syncUdpRate));
                StartCoroutine(SyncRoutine(true, forceSyncUdpRate));
            }
        }

        public override void InitNetIdentity()
        {
            base.InitNetIdentity();

            _position = transform.position;
            _rotation = transform.rotation;
            _scale = transform.localScale;
        }

        IEnumerator SyncRoutine(bool forceSync, float rate)
        {
            WaitForSeconds wait = new(rate);

            while (enabled)
            {
                Sync(forceSync);
                yield return wait;
            }
        }

        void Sync(bool forceSync)
        {
            if (_position != transform.position ||
                _rotation != transform.rotation ||
                _scale != transform.localScale ||
                forceSync)
            {
                _position = transform.position;
                BroadcastMethod(nameof(SyncTransformState), Filter.AllExceptSelf,
                    (SVector3)_position,
                    (SQuaternion)_rotation,
                    (SVector3)_scale);
            }
        }

        private void Update()
        {
            if (!IsOwner)
            {
                if (transform.position != _position)
                    transform.position = Vector3.Lerp(transform.position, _position, interpolationDemp * Time.deltaTime);

                if (transform.rotation != _rotation)
                    transform.rotation = Quaternion.Lerp(transform.rotation, _rotation, interpolationDemp * Time.deltaTime);

                if (transform.localScale != _scale)
                    transform.localScale = Vector3.Lerp(transform.localScale, _scale, interpolationDemp * Time.deltaTime);
            }
        }

        [Sync(ProtocolType.Udp)]
        void SyncTransformState(SVector3 position, SQuaternion rotation, SVector3 scale)
        {
            _position = position;
            _rotation = rotation;
            _scale = scale;
        }
    }
}

