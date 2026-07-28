struct fluidCell
{
    float density;
};

uniform StructuredBuffer<fluidCell> _Cells;

uniform int _GridHeight;
uniform int _GridWidth;

fluidCell GetCell(int2 c)
{
    return _Cells[c.x + c.y * _GridWidth];
}