using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ammo : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Agent A = collision.GetComponent<Agent>();
        if (A != null && A.Ammo < A.MaxAmmo)
        {
            A.Fitness += 10;
            Main.Ammos.Remove(this);
            Destroy(gameObject);
            A.Ammo++;
        }
    }
}
