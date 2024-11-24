using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class goalscript : NetworkBehaviour
{
    public GameStatsManager pt;
    public bool playernum;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Update()
    {
        if (pt == null)
        {
            pt = FindObjectOfType<GameStatsManager>();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "ball")
        {
            collision.gameObject.transform.position = new Vector3(0, 0, 0);
            collision.gameObject.GetComponent<Rigidbody2D>().velocity = Vector3.zero;
            Enemy[] e = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            for (int i = 0; i < e.Length; i++)
            {
                e[i].health = 0;
            }
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
