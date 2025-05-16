using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ShopMenu : NetworkBehaviour
{
    public GameObject menu;

    public GameObject[] weaponPickupPrefabs;

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
        }
        else
        {
            menu.SetActive(true);
        }
        
    }

    [Server]
    private void SpawnWithIndicator(Vector3 position, int num)
    {

        // 4) Spawn the actual weapon
        if (weaponPickupPrefabs != null && weaponPickupPrefabs.Length > 0)
        {
            var prefab = weaponPickupPrefabs[num];
            var go = Instantiate(prefab, position, Quaternion.identity);
            NetworkServer.Spawn(go);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        foreach (GameObject g in weaponPickupPrefabs)
        {
            NetworkClient.RegisterPrefab(g);
        }
    }

    public void BuyCommon()
    {
        if (coins >= 5)
        {
            coins -= 5;
        }
        SpawnWithIndicator(transform.position, 0);
    }

    public void BuyRare()
    {
        if (coins >= 10)
        {
            coins -= 10;
        }
        SpawnWithIndicator(transform.position, 0);
    }

}
