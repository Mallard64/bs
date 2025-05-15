using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
    public int bulletDamage = 10;
    public float knockbackBase = 18f;
    public float knockbackScaling = 1.4f;
    public int shooterId;

    private Vector2 lastPosition;
    private Vector2 moveDirection;
    
    [SerializeField] private float outwardOffset = 5f;

    public GameObject parent;

    Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    [ServerCallback]
    void OnCollisionEnter2D(Collision2D col)
    {
        var enemy = col.gameObject.GetComponent<Enemy>();
        if (enemy != null
            && enemy.connectionToClient != null
            && shooterId != enemy.connectionId)
        {
            // 1) Damage
            enemy.TakeDamage(bulletDamage);

            // 2) Knockback magnitude (Smash‐style)
            float d = bulletDamage;
            float w = enemy.weight;
            float dm = enemy.health;
            float baseComp = (d * 0.1f) + (d * dm / 20f);
            float kb = ((baseComp * knockbackScaling) * (200f / (w + 100f)))
                             + knockbackBase;

            // 3) Choose direction:
            //    • If parent‐attached, use our tracked moveDirection
            //    • else if physics is moving us, use rb.velocity
            //    • else fallback to collision normal
            Vector2 dir;
            if (parent != null && moveDirection.sqrMagnitude > 0.1f)
            {
                dir = moveDirection;
            }
            else if (rb.velocity.sqrMagnitude > 0.1f)
            {
                dir = rb.velocity.normalized;
            }
            else
            {
                dir = -col.contacts[0].normal;
            }

            // 4) Send RPC
            float stun = (kb * 0.4f) / 60f;
            enemy.TargetApplyKnockback(enemy.connectionToClient, dir * kb, stun);

            // 5) Destroy
            NetworkServer.Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (parent != null)
        {
            // 1) get parent’s current position
            Vector2 parentPos = parent.transform.position;

            // 2) compute how far we moved since last frame
            Vector2 delta = parentPos - lastPosition;
            if (delta.sqrMagnitude > 0.001f)
                moveDirection = delta.normalized;
            lastPosition = parentPos;

            // 3) snap us to follow, but pushed out along our movement direction
            transform.position = parentPos + moveDirection * outwardOffset;
        }
    }


}
