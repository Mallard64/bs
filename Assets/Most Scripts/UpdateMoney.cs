using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UpdateMoney : MonoBehaviour
{
    public TextMeshProUGUI t;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        t.text = "Coins: " + GetComponent<ShopMenu>().coins.ToString();
    }
}
