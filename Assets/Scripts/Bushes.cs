using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bushes : MonoBehaviour
{
    public bool inBush = false;

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Bushes")
        {
            gameObject.GetComponent<SpriteRenderer>().enabled = false;
            inBush = true;
        }
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Bushes")
        {
            gameObject.GetComponent<SpriteRenderer>().enabled = true;
            inBush = false;
        }
    }
}
