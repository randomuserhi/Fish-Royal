using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Main : MonoBehaviour
{
    LSTMManager.LSTMGroup LSTMGroup;
    ComputeShader LSTMShader;
    int Population = 50;

    public GameObject Agent;
    public static List<Agent> Agents = new List<Agent>();

    public float TimeScale = 1;
    public static float UpdateRate = 1f / 60f;

    float NetworkTimer = 0;
    float NetworkFrequency = 1f / 20f;

    string FileLocation = "LSTM_FishRoyal";

    public void Start()
    {
        Physics2D.queriesStartInColliders = false;

        Application.runInBackground = true;

        LSTMShader = LSTMManager.GenerateComputeShader();
        LSTMGroup = LSTMManager.CreateLSTMGroup(new int[] { 16, 25, 20, 10, 2 }, Population);
        LSTMManager.AssignLSTMGroupToShader(LSTMGroup, LSTMShader);

        LSTMGroup.Initialize();

        if (!System.IO.File.Exists(@"C:\Users\LenovoY720\Documents\" + FileLocation + ".LSTM"))
            for (int i = 0; i < LSTMGroup.WeightsBiases.Length; i++)
            {
                LSTMGroup.WeightsBiases[i] = UnityEngine.Random.Range(-1f, 1f);
            }
        else
        {
            Debug.Log("Loading Old File");
            LSTMGroup.LoadFullGroup(@"C:\Users\LenovoY720\Documents\" + FileLocation + ".LSTM");
        }

        LSTMGroup.SetWeightBiasData();

        for (int i = 0; i < Population; i++)
        {
            GameObject NewAgent = Instantiate(Agent);
            NewAgent.transform.position = new Vector2(UnityEngine.Random.Range(-50f, 50f), UnityEngine.Random.Range(-50f, 50f));
            Agent A = NewAgent.GetComponent<Agent>();
            A.Network = i;
            A.Group = LSTMGroup;
            Agents.Add(A);
        }
    }

    public float GenerationTimer = 10;
    public float MaxTimer = 10;

    public void FixedUpdate()
    {
        TimeScale = Mathf.Clamp(TimeScale, 0.01f, 1);
        Time.timeScale = TimeScale;
        Time.fixedDeltaTime = Time.timeScale * UpdateRate;

        NetworkTimer += Time.fixedDeltaTime;
        if (NetworkTimer >= NetworkFrequency)
        {
            NetworkTimer = 0;
            LSTMManager.FeedForward(LSTMGroup.Inputs, LSTMGroup, LSTMShader);
        }

        GenerationTimer -= Time.fixedDeltaTime;
        if (GenerationTimer < 0)
        {
            GenerationTimer = MaxTimer;

            //Agents = Agents.OrderBy(A => A.Fitness).ToList();
            Agents = Agents.OrderByDescending(A => A.Fitness).ToList();

            float Avg = Agents.Sum(A => A.Fitness) / Agents.Count;
            Debug.Log(Agents[0].Fitness + ", " + Agents[Agents.Count - 1].Fitness + " > " + Avg);

            for (int i = Agents.Count / 4; i < Agents.Count; i++)
            {
                int A = UnityEngine.Random.Range(0, Agents.Count / 4);
                Agents[i].Reset();
                Agents[i].transform.position = new Vector2(UnityEngine.Random.Range(-50f, 50f), UnityEngine.Random.Range(-50f, 50f));

                LSTMGroup.Copy(Agents[A].Network, Agents[i].Network);
                LSTMGroup.Mutate(Agents[i].Network);
            }

            for (int i = 0; i < Agents.Count / 4; i++)
            {
                Agents[i].Reset();
                Agents[i].transform.position = new Vector2(UnityEngine.Random.Range(-50f, 50f), UnityEngine.Random.Range(-50f, 50f));
            }
        }
    }

    public void OnApplicationQuit()
    {
        Debug.Log("Disposing");
        LSTMManager.DisposeGroup(LSTMGroup);
        Debug.Log("Saving");
        //LSTMGroup.SaveFullGroup(@"C:\Users\LenovoY720\Documents\" + FileLocation);
    }
}
