struct fluidCell
{
    float amount1;
    float amount2;
    float internalEnergy;
    float flowVelocityX;
    float flowVelocityY;
};

uniform StructuredBuffer<fluidCell> _Cells;

uniform int _GridHeight;
uniform int _GridWidth;

float IDEAL_GAS_CONSTANT = 8.31446261815324;
float AdjustedAdiabaticIndex = 0.28571428571;

uniform float _CellSize;
uniform float _CellSizeInv;
uniform float _CellSizeInv3;

fluidCell GetCell(int2 c)
{
    return _Cells[c.x + c.y * _GridWidth];
}