using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class EnvironmentalInteractionSystem : NetworkBehaviour
{
    [Header("Interactive Elements")]
    public LayerMask interactableLayer = 1 << 8; // Layer for interactable objects
    public float interactionRange = 2f;
    
    [Header("Environmental Effects")]
    public GameObject explosiveBarrelPrefab;
    public GameObject breakableWallPrefab;
    public GameObject teleporterPrefab;
    public GameObject bouncePadPrefab;
    
    // Network spawned environmental objects
    [SyncVar]
    public int environmentalObjectCount = 0;
    
    void Start()
    {
        if (isServer)
        {
            StartCoroutine(SpawnEnvironmentalObjects());
        }
    }
    
    [Server]
    IEnumerator SpawnEnvironmentalObjects()
    {
        yield return new WaitForSeconds(1f); // Wait for level to load
        
        // Spawn explosive barrels
        for (int i = 0; i < 5; i++)
        {
            Vector3 randomPos = GetRandomSpawnPosition();
            var barrel = Instantiate(explosiveBarrelPrefab, randomPos, Quaternion.identity);
            var envObject = barrel.GetComponent<EnvironmentalObject>();
            if (envObject != null)
            {
                envObject.Initialize("explosive_barrel", 50f);
            }
            NetworkServer.Spawn(barrel);
        }
        
        // Spawn breakable walls
        for (int i = 0; i < 3; i++)
        {
            Vector3 randomPos = GetRandomSpawnPosition();
            var wall = Instantiate(breakableWallPrefab, randomPos, Quaternion.identity);
            var envObject = wall.GetComponent<EnvironmentalObject>();
            if (envObject != null)
            {
                envObject.Initialize("breakable_wall", 100f);
            }
            NetworkServer.Spawn(wall);
        }
        
        // Spawn teleporters (pairs)
        for (int i = 0; i < 2; i++)
        {
            Vector3 pos1 = GetRandomSpawnPosition();
            Vector3 pos2 = GetRandomSpawnPosition();
            
            var teleporter1 = Instantiate(teleporterPrefab, pos1, Quaternion.identity);
            var teleporter2 = Instantiate(teleporterPrefab, pos2, Quaternion.identity);
            
            var env1 = teleporter1.GetComponent<EnvironmentalObject>();
            var env2 = teleporter2.GetComponent<EnvironmentalObject>();
            
            if (env1 != null && env2 != null)
            {
                env1.Initialize("teleporter", 0f);
                env2.Initialize("teleporter", 0f);
                env1.linkedObject = teleporter2;
                env2.linkedObject = teleporter1;
            }
            
            NetworkServer.Spawn(teleporter1);
            NetworkServer.Spawn(teleporter2);
        }
        
        // Spawn bounce pads
        for (int i = 0; i < 4; i++)
        {
            Vector3 randomPos = GetRandomSpawnPosition();
            var bouncePad = Instantiate(bouncePadPrefab, randomPos, Quaternion.identity);
            var envObject = bouncePad.GetComponent<EnvironmentalObject>();
            if (envObject != null)
            {
                envObject.Initialize("bounce_pad", 0f);
            }
            NetworkServer.Spawn(bouncePad);
        }
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        // Get a random position within the arena bounds
        float x = Random.Range(-10f, 10f);
        float y = Random.Range(-10f, 10f);
        return new Vector3(x, y, 0f);
    }
    
    // Check for environmental interactions when weapon hits environment
    [Server]
    public void ProcessEnvironmentalHit(Vector3 hitPosition, float damage, int weaponId, int mode)
    {
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(hitPosition, 0.5f, interactableLayer);
        
        foreach (var hitObject in hitObjects)
        {
            var envObject = hitObject.GetComponent<EnvironmentalObject>();
            if (envObject != null)
            {
                envObject.OnHit(damage, weaponId, mode, hitPosition);
            }
        }
    }
    
    // Special weapon interactions with environment
    [Server]
    public void ProcessSpecialWeaponEnvironmental(int weaponId, int mode, Vector3 position, Vector3 direction)
    {
        switch (weaponId)
        {
            case 4: // Elemental Staff
                ProcessElementalEnvironmental(mode, position, direction);
                break;
                
            case 5: // Morph Cannon
                ProcessCannonEnvironmental(mode, position, direction);
                break;
                
            case 7: // War Hammer
                ProcessHammerEnvironmental(mode, position, direction);
                break;
                
            case 10: // Chaos Orb
                ProcessChaosEnvironmental(mode, position, direction);
                break;
        }
    }
    
    [Server]
    void ProcessElementalEnvironmental(int mode, Vector3 position, Vector3 direction)
    {
        switch (mode)
        {
            case 0: // Fire mode - ignites flammable objects
                Collider2D[] fireTargets = Physics2D.OverlapCircleAll(position, 2f, interactableLayer);
                foreach (var target in fireTargets)
                {
                    var envObject = target.GetComponent<EnvironmentalObject>();
                    if (envObject != null && envObject.objectType == "explosive_barrel")
                    {
                        envObject.OnHit(999f, 4, 0, position); // Auto-ignite barrels
                    }
                }
                break;
                
            case 1: // Ice mode - freezes water, slows mechanisms
                Collider2D[] iceTargets = Physics2D.OverlapCircleAll(position, 1.5f, interactableLayer);
                foreach (var target in iceTargets)
                {
                    var envObject = target.GetComponent<EnvironmentalObject>();
                    if (envObject != null)
                    {
                        envObject.ApplyStatusEffect("frozen", 3f);
                    }
                }
                break;
                
            case 2: // Lightning mode - powers/disrupts electrical objects
                Collider2D[] lightningTargets = Physics2D.OverlapCircleAll(position, 3f, interactableLayer);
                foreach (var target in lightningTargets)
                {
                    var envObject = target.GetComponent<EnvironmentalObject>();
                    if (envObject != null && envObject.objectType == "teleporter")
                    {
                        envObject.ApplyStatusEffect("overcharged", 5f);
                    }
                }
                break;
        }
    }
    
    [Server]
    void ProcessCannonEnvironmental(int mode, Vector3 position, Vector3 direction)
    {
        switch (mode)
        {
            case 0: // Rocket mode - explosive damage to structures
                Collider2D[] rocketTargets = Physics2D.OverlapCircleAll(position, 2f, interactableLayer);
                foreach (var target in rocketTargets)
                {
                    var envObject = target.GetComponent<EnvironmentalObject>();
                    if (envObject != null)
                    {
                        envObject.OnHit(75f, 5, 0, position);
                    }
                }
                break;
                
            case 1: // Beam mode - cuts through materials
                RaycastHit2D[] beamHits = Physics2D.RaycastAll(position, direction, 8f, interactableLayer);
                foreach (var hit in beamHits)
                {
                    var envObject = hit.collider.GetComponent<EnvironmentalObject>();
                    if (envObject != null && envObject.objectType == "breakable_wall")
                    {
                        envObject.OnHit(150f, 5, 1, hit.point);
                    }
                }
                break;
                
            case 2: // Grenade mode - area destruction
                Collider2D[] grenadeTargets = Physics2D.OverlapCircleAll(position, 3f, interactableLayer);
                foreach (var target in grenadeTargets)
                {
                    var envObject = target.GetComponent<EnvironmentalObject>();
                    if (envObject != null)
                    {
                        envObject.OnHit(60f, 5, 2, position);
                    }
                }
                break;
        }
    }
    
    [Server]
    void ProcessHammerEnvironmental(int mode, Vector3 position, Vector3 direction)
    {
        switch (mode)
        {
            case 0: // Slam mode - ground pound affects area
                Collider2D[] slamTargets = Physics2D.OverlapCircleAll(position, 2.5f, interactableLayer);
                foreach (var target in slamTargets)
                {
                    var envObject = target.GetComponent<EnvironmentalObject>();
                    if (envObject != null)
                    {
                        envObject.OnHit(80f, 7, 0, position);
                        envObject.ApplyStatusEffect("shaken", 2f);
                    }
                }
                break;
                
            case 2: // Spin mode - whirlwind effect
                Collider2D[] spinTargets = Physics2D.OverlapCircleAll(position, 3f, interactableLayer);
                foreach (var target in spinTargets)
                {
                    var envObject = target.GetComponent<EnvironmentalObject>();
                    if (envObject != null && envObject.objectType == "bounce_pad")
                    {
                        envObject.ApplyStatusEffect("spinning", 4f);
                    }
                }
                break;
        }
    }
    
    [Server]
    void ProcessChaosEnvironmental(int mode, Vector3 position, Vector3 direction)
    {
        switch (mode)
        {
            case 0: // Random mode - unpredictable effects
                Collider2D[] randomTargets = Physics2D.OverlapCircleAll(position, 2f, interactableLayer);
                foreach (var target in randomTargets)
                {
                    var envObject = target.GetComponent<EnvironmentalObject>();
                    if (envObject != null)
                    {
                        string[] randomEffects = { "chaos_boost", "chaos_malfunction", "chaos_transform" };
                        string effect = randomEffects[Random.Range(0, randomEffects.Length)];
                        envObject.ApplyStatusEffect(effect, Random.Range(2f, 6f));
                    }
                }
                break;
                
            case 1: // Portal mode - creates temporary environmental portal
                CreateChaosPortal(position);
                break;
                
            case 2: // Gravity mode - affects all environmental objects
                Collider2D[] gravityTargets = Physics2D.OverlapCircleAll(position, 4f, interactableLayer);
                foreach (var target in gravityTargets)
                {
                    var envObject = target.GetComponent<EnvironmentalObject>();
                    if (envObject != null)
                    {
                        envObject.ApplyStatusEffect("gravity_pulled", 3f);
                    }
                }
                break;
        }
    }
    
    [Server]
    void CreateChaosPortal(Vector3 position)
    {
        // Create a temporary chaos portal that affects the environment
        var portal = new GameObject("ChaosPortal");
        portal.transform.position = position;
        
        var portalEffect = portal.AddComponent<ChaosPortalEffect>();
        portalEffect.duration = 8f;
        portalEffect.effectRadius = 3f;
        
        NetworkServer.Spawn(portal);
        
        StartCoroutine(DestroyChaosPortal(portal, 8f));
    }
    
    [Server]
    IEnumerator DestroyChaosPortal(GameObject portal, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (portal != null)
        {
            NetworkServer.Destroy(portal);
        }
    }
}

