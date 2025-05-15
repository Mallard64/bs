using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Autoaim : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector3 FindTarget()
    {
        var hittables = FindObjectsOfType<Hittable>();  // returns Hittable[]
        Vector3 nearestDir = Vector3.positiveInfinity;
        float bestSqr = float.PositiveInfinity;
        Vector3 me = transform.position;

        foreach (var h in hittables)
        {
            Vector3 delta = h.transform.position - me;
            float d2 = delta.sqrMagnitude;
            if (d2 != 0 && d2 < bestSqr)
            {
                bestSqr = d2;
                nearestDir = delta;
            }
        }
        return nearestDir;
    }

}
