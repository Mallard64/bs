using UnityEngine;
using Mirror;

public class LockRotationIfNotLocal : MonoBehaviour
{
    NetworkIdentity _ownerId;
    NetworkIdentity _localId;

    void Awake()
    {
        // find the NetworkIdentity on you or one of your parents
        _ownerId = GetComponentInParent<NetworkIdentity>();
    }

    void LateUpdate()
    {
        // cache the local player's identity once
        if (_localId == null && NetworkClient.connection != null)
            _localId = NetworkClient.connection.identity;

        // if we know both, and this sprite's owner ≠ local player, lock rotation
        if (_ownerId != null && _localId != null && _ownerId != _localId)
            transform.localRotation = Quaternion.identity;
    }
}
