using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class ShopMenu : NetworkBehaviour
{
    public GameObject menu;
    public GameObject[] weaponPickupPrefabsCommon;
    public GameObject[] weaponPickupPrefabsUncommon;
    public TextMeshProUGUI t;
    public TextMeshProUGUI coinDisplay; // Add this to show coin count in UI
    public GameObject other;

    [SyncVar(hook = nameof(OnCoinsChanged))]
    public int coins = 0;

    // Start is called before the first frame update
    void Start()
    {
        UpdateCoinDisplay();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ActivateMenu()
    {
        if (menu.activeSelf)
        {
            menu.SetActive(false);
            t.enabled = true;
            GetComponent<MouseShooting>().isShooting = false;
            other.SetActive(true);
        }
        else
        {
            menu.SetActive(true);
            t.enabled = false;
            GetComponent<MouseShooting>().isShooting = true;
            other.SetActive(false);
        }
    }

    // Add coins method - can be called from other scripts
    public void AddCoins(int amount)
    {
        if (isLocalPlayer)
        {
            CmdAddCoins(amount);
        }
    }

    // Command to add coins on server
    [Command]
    void CmdAddCoins(int amount)
    {
        coins += amount;
    }

    // Hook called when coins SyncVar changes
    void OnCoinsChanged(int oldValue, int newValue)
    {
        coins = newValue;
        UpdateCoinDisplay();
    }

    // Update the coin display UI
    void UpdateCoinDisplay()
    {
        if (coinDisplay != null)
        {
            coinDisplay.text = "Coins: " + coins.ToString();
        }
    }

    // Called on client, runs on server
    [Command]
    void CmdBuyWeaponCommon(int weaponIndex, int cost)
    {
        if (coins >= cost)
        {
            coins -= cost;
            // Spawn weapon on server
            if (weaponPickupPrefabsCommon != null && weaponPickupPrefabsCommon.Length > weaponIndex)
            {
                var prefab = weaponPickupPrefabsCommon[weaponIndex];
                var go = Instantiate(prefab, transform.position, Quaternion.identity);
                NetworkServer.Spawn(go);
            }
        }
    }

    [Command]
    void CmdBuyWeaponUncommon(int weaponIndex, int cost)
    {
        if (coins >= cost)
        {
            coins -= cost;
            // Spawn weapon on server
            if (weaponPickupPrefabsUncommon != null && weaponPickupPrefabsUncommon.Length > weaponIndex)
            {
                var prefab = weaponPickupPrefabsUncommon[weaponIndex];
                var go = Instantiate(prefab, transform.position, Quaternion.identity);
                NetworkServer.Spawn(go);
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        foreach (GameObject g in weaponPickupPrefabsCommon)
        {
            NetworkClient.RegisterPrefab(g);
        }
        foreach (GameObject g in weaponPickupPrefabsUncommon)
        {
            NetworkClient.RegisterPrefab(g);
        }
        UpdateCoinDisplay();
    }

    // These run on client
    public void BuyCommon()
    {
        if (isLocalPlayer) // Only allow local player to trigger
        {
            CmdBuyWeaponCommon((new System.Random()).Next(0, weaponPickupPrefabsCommon.Length - 1), 5); // 0 is common, 5 coins
        }
    }

    public void BuyRare()
    {
        if (isLocalPlayer)
        {
            CmdBuyWeaponUncommon((new System.Random()).Next(0, weaponPickupPrefabsUncommon.Length - 1), 10); // 0 is common, 5 coins
        }
    }
}