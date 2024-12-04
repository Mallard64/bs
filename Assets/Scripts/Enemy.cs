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
    public float oldHP;

    private CustomNetworkManager1 networkManager;
    public int connectionId; // Store the player's connection ID for consistency
    public Vector3 assignedSpawnPoint; // Store the player's assigned spawn point
    public int numPlayers;
    public GameStatsManager pt;

    public bool hx;


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
        oldHP = health;
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
        hx = PlayerPrefs.GetInt("team") % 2 == 0;
        if (isLocalPlayer)
        {
            
            if (hx)
            {
                pt.killsText.color = Color.red;
            }
            else
            {
                pt.killsText1.color = Color.red;
            }
        }
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Bongo bongo hit");
        if (collision.gameObject.GetComponent<Bullet>() != null && collision.gameObject.GetComponent<Bullet>().shooterId != connectionId)
        {
            Debug.Log("bengo bengo hit");
            if (NetworkClient.localPlayer.gameObject.GetComponent<Enemy>().connectionId == collision.gameObject.GetComponent<Bullet>().shooterId)
            {
                Debug.Log("bingo bingo hit");
                NetworkClient.localPlayer.gameObject.GetComponent<MouseShooting>().superCharge++;
            }
        }
    }

    void Update()
    {
        if (oldHP < health) //&& SceneManager.GetActiveScene().name == "Knockout")
        {
            if (NetworkClient.localPlayer.gameObject.GetComponent<Enemy>().connectionId != connectionId)
            {
                if (!hx)
                {
                    pt.AddKill1();
                    Debug.Log("Add kill1");
                }
                else
                {
                    pt.AddKill();
                    Debug.Log("Add kill");
                }
            }
            else
            {
                if (!hx)
                {
                    pt.AddKill();
                    Debug.Log("Add kill1");
                }
                else
                {
                    pt.AddKill1();
                    Debug.Log("Add kill");
                }
            }
            oldHP = health;
        }
        if (oldHP > health)
        {
            if (NetworkClient.localPlayer.gameObject.GetComponent<Enemy>().connectionId != connectionId)
            {
                NetworkClient.localPlayer.gameObject.GetComponent<MouseShooting>().superCharge++;
            }
            oldHP = health;
        }
        
        if (isLocalPlayer)
        {
            
            
            //if (FindObjectsOfType<Enemy>().Length % 2 == 0)
            //{
            //    pt.killsText1.color = Color.white;
            //    pt.killsText.color = Color.red;
            //    hx = true;
            //}
            //else
            //{
            //    pt.killsText.color = Color.white;
            //    pt.killsText1.color = Color.red;
            //    hx = false;
            //}
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
        oldHP = health;
    }

    void OnVisibilityChanged(bool oldVisibility, bool newVisibility)
    {
        spriteRenderer.enabled = newVisibility;
    }


    [Server]
    public void TakeDamage(int damage)
    {
        regainHealth = 0f;
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
        health = maxHealth;
        RpcDisablePlayer();
        yield return new WaitForSeconds(respawnTime);
        
        
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
