// Weapon.cs - Complete updated version
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

    public int maxSwaps;

    public bool isSpecial;

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
        if (!isSpecial)
        {
            Animator targetAnimator = GetComponent<Animator>();
            if (targetAnimator != null)
            {
                targetAnimator.Play("shoot");
                timer = timerMax;
            }
        }
        else
        {
            Animator targetAnimator = GetComponent<Animator>();
            if (targetAnimator != null)
            {
                targetAnimator.Play("shoot" + parent.GetComponent<MouseShooting>().swapModeNum.ToString());
                timer = timerMax;
            }
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
            parent.GetComponent<MouseShooting>().swapModeNumMax = maxSwaps;
            parent.GetComponent<MouseShooting>().hasSpecial = isSpecial;
            parent.GetComponent<MouseShooting>().bulletSpeed = bulletSpeed;
            parent.GetComponent<MouseShooting>().bulletLifetime = bulletLifetime;
            transform.position = parent.transform.position;

            // Update shot cooldown based on weapon mode for sword weapons
            var mouseShooting = parent.GetComponent<MouseShooting>();
            if (id == 2) // Sword weapon
            {
                switch (mouseShooting.swapModeNum)
                {
                    case 0: // Stab - fastest
                        mouseShooting.shotCooldownTime = 0.3f;
                        break;
                    case 1: // Slice - medium
                        mouseShooting.shotCooldownTime = 0.6f;
                        break;
                    case 2: // Throw - slowest, consumes ammo
                        mouseShooting.shotCooldownTime = 1.2f;
                        break;
                }
            }
            else
            {
                mouseShooting.shotCooldownTime = end + startup + shot;
            }
        }

        if (parent != null)
        {
            // Sync weapon net ID with parent's MouseShooting
            if (parent.GetComponent<MouseShooting>().weaponNetId == 0)
            {
                parent.GetComponent<MouseShooting>().weaponNetId = GetComponent<NetworkIdentity>().netId;
            }

            h = true;

            // Sync ammo values - special handling for sword modes
            var mouseShooting = parent.GetComponent<MouseShooting>();
            if (id == 2 && mouseShooting.swapModeNum == 2) // Sword throw mode
            {
                // Thrown swords consume ammo
                if (maxAmmo != mouseShooting.maxAmmo)
                {
                    mouseShooting.maxAmmo = maxAmmo;
                    mouseShooting.currentAmmo = maxAmmo;
                }
            }
            else if (id == 2) // Other sword modes don't consume ammo
            {
                mouseShooting.maxAmmo = 999; // Infinite ammo for stab/slice
                mouseShooting.currentAmmo = 999;
            }
            else
            {
                // Normal weapon ammo handling
                if (maxAmmo != mouseShooting.maxAmmo)
                {
                    mouseShooting.maxAmmo = maxAmmo;
                    mouseShooting.currentAmmo = maxAmmo;
                }
            }

            // Handle movement-based rotation when not shooting
            if (!mouseShooting.isShooting && !mouseShooting.isAiming)
            {
                float angle = Mathf.Atan2(parent.GetComponent<Rigidbody2D>().velocity.y, parent.GetComponent<Rigidbody2D>().velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
                GetComponent<SpriteRenderer>().flipY = ((angle + 360) % 360) > 180;
            }
            else if (mouseShooting.isAiming && !mouseShooting.isShooting)
            {
                float angle = Mathf.Atan2(mouseShooting.v.y, mouseShooting.v.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
                GetComponent<SpriteRenderer>().flipY = ((angle + 360) % 360) > 180;
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