using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class chargen : MonoBehaviour
{
    public GameObject MagePrefab;
    public GameObject SniperPrefab;
    public GameObject spawnedChar;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Spawn(int n)
    {
        if (spawnedChar != null)
        {
            Destroy(spawnedChar);
        }
        if (n == 0)
        {
            spawnedChar = Instantiate(MagePrefab);
        }
        else
        {
            spawnedChar = Instantiate(SniperPrefab);
        }
    }
}
