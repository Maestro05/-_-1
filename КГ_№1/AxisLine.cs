using System.Windows.Media;
namespace КГ__1
{
    // Структура для осей координат
    public struct AxisLine
    {
        public Vector4D Start, End;
        public Brush Color;
        public string Label;
        public AxisLine(Vector4D start, Vector4D end, Brush color, string label)
        {
            Start = start; End = end; Color = color; Label = label;
        }
    }
}
