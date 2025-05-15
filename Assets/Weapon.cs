using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Weapon : NetworkBehaviour
{
    public int currentAmmo;
    public Transform firePoint;
    public GameObject bulletPrefab;
    public float bulletSpeed = 5f;
    public float bulletLifetime = 5f;
    public GameObject aimingSprite;
    public int slotnum;

    

    public float startup;

    public int id;

    float timer;

    public float shot;
    public float end;

    public float timerMax;

    [SyncVar] public int maxAmmo;

    bool h = false;

    [SyncVar]
    bool isShooting = false;

    [SyncVar]
    public GameObject parent;

    // Start is called before the first frame update
    void Start()
    {
        timer = 0f;
    }


    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterPrefab(bulletPrefab);
    }


    // Update is called once per frame
    void Update()
    {
        if (parent != null)
        {
            transform.position = parent.transform.position;
        }
        if (parent != null && !isShooting)
        {

            if (parent.GetComponent<MouseShooting>().weaponNetId == 0) {
                parent.GetComponent<MouseShooting>().weaponNetId = GetComponent<NetworkIdentity>().netId;
            }

            h = true;
            
            parent.GetComponent<MouseShooting>().shotCooldownTime = end+startup+shot;

            if (maxAmmo != parent.GetComponent<MouseShooting>().maxAmmo)
            {
                parent.GetComponent<MouseShooting>().maxAmmo = maxAmmo;
                parent.GetComponent<MouseShooting>().currentAmmo = maxAmmo;
            }

            float angle = Mathf.Atan2(parent.GetComponent<Rigidbody2D>().velocity.y, parent.GetComponent<Rigidbody2D>().velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            GetComponent<SpriteRenderer>().flipY = ((angle + 360) % 360) > 180;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            if (parent.GetComponent<MouseShooting>().isAuto)
            {
                Aim(parent.GetComponent<MouseShooting>().d);
            }

            if (parent.GetComponent<MouseShooting>().isAiming)
            {
                
                Aim(parent.GetComponent<MouseShooting>().v);
                
            }
            else
            {
                aimingSprite.SetActive(false);
            }
        }
        Animator targetAnimator = GetComponent<Animator>();
        if (targetAnimator != null && timer <= 0.01f)
        {
            targetAnimator.Play("default");
        }
        else
        {
            timer -= Time.deltaTime;
        }
        
    }

    public void Shoot(Vector3 dir)
    {
        Animator targetAnimator = GetComponent<Animator>();
        if (targetAnimator != null)
        {
            targetAnimator.Play("shoot");
            timer = timerMax;
        }
        isShooting = true;
        
        StartCoroutine(Startup(dir));
        
     
    }

    public IEnumerator Startup(Vector3 dir)
    {
        yield return new WaitForSeconds(startup);
        currentAmmo--;
        //if (!isLocalPlayer) return;
        if (id == 0)
        {
            CmdShootSniper(dir);
        }
        else if (id == 1)
        {
            CmdShootShotgun(dir);
        }
        else if (id == 2)
        {
            CmdShootKnife(dir);
        }
        else
        {
            CmdShootAR(dir);
        }
        StartCoroutine(ShotTime());
    }

    public IEnumerator ShotTime()
    {
        yield return new WaitForSeconds(shot);
        StartCoroutine(EndTime());
    }

    public IEnumerator EndTime()
    {
        yield return new WaitForSeconds(end);
        isShooting = false;
        parent.GetComponent<MouseShooting>().isAuto = false;
    }

    public void Aim(Vector3 dir)
    {
        aimingSprite.SetActive(true);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        GetComponent<SpriteRenderer>().flipY = ((angle + 360) % 360) > 180;
        aimingSprite.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    [Command(requiresAuthority = false)]
    public virtual void CmdShootSniper(Vector3 direction)
    {
        Vector3 spawnPosition = transform.position + direction.normalized * 0.6f;
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        bullet.transform.rotation = transform.rotation;
        bullet.GetComponent<Rigidbody2D>().velocity = direction.normalized * bulletSpeed;
        bullet.GetComponent<Bullet>().shooterId = parent.GetComponent<Enemy>().connectionId;

        NetworkServer.Spawn(bullet);
        Destroy(bullet, bulletLifetime);
    }

    [Command(requiresAuthority = false)]
    public virtual void CmdShootShotgun(Vector3 direction)
    {
        int pellets = 7;            // you have 7 pellets: i = -3, -2, …, +3
        float maxSpread = 45f;      // total spread (°) from center to top/bottom
        float step = maxSpread / (pellets - 1) * 2;
        // since i goes from -3 to +3, use step = (2 * maxSpread) / (pellets - 1)

        for (int i = -3; i <= 3; i++)
        {
            // compute angle: i == -3 => -maxSpread; i == +3 => +maxSpread
            float angle = i * (maxSpread / 3f);

            // rotate direction around Z (for a 2D top‐down shooter)
            Vector3 spreadDir = Quaternion.Euler(0, 0, angle) * direction.normalized;

            Vector3 spawnPos = transform.position + spreadDir * 0.6f;
            GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

            var rb = bullet.GetComponent<Rigidbody2D>();
            rb.velocity = spreadDir * bulletSpeed;

            bullet.GetComponent<Bullet>().shooterId = parent.GetComponent<Enemy>().connectionId;

            NetworkServer.Spawn(bullet);
            Destroy(bullet, bulletLifetime);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdShootKnife(Vector3 direction)
    {
        Vector3 pos = transform.position + direction.normalized * 0.3f;
        var hitbox = Instantiate(bulletPrefab, pos, Quaternion.identity);
        //hitbox.transform.SetParent(firePoint, true);
        hitbox.GetComponent<Bullet>().shooterId = parent.GetComponent<Enemy>().connectionId;
        hitbox.GetComponent<Bullet>().parent = gameObject;
        NetworkServer.Spawn(hitbox);
        Destroy(hitbox, 0.1f);
    }

    [Command(requiresAuthority = false)]
    public virtual void CmdShootAR(Vector3 direction)
    {
        int pellets = 7;            // you have 7 pellets: i = -3, -2, …, +3
        float maxSpread = 1.5f;      // total spread (°) from center to top/bottom
        // since i goes from -3 to +3, use step = (2 * maxSpread) / (pellets - 1)

        // compute angle: i == -3 => -maxSpread; i == +3 => +maxSpread
        float angle = ((float) (new System.Random()).NextDouble()) * (maxSpread);

        // rotate direction around Z (for a 2D top‐down shooter)
        Vector3 spreadDir = Quaternion.Euler(0, 0, angle) * direction.normalized;

        Vector3 spawnPos = transform.position + spreadDir * 0.6f;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        var rb = bullet.GetComponent<Rigidbody2D>();
        rb.velocity = spreadDir * bulletSpeed;

        bullet.GetComponent<Bullet>().shooterId = parent.GetComponent<Enemy>().connectionId;

        NetworkServer.Spawn(bullet);
        Destroy(bullet, bulletLifetime);
    }

}
