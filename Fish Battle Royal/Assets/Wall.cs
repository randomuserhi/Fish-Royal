using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Agent A = collision.GetComponent<Agent>();
        if (A != null && !A.Dead) A.Dead = true;
    }
}