// Environmental object component
public class EnvironmentalObject : NetworkBehaviour
{
    [SyncVar]
    public string objectType;
    
    [SyncVar]
    public float health;
    
    [SyncVar]
    public float maxHealth;
    
    public GameObject linkedObject; // For teleporters, etc.
    
    private Dictionary<string, float> statusEffects = new Dictionary<string, float>();
    private Coroutine statusUpdateCoroutine;
    
    public void Initialize(string type, float hp)
    {
        objectType = type;
        health = hp;
        maxHealth = hp;
        
        if (statusUpdateCoroutine == null)
        {
            statusUpdateCoroutine = StartCoroutine(UpdateStatusEffects());
        }
    }
    
    [Server]
    public void OnHit(float damage, int weaponId, int mode, Vector3 hitPosition)
    {
        if (health <= 0) return;
        
        health -= damage;
        
        if (health <= 0)
        {
            TriggerDestruction(weaponId, mode, hitPosition);
        }
        else
        {
            TriggerHitEffect(weaponId, mode, hitPosition);
        }
    }
    
    [Server]
    public void ApplyStatusEffect(string effect, float duration)
    {
        statusEffects[effect] = duration;
        RpcApplyVisualEffect(effect, duration);
    }
    
    [Server]
    void TriggerDestruction(int weaponId, int mode, Vector3 position)
    {
        switch (objectType)
        {
            case "explosive_barrel":
                CreateExplosion(position, 3f, 75f);
                break;
                
            case "breakable_wall":
                CreateDebris(position);
                break;
                
            case "teleporter":
                if (linkedObject != null)
                {
                    var linkedEnv = linkedObject.GetComponent<EnvironmentalObject>();
                    if (linkedEnv != null)
                    {
                        linkedEnv.health = 0; // Destroy linked teleporter too
                        NetworkServer.Destroy(linkedObject);
                    }
                }
                break;
        }
        
        RpcPlayDestructionEffect(objectType, position);
        NetworkServer.Destroy(gameObject);
    }
    
