using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class Weapon : NetworkBehaviour
{
    public int currentAmmo;
    public Transform firePoint;
    public GameObject aimingSprite;
    public int slotnum;

    public Image ammoFillImage;
    public Text ammoFractionText;

    public float startup;
    public float shot;
    public float end;
    public float timerMax;

    public float bulletSpeed;

    public float bulletLifetime;

    public int id;

    [SyncVar] public int maxAmmo;

    // Sound components
    public AudioSource audioSource;
    public AudioClip shootSound;

    float timer;
    bool h = false;

    [SyncVar]
    public GameObject parent;

    void Start()
    {
        timer = 0f;

        // Get or add AudioSource component
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    // Called by MouseShooting to play shoot animation
    [ClientRpc]
    public void RpcPlayShootAnimation()
    {
        Animator targetAnimator = GetComponent<Animator>();
        if (targetAnimator != null)
        {
            targetAnimator.Play("shoot");
            timer = timerMax;
        }

        // Play shoot sound
        PlayShootSound();
    }

    // Called by MouseShooting to play shoot animation (server-side)
    public void PlayShootAnimation()
    {
        RpcPlayShootAnimation();
    }

    // Play shoot sound locally
    private void PlayShootSound()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    // Called by MouseShooting to rotate weapon to shoot direction
    public void RotateToDirection(Vector3 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        GetComponent<SpriteRenderer>().flipY = ((angle + 360) % 360) > 180;
    }

    void Update()
    {
        if (parent != null)
        {
            parent.GetComponent<MouseShooting>().bulletSpeed = bulletSpeed;
            parent.GetComponent<MouseShooting>().bulletLifetime = bulletLifetime;
            transform.position = parent.transform.position;
        }

        if (parent != null)
        {
            // Sync weapon net ID with parent's MouseShooting
            if (parent.GetComponent<MouseShooting>().weaponNetId == 0)
            {
                parent.GetComponent<MouseShooting>().weaponNetId = GetComponent<NetworkIdentity>().netId;
            }

            h = true;

            // Update parent's shot cooldown time
            parent.GetComponent<MouseShooting>().shotCooldownTime = end + startup + shot;

            // Sync ammo values
            if (maxAmmo != parent.GetComponent<MouseShooting>().maxAmmo)
            {
                parent.GetComponent<MouseShooting>().maxAmmo = maxAmmo;
                parent.GetComponent<MouseShooting>().currentAmmo = maxAmmo;
            }

            // Handle movement-based rotation when not shooting
            var mouseShooting = parent.GetComponent<MouseShooting>();
            if (!mouseShooting.isShooting && !mouseShooting.isAiming)
            {
                float angle = Mathf.Atan2(parent.GetComponent<Rigidbody2D>().velocity.y, parent.GetComponent<Rigidbody2D>().velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
                GetComponent<SpriteRenderer>().flipY = ((angle + 360) % 360) > 180;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            else if (mouseShooting.isAiming && !mouseShooting.isShooting)
            {
                float angle = Mathf.Atan2(mouseShooting.v.y, mouseShooting.v.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
                GetComponent<SpriteRenderer>().flipY = ((angle + 360) % 360) > 180;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }



            // Handle auto-aiming display
            if (mouseShooting.isAuto)
            {
                Aim(mouseShooting.d);
            }
        }

        // Handle animation timer
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

    // Called by MouseShooting to aim the weapon
    public void Aim(Vector3 dir)
    {
        var mouseShooting = parent.GetComponent<MouseShooting>();
        if (mouseShooting != null && mouseShooting.isShooting) return;

        aimingSprite.SetActive(true);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        GetComponent<SpriteRenderer>().flipY = ((angle + 360) % 360) > 180;
        aimingSprite.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // Called by MouseShooting to stop aiming
    public void StopAiming()
    {
        aimingSprite.SetActive(false);
    }
}