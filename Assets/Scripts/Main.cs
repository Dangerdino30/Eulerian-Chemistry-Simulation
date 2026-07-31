using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Custom;

public class Main : MonoBehaviour
{

    public int gridHeight;
    public int gridWidth;

    public float TicksPerSimulationSecond = 30;
    public float SecondsPerSimulationSecond = 10;

    public float D = 0.01f;//Arbitrary Coefficient of diffusivity, area per unit time

    public static float cellSize = 1f; //meters
    public static float cellSizeInv = 1/cellSize; //meters
    public static float cellSizeInv3 = 1/(cellSize * cellSize * cellSize); //1/m^3, cached

    public static float MOLARMASS1 = 0.002f;//Temp. Molar mass of H2 in kg/mol
    public static float MOLARMASS2 = 0.032f;//Temp. Molar mass of O2 in kg/mol


    public const float AdjustedAdiabaticIndex = 2f / 7f;//adiatatic index - 1. For diatomic gasses

    public static FluidCell[,] mainGrid;

    //public static float[,] horrVel;
    //public static float[,] vertVel;

    public struct FluidCell
    {


        public float amount1;//in moles
        public float amount2;

        public float internalEnergy;//Joules

        //public float Tempurature => 

        public float flowMomentumX;//kg m/s
        public float flowMomentumY;

        public readonly float Pressure => cellSizeInv3 * AdjustedAdiabaticIndex * internalEnergy; //Pascals (better be)
        //public readonly float Pressure => (amount1 + amount2) * IDEAL_GAS_CONSTANT * TEMP  * cellSizeInv3;
        public readonly float Density => Mass * cellSizeInv3;
        public readonly float Mass => (amount1 * MOLARMASS1 + amount2 * MOLARMASS2);
        public readonly float Temperature => AdjustedAdiabaticIndex * internalEnergy / ((amount1 + amount2) * IDEAL_GAS_CONSTANT);//Ideal gas law, Kelvin
        //public readonly float Concentration => amount1 * cellSizeInv3;


        public static FluidCell operator * (FluidCell left, float right)
        {
            return new FluidCell()
            {
                amount1 = left.amount1 * right,
                amount2 = left.amount2 * right,
                internalEnergy = left.internalEnergy * right,
                flowMomentumX = left.flowMomentumX * right,
                flowMomentumY = left.flowMomentumY * right,
            };
        }

        public static FluidCell operator + (FluidCell left, FluidCell right)
        {
            return new FluidCell()
            {
                amount1 = left.amount1 + right.amount1,
                amount2 = left.amount2 + right.amount2,
                internalEnergy = left.internalEnergy + right.internalEnergy,
                flowMomentumX = left.flowMomentumX + right.flowMomentumX,
                flowMomentumY = left.flowMomentumY + right.flowMomentumY,
            };
        }
    }

    public static Main instance;
    public Canvas canvas;

