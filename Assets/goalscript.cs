using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class goalscript : MonoBehaviour
{
    public GameStatsManager pt;
    public bool playernum;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "ball")
        {
            if (playernum)
            {
                pt.AddKill();
            }
            else
            {
                pt.AddKill1();
            }
        }
    }
}
