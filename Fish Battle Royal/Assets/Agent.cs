using System;
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

    //Input stuff
    int VisionResolution = 8;
    float VisionFallOff = 10;

    //Physics related stuff
    float PrevAngle = 0;
    float Speed = 0;
    float ResidualSpeed = 0f;

    public bool Dead = false;

    public void Reset()
    {
        Dead = false;
        PrevAngle = 0;
        Speed = 0;
        RB.velocity = Vector2.zero;
        RB.angularVelocity = 0;
        Fitness = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        RB = gameObject.GetComponent<Rigidbody2D>();
    }

    float DAS = 0;
    float FireRate = 3;
    bool FirstTrigger = true;

    void Shoot()
    {
        if (Ammo > 0)
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

    public float Fitness;

    public int Ammo = 0;
    public int MaxAmmo = 5;

    public Vector3 SpawnPoint;

    RaycastHit2D Ray;
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

        Fitness += Time.fixedDeltaTime + 3 * Speed * Time.fixedDeltaTime;

        //Network configuration
        int InputOffset = Group.GetInputOffset(Network);
        int OutputOffset = Group.GetOutputOffset(Network);

        //Input configuration

        //16 "eyes" for walls and projectiles
        //16 "eyes" for fish and ammo

        Quaternion Rot = transform.rotation;
        for (int i = 0; i < VisionResolution; i++)
        {
            Ray = Physics2D.Raycast(transform.position, Rot * Vector2.up);
            if (Ray.collider != null)
            {
                Agent A = Ray.collider.GetComponent<Agent>();
                Projectile P = Ray.collider.GetComponent<Projectile>();
                Wall W = Ray.collider.GetComponent<Wall>();
                Ammo AM = Ray.collider.GetComponent<Ammo>();

                if (A != null)
                {
                    if (A.Dead)
                    {
                        Group.Inputs[InputOffset + i] = 0;
                        Group.Inputs[InputOffset + VisionResolution + i] = -VisionFallOff / (Vector2.Distance(transform.position, A.transform.position) + VisionFallOff);
                    }
                    else
                    {
                        Group.Inputs[InputOffset + i] = VisionFallOff / (Vector2.Distance(transform.position, A.transform.position) + VisionFallOff);
                        Group.Inputs[InputOffset + VisionResolution + i] = 0;
                    }
                }
                else if (P != null && P.Parent != this)
                {
                    Group.Inputs[InputOffset + i] = 0;
                    Group.Inputs[InputOffset + VisionResolution + i] = VisionFallOff / (Vector2.Distance(transform.position, P.transform.position) + VisionFallOff);
                }
                else if (W != null)
                {
                    Group.Inputs[InputOffset + i] = 0;
                    Group.Inputs[InputOffset + VisionResolution + i] = -VisionFallOff / (Vector2.Distance(transform.position, W.transform.position) + VisionFallOff);
                }
                else if (AM != null)
                {
                    Group.Inputs[InputOffset + i] = -VisionFallOff / (Vector2.Distance(transform.position, AM.transform.position) + VisionFallOff);
                    Group.Inputs[InputOffset + VisionResolution + i] = 0;
                }
                else
                {
                    Group.Inputs[InputOffset + i] = 0;
                    Group.Inputs[InputOffset + VisionResolution + i] = 0;
                }
            }
            else
            {
                Group.Inputs[InputOffset + i] = 0;
                Group.Inputs[InputOffset + VisionResolution + i] = 0;
            }
            Rot.eulerAngles += new Vector3(0, 0, 360f / VisionResolution);
        }

        Group.Inputs[InputOffset + VisionResolution * 2] = Group.Outputs[OutputOffset];
        Group.Inputs[InputOffset + VisionResolution * 2 + 1] = Ammo == 0 ? -1 : Ammo == MaxAmmo ? 1 : 0;

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
}
