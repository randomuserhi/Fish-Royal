using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    public int Network = 0;
    public LSTMManager.LSTMGroup Group;

    public GameObject Projectile;
    public GameObject Tail;
    public Rigidbody2D RB;

    //Physics related stuff
    float PrevAngle = 0;
    float Speed = 0;
    float ResidualSpeed = 1f;

    public bool Dead = false;

    public void Reset()
    {
        Dead = false;
        PrevAngle = 0;
        Speed = 0;
        RB.velocity = Vector2.zero;
        RB.angularVelocity = 0;
        Fitness = 0;
        Ammo = 0;
        TimeWithoutAmmo = 500;
    }

    // Start is called before the first frame update
    void Start()
    {
        RB = gameObject.GetComponent<Rigidbody2D>();
    }

    float DAS = 0;
    float FireRate = 3;
    bool FirstTrigger = true;

    [NonSerialized]
    public float TimeWithoutAmmo = 500;

    bool Shooting = true;

    void Shoot()
    {
        if (Shooting && Ammo > 0)
        {
            GameObject O = Instantiate(Projectile);
            Projectile P = O.GetComponent<Projectile>();
            P.Parent = this;
            P.FixedParent = this;
            O.transform.position = transform.position;
            O.transform.rotation = transform.rotation;
            Ammo--;
        }
    }

    public void Copy(Agent A)
    {
        Speed = A.Speed;
        PrevAngle = A.PrevAngle;
        RB.velocity = A.RB.velocity;
        RB.angularVelocity = A.RB.angularVelocity;
    }

    public float Fitness;

    public int Ammo = 0;
    [NonSerialized]
    public int MaxAmmo = 5;

    public Vector3 SpawnPoint;

    RaycastHit2D Ray;

    List<Agent> Agents = new List<Agent>();
    List<Ammo> Ammos = new List<Ammo>();
    List<Projectile> Projs = new List<Projectile>();

    // Update is called once per frame
    public void FixedUpdate()
    {
        //Stiff update
        Timer += Time.fixedDeltaTime;
        if (Timer >= Main.UpdateRate)
        {
            StiffUpdate();
            Timer = 0;
        }

        if (Dead) return;

        Vector3 Pos = transform.position;
        if (Pos.x < -90) { Pos.x = -90; RB.velocity = new Vector2(0, RB.velocity.y); }
        else if (Pos.x > 90) { Pos.x = 90; RB.velocity = new Vector2(0, RB.velocity.y); }
        if (Pos.y < -90) { Pos.y = -90; RB.velocity = new Vector2(RB.velocity.x, 0); }
        else if (Pos.y > 90) { Pos.y = 90; RB.velocity = new Vector2(RB.velocity.x, 0); }
        transform.position = Pos;

        /*TimeWithoutAmmo -= Time.fixedDeltaTime;
        if (TimeWithoutAmmo <= 0)
            Dead = true;*/

        Fitness += Time.fixedDeltaTime + 3 * Speed * Time.fixedDeltaTime;

        //Network configuration
        int InputOffset = Group.GetInputOffset(Network);
        int OutputOffset = Group.GetOutputOffset(Network);

        //Input configuration
        Group.Inputs[InputOffset + 9] = Ammo == 0 ? -1 : Ammo == MaxAmmo ? 1 : 0;

        for (int i = 0; i < 3; i++)
        {
            if (i >= Agents.Count)
            {
                Group.Inputs[InputOffset + i] = 0;
                continue;
            }
            float A = Vector2.SignedAngle(Agents[i].transform.position - transform.position, transform.rotation * Vector2.up) / 180;
            Group.Inputs[InputOffset + i] = A;
        }

        for (int i = 0; i < 3; i++)
        {
            if (i >= Ammos.Count)
            {
                Group.Inputs[InputOffset + i] = 0;
                continue;
            }
            float A = Vector2.SignedAngle(Ammos[i].transform.position - transform.position, transform.rotation * Vector2.up) / 180;
            Group.Inputs[InputOffset + 3 + i] = A;
        }

        for (int i = 0; i < 3; i++)
        {
            if (i >= Projs.Count)
            {
                Group.Inputs[InputOffset + i] = 0;
                continue;
            }
            float A = Vector2.SignedAngle(Projs[i].transform.position - transform.position, transform.rotation * Vector2.up) / 180;
            Group.Inputs[InputOffset + 6 + i] = A;
        }

        //Shooting
        if (Group.Outputs[OutputOffset + 1] > 0)
        {
            if (FirstTrigger)
            {
                Shoot();
                DAS = 0;
                FirstTrigger = false;
            }
            if (DAS <= FireRate)
            {
                DAS += Time.fixedDeltaTime;
            }
            else
            {
                DAS = 0;
                Shoot();
            }
        }
        else
        {
            FirstTrigger = true;
        }

        //Accelerate Tail
        Quaternion TailRot = Tail.transform.localRotation;
        PrevAngle = TailRot.eulerAngles.z;
        if (PrevAngle > 70) PrevAngle -= 360;

        float Angle = Group.Outputs[OutputOffset] * 70;

        TailRot.eulerAngles = new Vector3(0, 0, Angle);

        Tail.transform.localRotation = TailRot;

        //Rotate body
        RB.angularVelocity -= Angle * Mathf.Min(RB.velocity.magnitude, 1) * 50 * Time.fixedDeltaTime;

        //Accelerate body
        float ChangeInAngle = Math.Abs(Angle - PrevAngle);
        if (ChangeInAngle > 10)
        {
            Speed += ChangeInAngle * 0.1f;
        }
        Speed += ResidualSpeed;

        RB.velocity += (Vector2)(transform.rotation * new Vector2(0, Speed)) * Time.fixedDeltaTime;

        //Steer velocity vector towards body
        Angle = Vector2.SignedAngle(RB.velocity, transform.rotation * Vector2.up) * 0.3f;
        Angle *= Mathf.Deg2Rad;
        RB.velocity = new Vector2(Mathf.Cos(Angle) * RB.velocity.x - Mathf.Sin(Angle) * RB.velocity.y, Mathf.Sin(Angle) * RB.velocity.x + Mathf.Cos(Angle) * RB.velocity.y);
    }

    float Timer;

    //Called strictly on every UpdateRate Tick
    private void StiffUpdate()
    {
        RB.velocity *= 0.95f;
        Speed *= 0.95f;
        RB.angularVelocity *= 0.95f;
    }

    public void RemoveAm(Ammo A)
    {
        Ammos.Remove(A);
    }

    private void OnTriggerEnter2D(Collider2D Other)
    {
        Agent A = Other.GetComponent<Agent>();
        if (A != null && !A.Dead)
        {
            Agents.Add(A);
            return;
        }
        Ammo AM = Other.GetComponent<Ammo>();
        if (AM != null)
        {
            Ammos.Add(AM);
        }
        Projectile P = Other.GetComponent<Projectile>();
        if (P != null)
        {
            Projs.Add(P);
        }
    }

    private void OnTriggerExit2D(Collider2D Other)
    {
        Agent A = Other.GetComponent<Agent>();
        if (A != null && !A.Dead)
        {
            Agents.Remove(A);
            return;
        }
        Ammo AM = Other.GetComponent<Ammo>();
        if (AM != null)
        {
            Ammos.Remove(AM);
        }
        Projectile P = Other.GetComponent<Projectile>();
        if (P != null)
        {
            Projs.Remove(P);
        }
    }
}
