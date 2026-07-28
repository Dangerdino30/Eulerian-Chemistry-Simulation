using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

using static Custom;

public class Main : MonoBehaviour
{

    public int gridHeight;
    public int gridWidth;

    public float TPS = 30;

    public float D = 0.01f;//Arbitrary Coefficient of diffusivity, area per unit time

    public static float cellSize = 0.005f; //meters

    public static float TEMP = 100f;//Temparary, kelvin
    public static float MOLARMASS = 2f;//Temp. Molar mass of H2 in g/mol

    public static FluidCell[,] mainGrid;

    //public static float[,] horrVel;
    //public static float[,] vertVel;

    public struct FluidCell
    {


        public float amount;//in moles

        public Vector2 flowVelocity;

        public readonly float Pressure => amount * IDEAL_GAS_CONSTANT * TEMP  / (cellSize * cellSize * cellSize);
        public readonly float Density => amount * MOLARMASS / (cellSize * cellSize * cellSize);
        public readonly float Concentration => amount / (cellSize * cellSize * cellSize);


        public static FluidCell operator * (FluidCell left, float right)
        {
            return new FluidCell()
            {
                amount = left.amount * right,
                flowVelocity = left.flowVelocity * right,
            };
        }

        public static FluidCell operator + (FluidCell left, FluidCell right)
        {
            return new FluidCell()
            {
                amount = left.amount + right.amount,
                flowVelocity = left.flowVelocity + right.flowVelocity,
            };
        }
    }
    

    void Awake()
    {
        mainGrid = new FluidCell[gridWidth,gridHeight];
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                mainGrid[i,j] = new FluidCell();
            }
        }

        Time.fixedDeltaTime = 1f / TPS;

        SettupShaders();
    }
    private void OnDisable()
    {
        //cellBuffer?.Release();
    }


    public static ComputeBuffer cellBuffer;

    void SettupShaders()
    {
        cellBuffer = new ComputeBuffer(gridWidth * gridHeight, sizeof(float));


        Shader.SetGlobalBuffer("_Cells", cellBuffer);

        Shader.SetGlobalInt("_GridHeight", gridHeight);
        Shader.SetGlobalInt("_GridWidth", gridWidth);
    }

    //Debug/interaction stuff
    private void Update()
    {

    }

    //I want to run everything on a fixed timestep
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            mainGrid[0, 0].amount += 1f;
            mainGrid[0, 1].amount += 1f;
            mainGrid[1, 0].amount += 1f;
            mainGrid[1, 1].amount += 1f;
        }

        //Update the fluid's velocity

        float dt = 1f / TPS;

        for (int i = 0; i < gridWidth; i++)//vertical, would apply gravity here if I cared
        {
            for (int j = 0; j < gridHeight - 1; j++)
            {
                float du = dt * (mainGrid[i, j].Pressure - mainGrid[i, j + 1].Pressure) * 2 / (mainGrid[i, j].Density + mainGrid[i, j + 1].Density);//Pressure gradient divided by average density

                mainGrid[i, j].flowVelocity.y += Mathf.Max(du, 0);
                mainGrid[i, j + 1].flowVelocity.y += Mathf.Min(du, 0);//Only effect the flow velocity of the cell that needs to move for equilibrium to be reached
                
            }
        }
        for (int i = 0; i < gridWidth - 1; i++)//horrizontal
        {
            for (int j = 0; j < gridHeight; j++)
            {
                float du = dt * (mainGrid[i, j].Pressure - mainGrid[i + 1, j].Pressure) * 2 / (mainGrid[i + 1, j].Density + mainGrid[i, j].Density);//Pressure gradient divided by average density

                mainGrid[i, j].flowVelocity.x += Mathf.Max(du, 0);
                mainGrid[i + 1, j].flowVelocity.x += Mathf.Min(du, 0);//Only effect the flow velocity of the cell that needs to move for equilibrium to be reached
                
            }
        }

        //Advect quanitities
        FluidCell[,] newCells = new FluidCell[gridWidth, gridHeight];
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                //Forward advection. Distribute among cells
                Vector2 newPos = new Vector2(i,j) + mainGrid[i, j].flowVelocity * dt;
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

        //Simple diffusion, pseudo fick's first law, going down a concentration gradient dimension by dimension

        float C = Mathf.Min(0.5f, D * dt / (cellSize * cellSize));//computed constant for the sake of optimization, unitless, capped at 0.5 (or problems arrise)
        for (int i = 0; i < gridWidth; i++)//vertical
        {
            for (int j = 0; j < gridHeight - 1; j++)
            {
                float grad = (mainGrid[i,j].amount - mainGrid[i, j + 1].amount);
                mainGrid[i, j].amount -= C * grad;
                mainGrid[i, j + 1].amount += C * grad;
            }
        }
        for (int i = 0; i < gridWidth - 1; i++)//horrizontal
        {
            for (int j = 0; j < gridHeight; j++)
            {
                float grad = (mainGrid[i,j].amount - mainGrid[i + 1, j].amount);
                mainGrid[i, j].amount -= C * grad;
                mainGrid[i + 1, j].amount += C * grad;
            }
        }

        //Temporary, replace later once the compute shaders get made
        float[] cellRenderQuantity = new float[gridWidth * gridHeight];
        int ic = 0;
        foreach (FluidCell cell in mainGrid)
        {
            cellRenderQuantity[ic++] = cell.amount;
        }

        cellBuffer.SetData(cellRenderQuantity);
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
