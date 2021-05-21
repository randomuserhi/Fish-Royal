using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    Rigidbody2D RB;

    // Start is called before the first frame update
    void Start()
    {
        RB = GetComponent<Rigidbody2D>();
    }

    float Speed = 25f;
    float TurnRate = 360f;
    public Agent Parent = null;
    public Agent FixedParent = null;
    float Timer = 3f;
    List<Agent> Agents = new List<Agent>();

    float VeerAngle = 60 * Mathf.Deg2Rad;
    float VeerTimer = 0;
    float LifeTime = 10;

    // Update is called once per frame
    void FixedUpdate()
    {
        LifeTime -= Time.fixedDeltaTime;
        if (LifeTime <= 0)
            Destroy(gameObject);

        Timer -= Time.fixedDeltaTime;
        if (Parent != null && Timer <= 0)
        {
            Parent = null;
        }

        Quaternion Rot = transform.rotation;

        if (Agents.Count > 0)
        {
            Agents = Agents.OrderBy(A => (transform.position - A.transform.position).sqrMagnitude).ToList();

            if (Agents[0].Dead)
                Agents.RemoveAt(0);
            else
            {
                if (Agents[0] != Parent)
                {
                    Vector2 Dir = transform.position - Agents[0].transform.position;
                    if (Dir.sqrMagnitude < 0.5f)
                    {
                        Agents[0].Dead = true;
                        if (Agents[0] != FixedParent)
                            FixedParent.Fitness += 10f;
                        Destroy(gameObject);
                        return;
                    }
                    float Angle = Vector2.SignedAngle(new Vector2(Mathf.Cos(VeerAngle) * RB.velocity.x - Mathf.Sin(VeerAngle) * RB.velocity.y, Mathf.Sin(VeerAngle) * RB.velocity.x + Mathf.Cos(VeerAngle) * RB.velocity.y), Dir);
                    Rot.eulerAngles += new Vector3(0, 0, TurnRate * Time.fixedDeltaTime * (Angle < 0 ? 1 : -1));
                }
            }
        }

        VeerTimer -= Time.fixedDeltaTime;
        if (VeerTimer <= 0)
        {
            VeerTimer = 0.7f;
            VeerAngle *= -1;
        }
        transform.rotation = Rot;
        RB.velocity = transform.rotation * Vector2.up * Speed;
    }

    private void OnTriggerEnter2D(Collider2D Other)
    {
        if (Agents.Count > 10) return;
        Agent A = Other.GetComponent<Agent>();
        if (A != null && !A.Dead)
        {
            Agents.Add(A);
        }
    }

    private void OnTriggerExit2D(Collider2D Other)
    {
        Agent A = Other.GetComponent<Agent>();
        if (A != null && !A.Dead)
        {
            Agents.Remove(A);
        }
    }
}
