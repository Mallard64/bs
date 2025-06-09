using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_location : MonoBehaviour
{
    public Transform t;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        collision.gameObject.transform.position = t.position;
    }
}
