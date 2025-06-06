using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class ShouldBeFlipped : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isClient)
        {
            if (NetworkClient.localPlayer.gameObject.GetComponent<MouseShooting>().isFlipped)
            {
                transform.localEulerAngles = new Vector3(0, 0, 180);
            }
            else
            {
                transform.localEulerAngles = new Vector3(0, 0, 0);
            }
        }
    }
}
