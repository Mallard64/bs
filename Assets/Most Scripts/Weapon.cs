// Weapon.cs - Fixed version for client sync
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
    [SyncVar] public uint parentNetId; // Use netId instead of GameObject
    [SyncVar] public int maxSwaps;

    public bool isSpecial;

    // Sound components
    public AudioSource audioSource;
    public AudioClip shootSound;

    [SyncVar] float timer;
    bool h = false;

    private GameObject _parent; // Cache the parent GameObject

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

    // Server sets the parent reference
    [Server]
    public void SetParent(GameObject parentObj)
    {
        if (parentObj != null)
        {
            parentNetId = parentObj.GetComponent<NetworkIdentity>().netId;
        }
    }

    // Get parent GameObject from netId
    private GameObject GetParent()
    {
        if (parentNetId == 0) return null;

        if (_parent == null || _parent.GetComponent<NetworkIdentity>().netId != parentNetId)
        {
            // Try to find parent by netId
            var spawnedDict = isServer ? NetworkServer.spawned : NetworkClient.spawned;
            if (spawnedDict.TryGetValue(parentNetId, out var parentObj))
            {
                _parent = parentObj.gameObject;
            }
        }

        return _parent;
    }

    // Called by MouseShooting to play shoot animation
    [ClientRpc]
    public void RpcPlayShootAnimation()
    {
        var parent = GetParent();
        if (parent == null) return;

        if (!isSpecial)
        {
            Animator targetAnimator = GetComponent<Animator>();
            if (targetAnimator != null)
            {
                targetAnimator.Play("shoot");
                SetTimer(timerMax); // Use server method to sync timer
            }
        }
        else
        {
            Animator targetAnimator = GetComponent<Animator>();
            if (targetAnimator != null)
            {
                targetAnimator.Play("shoot" + parent.GetComponent<MouseShooting>().swapModeNum.ToString());
                SetTimer(timerMax); // Use server method to sync timer
            }
        }

        // Play shoot sound
        PlayShootSound();
    }

    [Server]
    void SetTimer(float value)
    {
        timer = value;
    }

    // Called by MouseShooting to play shoot animation (server-side)
    [Server]
    public void PlayShootAnimation()
    {
        RpcPlayShootAnimation();
    }

    // Play shoot sound locally
    private void PlayShootSound()
    {
        if (audioSource != null && shootSound != null)
        {
            audioSource.clip = shootSound;
            audioSource.Play();
        }
    }

    // Called by MouseShooting to rotate weapon to shoot direction
    [ClientRpc]
    public void RpcRotateToDirection(Vector3 direction)
    {
        RotateToDirection(direction);
    }

    public void RotateToDirection(Vector3 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        GetComponent<SpriteRenderer>().flipY = ((angle + 360) % 360) > 180;
    }

    void Update()
    {
        var parent = GetParent();
        if (parent == null) return;

        // Both server and clients need weapon stats for shooting to work
        UpdateWeaponStats(parent);
        UpdateVisuals(parent);
        UpdateAnimationTimer();
    }

    void UpdateWeaponStats(GameObject parent)
    {
        var mouseShooting = parent.GetComponent<MouseShooting>();
        if (mouseShooting == null) return;

        mouseShooting.swapModeNumMax = maxSwaps;
        mouseShooting.hasSpecial = isSpecial;
        mouseShooting.bulletSpeed = bulletSpeed;
        mouseShooting.bulletLifetime = bulletLifetime;

        // Update shot cooldown based on weapon mode for sword weapons
        mouseShooting.shotCooldownTime = end + startup + shot;
        if (id == 2) // Sword weapon
        {
            switch (mouseShooting.swapModeNum)
            {
                case 0: mouseShooting.shotCooldownTime = 0.3f; break;
                case 1: mouseShooting.shotCooldownTime = 0.6f; break;
                case 2: mouseShooting.shotCooldownTime = 1.2f; break;
            }
        }

        // Sync weapon net ID with parent's MouseShooting
        if (mouseShooting.weaponNetId == 0)
        {
            mouseShooting.weaponNetId = GetComponent<NetworkIdentity>().netId;
        }

        // Handle ammo sync - only server should modify ammo values
        if (isServer)
        {
            if (id == 2 && mouseShooting.swapModeNum == 2) // Sword throw mode
            {
                if (maxAmmo != mouseShooting.maxAmmo)
                {
                    mouseShooting.maxAmmo = maxAmmo;
                    mouseShooting.currentAmmo = maxAmmo;
                }
            }
            else if (id == 2) // Other sword modes don't consume ammo
            {
                mouseShooting.maxAmmo = 999;
                mouseShooting.currentAmmo = 999;
            }
            else
            {
                if (maxAmmo != mouseShooting.maxAmmo)
                {
                    mouseShooting.maxAmmo = maxAmmo;
                    mouseShooting.currentAmmo = maxAmmo;
                }
            }
        }
    }

    void UpdateVisuals(GameObject parent)
    {
        // Don't manually set position if this weapon is a child of parent
        // The Transform hierarchy handles positioning automatically
        // Only set position if this weapon is NOT a child
        transform.position = parent.transform.position;

        var mouseShooting = parent.GetComponent<MouseShooting>();
        if (mouseShooting == null) return;

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
        if (mouseShooting.isAuto && parent.GetComponent<NetworkIdentity>().isLocalPlayer)
        {
            Aim(mouseShooting.d);
        }
    }


    void UpdateAnimationTimer()
    {
        Animator targetAnimator = GetComponent<Animator>();
        if (targetAnimator != null && timer <= 0.01f)
        {
            targetAnimator.Play("default");
        }
        else if (isServer)
        {
            timer -= Time.deltaTime;
        }
    }

    // Called by MouseShooting to aim the weapon
    public void Aim(Vector3 dir)
    {
        var parent = GetParent();
        if (parent == null) return;

        var mouseShooting = parent.GetComponent<MouseShooting>();
        if (mouseShooting != null && mouseShooting.isShooting) return;

        // Check if the parent is owned by the local client
        if (parent != null)
        {
            var parentIdentity = parent.GetComponent<NetworkIdentity>();
            bool showAimSprite = parentIdentity.isLocalPlayer || (isClient && parentIdentity.isOwned);

            if (showAimSprite)
            {
                aimingSprite.SetActive(true);
                aimingSprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }
        }

        // Rotation happens for everyone
        RotateToDirection(dir);
    }

    // Called by MouseShooting to stop aiming
    public void StopAiming()
    {
        aimingSprite.SetActive(false);
    }
}