    void Awake()
    {
        instance = this;
        mainGrid = new FluidCell[gridWidth,gridHeight];
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                mainGrid[i, j] = new FluidCell()
                {
                    //amount1 = 0.0000001f,
                    //internalEnergy = 10f,
                };
            }
        }

        Time.fixedDeltaTime = SecondsPerSimulationSecond / TicksPerSimulationSecond;

        UI = new UI();

        SettupShaders();
    }
    private void OnDisable()
    {
        //cellBuffer?.Release();
    }


    public static ComputeBuffer cellBuffer;

    void SettupShaders()
    {
        cellBuffer = new ComputeBuffer(gridWidth * gridHeight, sizeof(float) * 5);


        Shader.SetGlobalBuffer("_Cells", cellBuffer);

        Shader.SetGlobalInt("_GridHeight", gridHeight);
        Shader.SetGlobalInt("_GridWidth", gridWidth);

        Shader.SetGlobalFloat("_CellSize", cellSize);
        Shader.SetGlobalFloat("_CellSizeInv", cellSizeInv);
        Shader.SetGlobalFloat("_CellSizeInv3", cellSizeInv3);
    }


    public static UI UI;
    //Debug/interaction stuff
    private void Update()
    {



        UI.Update();
    }

    //I want to run everything on a fixed timestep
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.T))
        {
            return;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            mainGrid[0, 0].amount1 += 0.1f;
            mainGrid[0, 1].amount1 += 0.1f;
            mainGrid[1, 0].amount1 += 0.1f;
            mainGrid[1, 1].amount1 += 0.1f;
            mainGrid[0, 0].internalEnergy += 3000f;
            mainGrid[0, 1].internalEnergy += 3000f; 
            mainGrid[1, 0].internalEnergy += 3000f;
            mainGrid[1, 1].internalEnergy += 3000f;
        }
        if (Input.GetKey(KeyCode.W))
        {
            mainGrid[gridWidth - 1, 0].amount2 += 0.1f;
            mainGrid[gridWidth - 1, 1].amount2 += 0.1f;
            mainGrid[gridWidth - 2, 0].amount2 += 0.1f;
            mainGrid[gridWidth - 2, 1].amount2 += 0.1f;
            mainGrid[gridWidth - 1, 0].internalEnergy += 3000f;
            mainGrid[gridWidth - 1, 1].internalEnergy += 3000f;
            mainGrid[gridWidth - 2, 0].internalEnergy += 3000f;
            mainGrid[gridWidth - 2, 1].internalEnergy += 3000f;
        }

        

        float dt = 1f / TicksPerSimulationSecond;

        //Apply forces

        for (int i = 0; i < gridWidth; i++)//vertical, would apply gravity here if I cared
        {
            for (int j = 0; j < gridHeight - 1; j++)
            {
                float du = dt * (mainGrid[i, j].Pressure - mainGrid[i, j + 1].Pressure) * cellSizeInv; //* 2 / (mainGrid[i, j].Density + mainGrid[i, j + 1].Density);//Pressure gradient divided by average density

                mainGrid[i, j].flowMomentumY += Mathf.Max(du, 0);
                mainGrid[i, j + 1].flowMomentumY += Mathf.Min(du, 0);//Only effect the flow velocity of the cell that needs to move for equilibrium to be reached

            }
            if (mainGrid[i, 0].flowMomentumY < 0)
            {
                mainGrid[i, 0].flowMomentumY *= -1;
            }
            if (mainGrid[i, gridHeight - 1].flowMomentumY > 0)
            {
                mainGrid[i, gridHeight - 1].flowMomentumY *= -1;
            }
        }
        for (int j = 0; j < gridHeight; j++)//horrizontal
        {
            for (int i = 0; i < gridWidth - 1; i++)
            {
                float du = dt * (mainGrid[i, j].Pressure - mainGrid[i + 1, j].Pressure) * cellSizeInv;// * 2 / (mainGrid[i + 1, j].Density + mainGrid[i, j].Density);//Pressure gradient divided by average density
                
                mainGrid[i, j].flowMomentumX += Mathf.Max(du, 0);
                mainGrid[i + 1, j].flowMomentumX += Mathf.Min(du, 0);

            }
            if (mainGrid[0, j].flowMomentumX < 0)
            {
                mainGrid[0, j].flowMomentumX *= -1;
            }
            if (mainGrid[gridWidth - 1, j].flowMomentumX > 0)
            {
                mainGrid[gridWidth - 1, j].flowMomentumX *= -1;
            }
        }

        //Advect quanitities
        FluidCell[,] newCells = new FluidCell[gridWidth, gridHeight];
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                if (mainGrid[i,j].Density == 0)
                {
                    continue;
                }

                //Forward advection. Distribute among cells
                Vector2 newPos = new Vector2(i + mainGrid[i, j].flowMomentumX / mainGrid[i,j].Density * dt, j + mainGrid[i, j].flowMomentumY / mainGrid[i, j].Density * dt);
                newPos.x = Mathf.Clamp(newPos.x, 0, gridWidth - 1); 
                newPos.y = Mathf.Clamp(newPos.y, 0, gridHeight - 1);
                int x = Mathf.Min(Mathf.FloorToInt(newPos.x), gridWidth - 2);
                int y = Mathf.Min(Mathf.FloorToInt(newPos.y), gridHeight - 2);

                //Weights
                float x1 = newPos.x - x; 
                float y1 = newPos.y - y;
                float x2 = 1 - x1;
                float y2 = 1 - y1;


                newCells[x, y] += mainGrid[i, j] * (x2 * y2);
                newCells[x + 1, y] += mainGrid[i, j] * (x1 * y2);
                newCells[x, y + 1] += mainGrid[i, j] * (x2 * y1);
                newCells[x + 1, y + 1] += mainGrid[i, j] * (x1 * y1);

            }
        }
        mainGrid = newCells;

        //Simple diffusion, pseudo fick's first law, going down a concentration gradient dimension by dimension. Not effected by density or anything lmao.

        /* */
        float C = Mathf.Min(0.5f, D * dt / (cellSize * cellSize));//computed constant for the sake of optimization, m^-1, capped at 0.5 (or problems arrise)
        for (int i = 0; i < gridWidth; i++)//vertical
        {
            for (int j = 0; j < gridHeight - 1; j++)
            {
                float grad1 = (mainGrid[i, j].amount1 - mainGrid[i, j + 1].amount1);//mole-meters
                mainGrid[i, j].amount1 -= C * grad1;
                mainGrid[i, j + 1].amount1 += C * grad1;
                float grad2 = (mainGrid[i, j].amount2 - mainGrid[i, j + 1].amount2);
                mainGrid[i, j].amount2 -= C * grad2;
                mainGrid[i, j + 1].amount2 += C * grad2;
                if (mainGrid[i, j].Mass + mainGrid[i, j + 1].Mass == 0)
                {
                    continue;
                }
                float energyPerKg = (mainGrid[i, j].internalEnergy + mainGrid[i, j + 1].internalEnergy) / (mainGrid[i, j].Mass + mainGrid[i, j + 1].Mass);
                float energyFrac = (grad1 * MOLARMASS1 + grad2 * MOLARMASS2) * energyPerKg;
                mainGrid[i, j].internalEnergy -= C * energyFrac;
                mainGrid[i, j + 1].internalEnergy += C * energyFrac;
            }
        }
        for (int i = 0; i < gridWidth - 1; i++)//horrizontal
        {
            for (int j = 0; j < gridHeight; j++)
            {
                float grad1 = (mainGrid[i,j].amount1 - mainGrid[i + 1, j].amount1);
                mainGrid[i, j].amount1 -= C * grad1;
                mainGrid[i + 1, j].amount1 += C * grad1;
                float grad2 = (mainGrid[i,j].amount2 - mainGrid[i + 1, j].amount2);
                mainGrid[i, j].amount2 -= C * grad2;
                mainGrid[i + 1, j].amount2 += C * grad2;
                if (mainGrid[i, j].Mass + mainGrid[i + 1, j].Mass == 0)
                {
                    continue;
                }
                float energyPerKg = (mainGrid[i, j].internalEnergy + mainGrid[i + 1, j].internalEnergy) / (mainGrid[i, j].Mass + mainGrid[i + 1, j].Mass);
                float energyFrac = (grad1 * MOLARMASS1 + grad2 * MOLARMASS2) * energyPerKg;//Transfer energy depending on mass transfered
                mainGrid[i, j].internalEnergy -= C * energyFrac;
                mainGrid[i + 1, j].internalEnergy += C * energyFrac;


            }
        }
        /**/
        //Temporary, replace later once the compute shaders get made
        
        FluidCell[] cells = new FluidCell[gridWidth * gridHeight];
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                cells[i + j * gridWidth] = mainGrid[i, j];
            }
        }

        cellBuffer.SetData(cells);
    }
    /*
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        for (int i = 0; i <= gridWidth; i++)
        {
            Gizmos.DrawLine(new Vector2(0, i * cellSize), new Vector2(gridHeight * cellSize, i * cellSize));
        }
        for (int i = 0; i <= gridHeight; i++)
        {
            Gizmos.DrawLine(new Vector2(i * cellSize, 0), new Vector2(i * cellSize, gridWidth * cellSize));
        }
        if (MAINGRID == null)
        {
            return;
        }
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                Gizmos.color = new Color(1, 0, 0, MAINGRID[i,j].density * 0.5f);
                Gizmos.DrawCube(new Vector2(i + 0.5f, j + 0.5f) * cellSize, Vector3.one * cellSize * 0.95f);
            }
        }
    }*/
}
