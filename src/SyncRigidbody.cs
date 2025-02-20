using P2PNet;
using System;
using System.Collections;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityP2PNet
{
    [RequireComponent(typeof(Rigidbody))]
    public class SyncRigidbody : NetMonoBehaviour
    {
        [Header("Sync Rigidbody")]
        [SerializeField] private float syncUdpRate = 0.1f;
        [SerializeField] private float forceSyncUdpRate = 1f; // correction for possible UDP packet loss
        [SerializeField] private float interpolationDemp = 8f;

        private Vector3 _position;
        private Quaternion _rotation;
        private Vector3 _velocity;
        private Vector3 _angularVelocity;

        private Rigidbody _rigidbody;

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

            _rigidbody = GetComponent<Rigidbody>();

            // Store initial values
            _position = _rigidbody.position;
            _rotation = _rigidbody.rotation;
            _velocity = _rigidbody.linearVelocity;
            _angularVelocity = _rigidbody.angularVelocity;
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
            if (_position != _rigidbody.position ||
                _rotation != _rigidbody.rotation ||
                _velocity != _rigidbody.linearVelocity ||
                _angularVelocity != _rigidbody.angularVelocity ||
                forceSync)
            {
                _position = _rigidbody.position;
                _rotation = _rigidbody.rotation;
                _velocity = _rigidbody.linearVelocity;
                _angularVelocity = _rigidbody.angularVelocity;
                BroadcastMethod(nameof(SyncRigidbodyState), Filter.AllExceptSelf,
                    (SVector3)_position,
                    (SQuaternion)_rotation, 
                    (SVector3)_velocity,
                    (SVector3)_angularVelocity);
            }
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
            {
                if (_rigidbody.position != _position)
                    _rigidbody.MovePosition(Vector3.Lerp(_rigidbody.position, _position, interpolationDemp * Time.deltaTime));

                if (_rigidbody.rotation != _rotation)
                    _rigidbody.MoveRotation(Quaternion.Lerp(_rigidbody.rotation, _rotation, interpolationDemp * Time.deltaTime));

                if (_rigidbody.linearVelocity != _velocity)
                    _rigidbody.linearVelocity = Vector3.Lerp(_rigidbody.linearVelocity, _velocity, interpolationDemp * Time.deltaTime);

                if (_rigidbody.angularVelocity != _angularVelocity)
                    _rigidbody.angularVelocity = Vector3.Lerp(_rigidbody.angularVelocity, _angularVelocity, interpolationDemp * Time.deltaTime);
            }
        }

        [Sync(ProtocolType.Udp)]
        void SyncRigidbodyState(SVector3 position, SQuaternion rotation, SVector3 velocity, SVector3 angularVelocity)
        {
            _position = position;
            _rotation = rotation;
            _velocity = velocity;
            _angularVelocity = angularVelocity;
        }
    }
}