    [Server]
    void TriggerHitEffect(int weaponId, int mode, Vector3 position)
    {
        switch (objectType)
        {
            case "bounce_pad":
                TriggerBouncePad(position);
                break;
                
            case "teleporter":
                if (linkedObject != null && !statusEffects.ContainsKey("frozen"))
                {
                    TriggerTeleporter();
                }
                break;
        }
    }
    
    [Server]
    void CreateExplosion(Vector3 center, float radius, float damage)
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(center, radius);
        foreach (var target in targets)
        {
            var enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage((int)damage);
                
                // Knockback
                Vector2 direction = (target.transform.position - center).normalized;
                enemy.ApplyKnockback(direction * 500f, 0.5f);
            }
            
            // Chain explode other barrels
            var otherBarrel = target.GetComponent<EnvironmentalObject>();
            if (otherBarrel != null && otherBarrel.objectType == "explosive_barrel" && otherBarrel != this)
            {
                otherBarrel.OnHit(999f, 0, 0, center);
            }
        }
        
        RpcCreateExplosionEffect(center, radius);
    }
    
    [Server]
    void CreateDebris(Vector3 position)
    {
        // Create debris particles that can damage players
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.up;
            
            StartCoroutine(SpawnDebrisProjectile(position, direction));
        }
    }
    
    [Server]
    IEnumerator SpawnDebrisProjectile(Vector3 start, Vector3 direction)
    {
        var debris = new GameObject("Debris");
        debris.transform.position = start;
        
        var rb = debris.AddComponent<Rigidbody2D>();
        var collider = debris.AddComponent<CircleCollider2D>();
        collider.radius = 0.1f;
        
        rb.velocity = direction * 8f;
        
        NetworkServer.Spawn(debris);
        
        yield return new WaitForSeconds(2f);
        
        if (debris != null)
        {
            NetworkServer.Destroy(debris);
        }
    }
    
    [Server]
    void TriggerBouncePad(Vector3 position)
    {
        float bounceRadius = statusEffects.ContainsKey("spinning") ? 4f : 2f;
        float bounceForce = statusEffects.ContainsKey("chaos_boost") ? 800f : 400f;
        
        Collider2D[] targets = Physics2D.OverlapCircleAll(position, bounceRadius);
        foreach (var target in targets)
        {
            var enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                Vector2 bounceDirection = Vector2.up; // Bounce upward
                enemy.ApplyKnockback(bounceDirection * bounceForce, 0.1f);
            }
        }
        
        RpcPlayBounceEffect(position, bounceRadius);
    }
    
    [Server]
    void TriggerTeleporter()
    {
        if (linkedObject == null) return;
        
        float teleportRadius = statusEffects.ContainsKey("overcharged") ? 3f : 1.5f;
        
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, teleportRadius);
        foreach (var target in targets)
        {
            var enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.transform.position = linkedObject.transform.position;
                RpcPlayTeleportEffect(transform.position, linkedObject.transform.position);
            }
        }
    }
    
    [Server]
    IEnumerator UpdateStatusEffects()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            
            var effectsToRemove = new List<string>();
            var effectKeys = new List<string>(statusEffects.Keys);
            
            foreach (var effect in effectKeys)
            {
                statusEffects[effect] -= 0.1f;
                if (statusEffects[effect] <= 0)
                {
                    effectsToRemove.Add(effect);
                }
            }
            
            foreach (var effect in effectsToRemove)
            {
                statusEffects.Remove(effect);
                RpcRemoveVisualEffect(effect);
            }
        }
    }
    
    [ClientRpc]
    void RpcApplyVisualEffect(string effect, float duration)
    {
        // Apply visual effects based on status
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer == null) return;
        
        switch (effect)
        {
            case "frozen":
                renderer.color = Color.cyan;
                break;
            case "overcharged":
                renderer.color = Color.yellow;
                break;
            case "shaken":
                StartCoroutine(ShakeEffect(duration));
                break;
            case "spinning":
                StartCoroutine(SpinEffect(duration));
                break;
            case "chaos_boost":
                renderer.color = Color.magenta;
                break;
        }
    }
    
    [ClientRpc]
    void RpcRemoveVisualEffect(string effect)
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Color.white; // Reset color
        }
    }
    
    [ClientRpc]
    void RpcPlayDestructionEffect(string type, Vector3 position)
    {
        // Play destruction particle effects
    }
    
    [ClientRpc]
    void RpcCreateExplosionEffect(Vector3 center, float radius)
    {
        // Create explosion visual and sound effects
    }
    
    [ClientRpc]
    void RpcPlayBounceEffect(Vector3 position, float radius)
    {
        // Play bounce pad activation effects
    }
    
    [ClientRpc]
    void RpcPlayTeleportEffect(Vector3 from, Vector3 to)
    {
        // Play teleportation effects
    }
    
    IEnumerator ShakeEffect(float duration)
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            transform.position = originalPos + (Vector3)Random.insideUnitCircle * 0.1f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPos;
    }
    
    IEnumerator SpinEffect(float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            transform.Rotate(0, 0, 360f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}

// Chaos portal effect component
public class ChaosPortalEffect : NetworkBehaviour
{
    public float duration = 8f;
    public float effectRadius = 3f;
    
    void Start()
    {
        StartCoroutine(ChaosEffectRoutine());
    }
    
    IEnumerator ChaosEffectRoutine()
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;
            
            // Apply random chaos effects to nearby objects
            Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, effectRadius);
            foreach (var target in targets)
            {
                var envObject = target.GetComponent<EnvironmentalObject>();
                if (envObject != null)
                {
                    ApplyRandomChaosEffect(envObject);
                }
                
                var enemy = target.GetComponent<Enemy>();
                if (enemy != null)
                {
                    ApplyRandomPlayerEffect(enemy);
                }
            }
        }
    }
    
    void ApplyRandomChaosEffect(EnvironmentalObject obj)
    {
        string[] effects = { "chaos_transform", "chaos_malfunction", "chaos_boost" };
        string randomEffect = effects[Random.Range(0, effects.Length)];
        obj.ApplyStatusEffect(randomEffect, Random.Range(1f, 3f));
    }
    
    void ApplyRandomPlayerEffect(Enemy player)
    {
        int randomEffect = Random.Range(0, 4);
        
        switch (randomEffect)
        {
            case 0: // Speed boost
                // Apply speed buff
                break;
            case 1: // Confusion (reverse controls temporarily)
                // Apply confusion effect
                break;
            case 2: // Weapon swap
                // Force random weapon mode
                break;
            case 3: // Teleport
                Vector3 randomPos = transform.position + (Vector3)Random.insideUnitCircle * 5f;
                player.transform.position = randomPos;
                break;
        }
    }
}