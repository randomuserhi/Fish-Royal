using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class LSTMManager
{
    public static ComputeShader GenerateComputeShader()
    {
        return Resources.Load<ComputeShader>("LSTM");
    }

    public struct MatrixInfo
    {
        public int Rows;
        public int Cols;
        public int Size;
        public int Offset; //Offset in memory for the buffers
    }

    public struct LSTMGroup
    {
        public int Population;
        public int[] Structure;

        public int NumInputs;
        public int NumOutputs;

        public float[] Inputs;
        public float[] Nodes;
        public float[] Outputs;
        internal ComputeBuffer _Inputs;
        internal ComputeBuffer _Outputs;
        internal ComputeBuffer _Nodes;

        public float[] CellStates;
        internal ComputeBuffer _CellStates;

        public float[] WeightsBiases;
        internal ComputeBuffer _WeightsBiases;

        public MatrixInfo[] NodeCellInfo;
        public MatrixInfo[] WeightBiasInfo;
        public int MaxGateSize;
        internal ComputeBuffer _NodeCellInfo;
        internal ComputeBuffer _WeightBiasInfo;

        public float[] Gates;
        internal ComputeBuffer _Gates;

        public int GetOutputOffset(int Network)
        {
            return Network * NumOutputs;
        }

        public int GetInputOffset(int Network)
        {
            return Network * NumInputs;
        }

        public void LoadFullGroup(string FilePath)
        {
            byte[] Data = File.ReadAllBytes(FilePath);
            Buffer.BlockCopy(Data, 0, WeightsBiases, 0, Data.Length);
        }

        public void SaveFullGroup(string FilePath)
        {
            byte[] Data = new byte[WeightsBiases.Length * sizeof(float)];
            Buffer.BlockCopy(WeightsBiases, 0, Data, 0, WeightsBiases.Length * sizeof(float));
            File.WriteAllBytes(FilePath + ".LSTM", Data);
        }

        private void _HeavyMutate(ref int Offset, int WeightSize, int WeightStateSize, int BiasSize)
        {
            const float Chance = 0.5f;
            for (int z = 0; z < 4; z++)
            {
                for (int j = 0; j < WeightSize; j++, Offset++)
                {
                    float MutationChance = UnityEngine.Random.Range(0f, 1f);
                    if (MutationChance < Chance)
                    {
                        float MutationOption = UnityEngine.Random.Range(0f, 1f);
                        if (MutationOption < 0.25f) WeightsBiases[Offset] *= -1f;
                        else if (MutationOption < 0.5f) WeightsBiases[Offset] += UnityEngine.Random.Range(0f, 1f);
                        else if (MutationOption < 0.75f) WeightsBiases[Offset] -= UnityEngine.Random.Range(0f, 1f);
                        else WeightsBiases[Offset] = UnityEngine.Random.Range(-1f, 1f);
                    }

                    WeightsBiases[Offset] += UnityEngine.Random.Range(-0.3f, 0.3f);
                }
                for (int j = 0; j < WeightStateSize; j++, Offset++)
                {
                    float MutationChance = UnityEngine.Random.Range(0f, 1f);
                    if (MutationChance < Chance)
                    {
                        float MutationOption = UnityEngine.Random.Range(0f, 1f);
                        if (MutationOption < 0.25f) WeightsBiases[Offset] *= -1f;
                        else if (MutationOption < 0.5f) WeightsBiases[Offset] += UnityEngine.Random.Range(0f, 1f);
                        else if (MutationOption < 0.75f) WeightsBiases[Offset] -= UnityEngine.Random.Range(0f, 1f);
                        else WeightsBiases[Offset] = UnityEngine.Random.Range(-1f, 1f);
                    }

                    WeightsBiases[Offset] += UnityEngine.Random.Range(-0.3f, 0.3f);
                }
                for (int j = 0; j < BiasSize; j++, Offset++)
                {
                    float MutationChance = UnityEngine.Random.Range(0f, 1f);
                    if (MutationChance < Chance)
                    {
                        float MutationOption = UnityEngine.Random.Range(0f, 1f);
                        if (MutationOption < 0.25f) WeightsBiases[Offset] *= -1f;
                        else if (MutationOption < 0.5f) WeightsBiases[Offset] += UnityEngine.Random.Range(0f, 1f);
                        else if (MutationOption < 0.75f) WeightsBiases[Offset] -= UnityEngine.Random.Range(0f, 1f);
                        else WeightsBiases[Offset] = UnityEngine.Random.Range(-1f, 1f);
                    }

                    WeightsBiases[Offset] += UnityEngine.Random.Range(-0.3f, 0.3f);
                }
            }
        }
        public void HeavyMutate(int Network)
        {
            int PopulationOffset = Population - 1 - Network;
            _WeightsBiases.GetData(WeightsBiases);

            int WeightSize = WeightBiasInfo[0].Size;
            int WeightStateSize = WeightBiasInfo[1].Size;
            int BiasSize = NodeCellInfo[1].Size;
            int StrideSize = (WeightSize + WeightStateSize + BiasSize) * 4;
            int Offset = Network * StrideSize;

            _HeavyMutate(ref Offset, WeightSize, WeightStateSize, BiasSize);

            Offset += StrideSize * PopulationOffset;

            for (int i = 1, k = 2; i < Structure.Length - 1; i++, k++)
            {
                WeightSize = WeightBiasInfo[i * 2].Size;
                WeightStateSize = WeightBiasInfo[i * 2 + 1].Size;
                BiasSize = NodeCellInfo[k].Size;
                StrideSize = (WeightSize + WeightStateSize + BiasSize) * 4;
                Offset += Network * StrideSize;

                _HeavyMutate(ref Offset, WeightSize, WeightStateSize, BiasSize);

                Offset += StrideSize * PopulationOffset;
            }

            _WeightsBiases.SetData(WeightsBiases);
        }

        private void _Mutate(ref int Offset, int WeightSize, int WeightStateSize, int BiasSize)
        {
            const float Chance = 0.1f;
            for (int z = 0; z < 4; z++)
            {
                for (int j = 0; j < WeightSize; j++, Offset++)
                {
                    float MutationChance = UnityEngine.Random.Range(0f, 1f);
                    if (MutationChance < Chance)
                    {
                        float MutationOption = UnityEngine.Random.Range(0f, 1f);
                        if (MutationOption < 0.25f) WeightsBiases[Offset] *= -1f;
                        else if (MutationOption < 0.5f) WeightsBiases[Offset] += UnityEngine.Random.Range(0f, 1f);
                        else if (MutationOption < 0.75f) WeightsBiases[Offset] -= UnityEngine.Random.Range(0f, 1f);
                        else WeightsBiases[Offset] = UnityEngine.Random.Range(-1f, 1f);
                    }

                    WeightsBiases[Offset] += UnityEngine.Random.Range(-0.01f, 0.01f);
                }
                for (int j = 0; j < WeightStateSize; j++, Offset++)
                {
                    float MutationChance = UnityEngine.Random.Range(0f, 1f);
                    if (MutationChance < Chance)
                    {
                        float MutationOption = UnityEngine.Random.Range(0f, 1f);
                        if (MutationOption < 0.25f) WeightsBiases[Offset] *= -1f;
                        else if (MutationOption < 0.5f) WeightsBiases[Offset] += UnityEngine.Random.Range(0f, 1f);
                        else if (MutationOption < 0.75f) WeightsBiases[Offset] -= UnityEngine.Random.Range(0f, 1f);
                        else WeightsBiases[Offset] = UnityEngine.Random.Range(-1f, 1f);
                    }

                    WeightsBiases[Offset] += UnityEngine.Random.Range(-0.01f, 0.01f);
                }
                for (int j = 0; j < BiasSize; j++, Offset++)
                {
                    float MutationChance = UnityEngine.Random.Range(0f, 1f);
                    if (MutationChance < Chance)
                    {
                        float MutationOption = UnityEngine.Random.Range(0f, 1f);
                        if (MutationOption < 0.25f) WeightsBiases[Offset] *= -1f;
                        else if (MutationOption < 0.5f) WeightsBiases[Offset] += UnityEngine.Random.Range(0f, 1f);
                        else if (MutationOption < 0.75f) WeightsBiases[Offset] -= UnityEngine.Random.Range(0f, 1f);
                        else WeightsBiases[Offset] = UnityEngine.Random.Range(-1f, 1f);
                    }

                    WeightsBiases[Offset] += UnityEngine.Random.Range(-0.01f, 0.01f);
                }
            }
        }
        public void Mutate(int Network)
        {
            int PopulationOffset = Population - 1 - Network;
            _WeightsBiases.GetData(WeightsBiases);

            int WeightSize = WeightBiasInfo[0].Size;
            int WeightStateSize = WeightBiasInfo[1].Size;
            int BiasSize = NodeCellInfo[1].Size;
            int StrideSize = (WeightSize + WeightStateSize + BiasSize) * 4;
            int Offset = Network * StrideSize;

            _Mutate(ref Offset, WeightSize, WeightStateSize, BiasSize);

            Offset += StrideSize * PopulationOffset;

            for (int i = 1, k = 2; i < Structure.Length - 1; i++, k++)
            {
                WeightSize = WeightBiasInfo[i * 2].Size;
                WeightStateSize = WeightBiasInfo[i * 2 + 1].Size;
                BiasSize = NodeCellInfo[k].Size;
                StrideSize = (WeightSize + WeightStateSize + BiasSize) * 4;
                Offset += Network * StrideSize;

                _Mutate(ref Offset, WeightSize, WeightStateSize, BiasSize);

                Offset += StrideSize * PopulationOffset;
            }

            _WeightsBiases.SetData(WeightsBiases);
        }
        private void _Merge(ref int AOffset, ref int BOffset, ref int COffset, int WeightSize, int WeightStateSize, int BiasSize)
        {
            for (int z = 0; z < 4; z++)
            {
                for (int j = 0; j < WeightSize; j++, AOffset++, BOffset++, COffset++)
                {
                    float Chance = UnityEngine.Random.Range(0f, 1f);
                    if (Chance < 0.5f)
                        WeightsBiases[COffset] = WeightsBiases[AOffset];
                    else
                        WeightsBiases[COffset] = WeightsBiases[BOffset];
                }
                for (int j = 0; j < WeightStateSize; j++, AOffset++, BOffset++, COffset++)
                {
                    float Chance = UnityEngine.Random.Range(0f, 1f);
                    if (Chance < 0.5f)
                        WeightsBiases[COffset] = WeightsBiases[AOffset];
                    else
                        WeightsBiases[COffset] = WeightsBiases[BOffset];
                }
                for (int j = 0; j < BiasSize; j++, AOffset++, BOffset++, COffset++)
                {
                    float Chance = UnityEngine.Random.Range(0f, 1f);
                    if (Chance < 0.5f)
                        WeightsBiases[COffset] = WeightsBiases[AOffset];
                    else
                        WeightsBiases[COffset] = WeightsBiases[BOffset];
                }
            }
        }
        public void Merge(int NetworkA, int NetworkB, int NetworkC)
        {
            int PopulationOffsetA = Population - 1 - NetworkA;
            int PopulationOffsetB = Population - 1 - NetworkB;
            int PopulationOffsetC = Population - 1 - NetworkC;
            _WeightsBiases.GetData(WeightsBiases);

            int WeightSize = WeightBiasInfo[0].Size;
            int WeightStateSize = WeightBiasInfo[1].Size;
            int BiasSize = NodeCellInfo[1].Size;
            int StrideSize = (WeightSize + WeightStateSize + BiasSize) * 4;
            int AOffset = NetworkA * StrideSize;
            int BOffset = NetworkB * StrideSize;
            int COffset = NetworkC * StrideSize;

            _Merge(ref AOffset, ref BOffset, ref COffset, WeightSize, WeightStateSize, BiasSize);

            AOffset += StrideSize * PopulationOffsetA;
            BOffset += StrideSize * PopulationOffsetB;
            COffset += StrideSize * PopulationOffsetC;

            for (int i = 1, k = 2; i < Structure.Length - 1; i++, k++)
            {
                WeightSize = WeightBiasInfo[i * 2].Size;
                WeightStateSize = WeightBiasInfo[i * 2 + 1].Size;
                BiasSize = NodeCellInfo[k].Size;
                StrideSize = (WeightSize + WeightStateSize + BiasSize) * 4;
                AOffset += NetworkA * StrideSize;
                BOffset += NetworkB * StrideSize;
                COffset += NetworkC * StrideSize;

                _Merge(ref AOffset, ref BOffset, ref COffset, WeightSize, WeightStateSize, BiasSize);

                AOffset += StrideSize * PopulationOffsetA;
                BOffset += StrideSize * PopulationOffsetB;
                COffset += StrideSize * PopulationOffsetC;
            }

            _WeightsBiases.SetData(WeightsBiases);
        }

        private void _Copy(ref int AOffset, ref int BOffset, int WeightSize, int WeightStateSize, int BiasSize)
        {
            for (int z = 0; z < 4; z++)
            {
                for (int j = 0; j < WeightSize; j++, AOffset++, BOffset++)
                {
                    WeightsBiases[BOffset] = WeightsBiases[AOffset];
                }
                for (int j = 0; j < WeightStateSize; j++, AOffset++, BOffset++)
                {
                    WeightsBiases[BOffset] = WeightsBiases[AOffset];
                }
                for (int j = 0; j < BiasSize; j++, AOffset++, BOffset++)
                {
                    WeightsBiases[BOffset] = WeightsBiases[AOffset];
                }
            }
        }
        public void Copy(int NetworkA, int NetworkB)
        {
            int PopulationOffsetA = Population - 1 - NetworkA;
            int PopulationOffsetB = Population - 1 - NetworkB;
            _WeightsBiases.GetData(WeightsBiases);

            int WeightSize = WeightBiasInfo[0].Size;
            int WeightStateSize = WeightBiasInfo[1].Size;
            int BiasSize = NodeCellInfo[1].Size;
            int StrideSize = (WeightSize + WeightStateSize + BiasSize) * 4;
            int AOffset = NetworkA * StrideSize;
            int BOffset = NetworkB * StrideSize;

            _Copy(ref AOffset, ref BOffset, WeightSize, WeightStateSize, BiasSize);

            AOffset += StrideSize * PopulationOffsetA;
            BOffset += StrideSize * PopulationOffsetB;

            for (int i = 1, k = 2; i < Structure.Length - 1; i++, k++)
            {
                WeightSize = WeightBiasInfo[i * 2].Size;
                WeightStateSize = WeightBiasInfo[i * 2 + 1].Size;
                BiasSize = NodeCellInfo[k].Size;
                StrideSize = (WeightSize + WeightStateSize + BiasSize) * 4;
                AOffset += NetworkA * StrideSize;
                BOffset += NetworkB * StrideSize;

                _Copy(ref AOffset, ref BOffset, WeightSize, WeightStateSize, BiasSize);

                AOffset += StrideSize * PopulationOffsetA;
                BOffset += StrideSize * PopulationOffsetB;
            }

            _WeightsBiases.SetData(WeightsBiases);
        }

        public void Reset()
        {
            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i] = 0;
            }
            for (int i = 0; i < Nodes.Length; i++)
            {
                Nodes[i] = 0;
            }
            for (int i = 0; i < CellStates.Length; i++)
            {
                CellStates[i] = 0;
            }
            for (int i = 0; i < Outputs.Length; i++)
            {
                Outputs[i] = 0;
            }
            _Inputs.SetData(Inputs);
            _CellStates.SetData(CellStates);
            _Outputs.SetData(Outputs);
            _Nodes.SetData(Nodes);
        }
        public void Initialize()
        {
            _Inputs.SetData(Inputs);
            _Outputs.SetData(Outputs);
            _Nodes.SetData(Nodes);
            _CellStates.SetData(CellStates);
            _WeightsBiases.SetData(WeightsBiases);
            _NodeCellInfo.SetData(NodeCellInfo);
            _WeightBiasInfo.SetData(WeightBiasInfo);
            _Gates.SetData(Gates);
        }
        public void SetWeightBiasData()
        {
            _WeightsBiases.SetData(WeightsBiases);
        }
        public void DebugData()
        {
            _Inputs.GetData(Inputs);
            _Outputs.GetData(Outputs);
            _Nodes.GetData(Nodes);
            _CellStates.GetData(CellStates);
            _WeightsBiases.GetData(WeightsBiases);
            _NodeCellInfo.GetData(NodeCellInfo);
            _WeightBiasInfo.GetData(WeightBiasInfo);
            _Gates.GetData(Gates);

            Debug.Log("Inputs: " + String.Join(",", Inputs));
            Debug.Log("Outputs: " + String.Join(",", Outputs));
            Debug.Log("Nodes: " + String.Join(",", Nodes));
            Debug.Log("CellStates: " + String.Join(",", CellStates));
            Debug.Log("WeightsBiases: " + String.Join(",", WeightsBiases));
            Debug.Log("NodeCellInfo: " + String.Join(",", NodeCellInfo));
            Debug.Log("WeightBiasInfo: " + String.Join(",", WeightBiasInfo));
            Debug.Log("Gates: " + String.Join(",", Gates));
        }
    }

    public static LSTMGroup CreateLSTMGroup(int[] Structure, int Population)
    {
        int NodeCount = 0;
        int WeightCount = 0;
        int StateWeightCount = 0;
        MatrixInfo[] NodeCellInfo = new MatrixInfo[Structure.Length];
        MatrixInfo[] WeightBiasInfo = new MatrixInfo[2 * (Structure.Length - 1)];
        int MaxGateSize = Structure[1];

        NodeCellInfo[0] = new MatrixInfo()
        {
            Rows = 1,
            Cols = Structure[0],
            Size = Structure[0],
            Offset = 0
        };
        for (int i = 1, j = 0; i < Structure.Length; i++, j++)
        {
            WeightCount += Structure[j] * Structure[i] * 4;
            StateWeightCount += Structure[i] * Structure[i] * 4;
            NodeCount += Structure[i] * 4;

            NodeCellInfo[i] = new MatrixInfo()
            {
                Rows = 1,
                Cols = Structure[i],
                Size = Structure[i],
                Offset = 0
            };

            WeightBiasInfo[j * 2] = new MatrixInfo()
            {
                Rows = Structure[j],
                Cols = Structure[i],
                Size = Structure[j] * Structure[i],
                Offset = 0
            };
            WeightBiasInfo[j * 2 + 1] = new MatrixInfo()
            {
                Rows = Structure[i],
                Cols = Structure[i],
                Size = Structure[i] * Structure[i],
                Offset = 0
            };

            if (Structure[i] > MaxGateSize) MaxGateSize = Structure[i];
        }
        int NodeCountNoInputOutput = NodeCount - Structure[Structure.Length - 1];
        NodeCountNoInputOutput *= Population;
        NodeCount *= Population;
        WeightCount *= Population;
        StateWeightCount *= Population;
        int GateSize = MaxGateSize * Population;
        int TotalWeightBiasCount = WeightCount + StateWeightCount + NodeCount;
        int TotalGateSize = GateSize * 4;

        int InputSize = Structure[0] * Population;
        int OutputSize = Structure[Structure.Length - 1] * Population;

        return new LSTMGroup()
        {
            Population = Population,
            Structure = Structure,

            NumInputs = Structure[0],
            NumOutputs = Structure[Structure.Length - 1],

            Inputs = new float[InputSize],
            Nodes = new float[NodeCountNoInputOutput],
            Outputs = new float[OutputSize],
            _Inputs = new ComputeBuffer(InputSize, sizeof(float)),
            _Nodes = NodeCountNoInputOutput == 0 ? new ComputeBuffer(1, sizeof(float)) : new ComputeBuffer(NodeCountNoInputOutput, sizeof(float)),
            _Outputs = new ComputeBuffer(OutputSize, sizeof(float)),

            CellStates = new float[NodeCount],
            _CellStates = new ComputeBuffer(NodeCount, sizeof(float)),

            WeightsBiases = new float[TotalWeightBiasCount],
            _WeightsBiases = new ComputeBuffer(TotalWeightBiasCount, sizeof(float)),

            NodeCellInfo = NodeCellInfo,
            WeightBiasInfo = WeightBiasInfo,
            MaxGateSize = MaxGateSize,
            _NodeCellInfo = new ComputeBuffer(NodeCellInfo.Length, sizeof(int) * 4),
            _WeightBiasInfo = new ComputeBuffer(WeightBiasInfo.Length, sizeof(int) * 4),

            Gates = new float[TotalGateSize],
            _Gates = new ComputeBuffer(TotalGateSize, sizeof(float))
        };
    }

    public static void AssignLSTMGroupToShader(LSTMGroup Group, ComputeShader Compute)
    {
        Compute.SetInt("StructureLength", Group.Structure.Length);
        Compute.SetInt("Population", Group.Population);

        Compute.SetBuffer(0, "Inputs", Group._Inputs);
        Compute.SetBuffer(0, "Nodes", Group._Nodes);
        Compute.SetBuffer(0, "Outputs", Group._Outputs);

        Compute.SetBuffer(0, "CellStates", Group._CellStates);

        Compute.SetBuffer(0, "WeightsBiases", Group._WeightsBiases);

        Compute.SetBuffer(0, "NodeCellInfo", Group._NodeCellInfo);
        Compute.SetBuffer(0, "WeightBiasInfo", Group._WeightBiasInfo);
        Compute.SetInt("MaxGateSize", Group.MaxGateSize);
        Compute.SetInt("TotalGateStride", Group.MaxGateSize * 4);

        Compute.SetBuffer(0, "Gates", Group._Gates);
    }

    public static float[] FeedForward(float[] Inputs, LSTMGroup Group, ComputeShader Compute)
    {
        if (Inputs != Group.Inputs)
            for (int i = 0; i < Inputs.Length; i++)
            {
                Group.Inputs[i] = Inputs[i];
            }
        Group._Inputs.SetData(Group.Inputs);
        Compute.Dispatch(0, Group.Population, 1, 1);
        Group._Outputs.GetData(Group.Outputs);
        return Group.Outputs;
    }

    public static float[] FeedForward(float[] Inputs, int Count, LSTMGroup Group, ComputeShader Compute)
    {
        for (int i = 0; i < Inputs.Length; i++)
        {
            Group.Inputs[i] = Inputs[i];
        }
        Group._Inputs.SetData(Group.Inputs);
        Compute.Dispatch(0, Count, 1, 1);
        Group._Outputs.GetData(Group.Outputs);
        return Group.Outputs;
    }

    public static void DisposeGroup(LSTMGroup Group)
    {
        Group._Inputs?.Dispose();
        Group._Nodes?.Dispose();
        Group._Outputs?.Dispose();

        Group._CellStates?.Dispose();

        Group._WeightsBiases?.Dispose();

        Group._NodeCellInfo?.Dispose();
        Group._WeightBiasInfo?.Dispose();

        Group._Gates?.Dispose();
    }
}
