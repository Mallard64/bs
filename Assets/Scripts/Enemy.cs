using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

public class Enemy : NetworkBehaviour
{
    [SyncVar] public float health;
    [SyncVar(hook = nameof(OnVisibilityChanged))] public bool isVisible = true;

    public float maxHealth = 100;
    public SpriteRenderer spriteRenderer;
    public float respawnTime = 3f;

    private CustomNetworkManager1 networkManager;
    public int connectionId; // Store the player's connection ID for consistency
    public Vector3 assignedSpawnPoint; // Store the player's assigned spawn point
    public int numPlayers;
    GameStatsManager pt;

    public bool hx = false;

    public float regainHealth = 0f;

    public void Win()
    {
        SceneManager.LoadScene("Win");
    }

    public void Lose()
    {
        SceneManager.LoadScene("Lose");
    }

    public void Tie()
    {
        SceneManager.LoadScene("Tie");
    }

    void Start()
    {
        pt = FindObjectOfType<GameStatsManager>();
        health = maxHealth;
        networkManager = (CustomNetworkManager1)NetworkManager.singleton;

        System.Random r = new System.Random();
        connectionId = r.Next(10000000);

        assignedSpawnPoint = networkManager.AssignSpawnPoint(connectionId);
        Debug.Log("ASSSIGNED SPAWN POINT: " + assignedSpawnPoint);
        if (assignedSpawnPoint != null)
        {
            transform.position = assignedSpawnPoint;
        }
        else
        {
            Debug.LogError("Failed to assign a spawn point.");
        }
        if (isLocalPlayer)
        {
            if (FindObjectsOfType<Enemy>().Length % 2 == 0)
            {
                pt.killsText.color = Color.red;
                hx = true;
            }
            else
            {
                pt.killsText1.color = Color.red;
                hx = false;
            }
        }
        
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            if (pt.gameTime >= 120)
            {
                if (hx)
                {
                    if (pt.kills > pt.kills1)
                    {
                        Win();
                    }
                    else if (pt.kills < pt.kills1)
                    {
                        Lose();
                    }
                    else
                    {
                        Tie();
                    }
                }
                else
                {
                    if (pt.kills < pt.kills1)
                    {
                        Win();
                    }
                    else if (pt.kills > pt.kills1)
                    {
                        Lose();
                    }
                    else
                    {
                        Tie();
                    }
                }
            }
        }
        UpdateHealthColor();
        if (health <= 0)
        {
            transform.position = assignedSpawnPoint;
            StartCoroutine(RespawnPlayer());
        }
        else
        {
            regainHealth += Time.deltaTime;
        }
        if (health < 100 && regainHealth >= 5f)
        {
            health += Time.deltaTime * 5f;
        }
    }

    void OnVisibilityChanged(bool oldVisibility, bool newVisibility)
    {
        spriteRenderer.enabled = newVisibility;
    }

    [Server]
    public void TakeDamage(int damage)
    {
        regainHealth = 0f;
        if (health <= 0 && SceneManager.GetActiveScene().name == "Knockout")
        {
            if (!hx)
            {
                pt.AddKill();
            }
            else
            {
                pt.AddKill1();
            }
            return;
        }
        health -= damage;

        SetVisibility(true);
        StartCoroutine(ShowFace());

        if (health <= 0)
        {
            transform.position = assignedSpawnPoint;
            StartCoroutine(RespawnPlayer());
        }
    }

    IEnumerator RespawnPlayer()
    {
        
        RpcDisablePlayer();
        yield return new WaitForSeconds(respawnTime);
        health = maxHealth;
        
        Debug.Log("RESPAWN LOCATION: " + transform.position);
        RpcEnablePlayer();
    }

    IEnumerator ShowFace()
    {
        yield return new WaitForSeconds(0.5f);
        if (gameObject.GetComponent<Bushes>().inBush)
        {
            SetVisibility(false);
        }
    }

    [Server]
    void SetVisibility(bool state)
    {
        isVisible = state;
    }

    [ClientRpc]
    void RpcDisablePlayer()
    {
        spriteRenderer.enabled = false;
        GetComponent<Collider2D>().enabled = false;
        GetComponent<PlayerMovement>().enabled = false;
    }

    [ClientRpc]
    void RpcEnablePlayer()
    {
        spriteRenderer.enabled = true;
        GetComponent<Collider2D>().enabled = true;
        GetComponent<PlayerMovement>().enabled = true;
        UpdateHealthColor();
    }

    void UpdateHealthColor()
    {
        float healthPercentage = (float)health / maxHealth;
        spriteRenderer.color = Color.Lerp(Color.red, Color.green, healthPercentage);
    }
}
