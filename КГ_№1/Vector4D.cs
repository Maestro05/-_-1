namespace КГ__1
{
    // Структура для хранения 4D-вектора
    public struct Vector4D
    {
        public double X, Y, Z, W;
        public Vector4D(double x, double y, double z, double w = 1.0)
        {
            X = x; Y = y; Z = z; W = w;
        }
    }
}
