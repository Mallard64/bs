using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ShopMenu : NetworkBehaviour
{
    public GameObject menu;

    public GameObject[] weaponPickupPrefabsCommon;
    public GameObject[] weaponPickupPrefabsUncommon;

    public GameObject other;

    [SyncVar] public int coins = 0;

    // Start is called before the first frame update
    void Start()
    {

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
            GetComponent<MouseShooting>().enabled = true;
            other.SetActive(true);
        }
        else
        {
            menu.SetActive(true);
            GetComponent<MouseShooting>().enabled = false;
            other.SetActive(false);
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
    }

    // These run on client
    public void BuyCommon()
    {
        if (isLocalPlayer) // Only allow local player to trigger
        {

            CmdBuyWeaponCommon((new System.Random()).Next(0,weaponPickupPrefabsCommon.Length-1), 5); // 0 is common, 5 coins
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
