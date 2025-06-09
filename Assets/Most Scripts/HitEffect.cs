// HitEffect.cs
using System.Collections;
using Mirror;
using UnityEngine;

public class HitEffect : NetworkBehaviour
{
    [SerializeField] Animator animator;
    [Tooltip("How long before the server destroys this object")]
    [SerializeField] public float lifetime = 1f;

    /// <summary>
    /// Called by the spawner RPC to start the VFX.
    /// </summary>
    [ClientRpc]
    public void RpcPlayEffect()
    {
        if (animator != null)
            animator.Play(animator.GetCurrentAnimatorStateInfo(0).shortNameHash, 0, 0f);
    }

    [ServerCallback]
    IEnumerator Start()
    {
        // Wait lifetime seconds on the server, then destroy everywhere
        yield return new WaitForSeconds(lifetime);
        NetworkServer.Destroy(gameObject);
    }
}
