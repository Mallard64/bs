using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class goalscript : NetworkBehaviour
{
    public GameStatsManager pt;
    public bool playernum;
    public int playern;
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
            if (NetworkClient.localPlayer.gameObject.GetComponent<Enemy>().connectionId != playern)
            {
                if (!playernum)
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
                if (!playernum)
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
        }
    }
}
