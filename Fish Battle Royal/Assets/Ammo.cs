using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ammo : MonoBehaviour
{
    int Destroy = 2;
    bool Death = false;

    public void FixedUpdate()
    {
        if (Death)
        {
            Destroy--;
            if (Destroy <= 0)
                Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player") return;
        Agent A = collision.gameObject.GetComponentInParent<Agent>();
        if (A != null && A.Ammo < A.MaxAmmo)
        {
            A.TimeWithoutAmmo += 5;
            A.Fitness += 50;
            transform.position = new Vector3(1000, 1000);
            Death = true;
            A.Ammo++;
        }
    }
}
