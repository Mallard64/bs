using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Settings : MonoBehaviour
{
    public GameObject menu;
    public GameObject other;

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
            other.SetActive(true);
        }
        else
        {
            menu.SetActive(true);
            other.SetActive(false);
        }
    }

}
