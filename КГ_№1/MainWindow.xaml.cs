using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace КГ__1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Vector4D> originalVertices;
        private List<Vector4D> transformedVertices;
        private List<Edge> edges;

        // Локальные оси буквы
        private List<AxisLine> localAxes;

        // Параметры аффинных преобразований
        private double rotX = 0, rotY = 0, rotZ = 0;
        private double scale = 1.0;
        private Point position = new Point(0, 0);

        // Мышь
        private Point lastMousePos;
        private bool isDragging = false;

        // Таймер для Варианта №1 (Анимация замедления)
        private DispatcherTimer animTimer;
        private Point animStartPos;
        private Point animTargetPos;
        private double animProgress = 0;
        private const double ANIM_DURATION_FRAMES = 60.0;

        public MainWindow()
        {
            InitializeComponent();
            InitLetterMesh();
            InitAxes();
            InitTimer();
            UpdateTransformations();
        }

        // Инициализация 3D-буквы "Н"
        private void InitLetterMesh()
        {
            originalVertices = new List<Vector4D>
            {
                // Передняя грань (Z = +20)
                new Vector4D(-70, -80,  20), new Vector4D(-30, -80,  20),
                new Vector4D(-30,  80,  20), new Vector4D(-70,  80,  20),
                new Vector4D( 30, -80,  20), new Vector4D( 70, -80,  20),
                new Vector4D( 70,  80,  20), new Vector4D( 30,  80,  20),
                new Vector4D(-30, -20,  20), new Vector4D( 30, -20,  20),
                new Vector4D( 30,  20,  20), new Vector4D(-30,  20,  20),

                // Задняя грань (Z = -20)
                new Vector4D(-70, -80, -20), new Vector4D(-30, -80, -20),
                new Vector4D(-30,  80, -20), new Vector4D(-70,  80, -20),
                new Vector4D( 30, -80, -20), new Vector4D( 70, -80, -20),
                new Vector4D( 70,  80, -20), new Vector4D( 30,  80, -20),
                new Vector4D(-30, -20, -20), new Vector4D( 30, -20, -20),
                new Vector4D( 30,  20, -20), new Vector4D(-30,  20, -20)
            };

            edges = new List<Edge>
            {
                // Передняя грань
                new Edge(0,1), new Edge(1,8), new Edge(8,11), new Edge(11,2), new Edge(2,3), new Edge(3,0),
                new Edge(4,5), new Edge(5,6), new Edge(6,7), new Edge(7,10), new Edge(10,9), new Edge(9,4),
                new Edge(8,9), new Edge(11,10),

                // Задняя грань
                new Edge(12,13), new Edge(13,20), new Edge(20,23), new Edge(23,14), new Edge(14,15), new Edge(15,12),
                new Edge(16,17), new Edge(17,18), new Edge(18,19), new Edge(19,22), new Edge(22,21), new Edge(21,16),
                new Edge(20,21), new Edge(23,22),

                // Связи объема
                new Edge(0,12), new Edge(1,13), new Edge(2,14), new Edge(3,15),
                new Edge(4,16), new Edge(5,17), new Edge(6,18), new Edge(7,19),
                new Edge(8,20), new Edge(9,21), new Edge(10,22), new Edge(11,23)
            };

            transformedVertices = new List<Vector4D>(new Vector4D[originalVertices.Count]);
        }

        // Создаем оси координат (Красный = X, Зеленый = Y, Синий = Z)
        private void InitAxes()
        {
            // Локальные оси, привязанные к самой букве
            localAxes = new List<AxisLine>
            {
                new AxisLine(new Vector4D(0, 0, 0), new Vector4D(120, 0, 0), Brushes.OrangeRed, "X_obj"),
                new AxisLine(new Vector4D(0, 0, 0), new Vector4D(0, -120, 0), Brushes.SpringGreen, "Y_obj"),
                new AxisLine(new Vector4D(0, 0, 0), new Vector4D(0, 0, 120), Brushes.Cyan, "Z_obj")
            };
        }

        private void InitTimer()
        {
            animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            animTimer.Tick += AnimTimer_Tick;
        }

        // Перемножение матриц 4x4
        private double[,] MultiplyMatrices(double[,] a, double[,] b)
        {
            double[,] res = new double[4, 4];
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                    for (int k = 0; k < 4; k++)
                        res[r, c] += a[r, k] * b[k, c];
            return res;
        }

        // Умножение матрицы на вектор
        private Vector4D MultiplyMatrixVector(double[,] m, Vector4D v)
        {
            return new Vector4D(
                m[0, 0] * v.X + m[0, 1] * v.Y + m[0, 2] * v.Z + m[0, 3] * v.W,
                m[1, 0] * v.X + m[1, 1] * v.Y + m[1, 2] * v.Z + m[1, 3] * v.W,
                m[2, 0] * v.X + m[2, 1] * v.Y + m[2, 2] * v.Z + m[2, 3] * v.W,
                m[3, 0] * v.X + m[3, 1] * v.Y + m[3, 2] * v.Z + m[3, 3] * v.W
            );
        }

        // Пересчет координат матрицами
        private void UpdateTransformations()
        {
            double cx = Math.Cos(rotX), sx = Math.Sin(rotX);
            double cy = Math.Cos(rotY), sy = Math.Sin(rotY);
            double cz = Math.Cos(rotZ), sz = Math.Sin(rotZ);

            double[,] scaleM = {
                { scale, 0, 0, 0 },
                { 0, scale, 0, 0 },
                { 0, 0, scale, 0 },
                { 0, 0, 0, 1 }
            };

            double[,] rotXM = {
                { 1, 0, 0, 0 },
                { 0, cx, -sx, 0 },
                { 0, sx, cx, 0 },
                { 0, 0, 0, 1 }
            };

            double[,] rotYM = {
                { cy, 0, sy, 0 },
                { 0, 1, 0, 0 },
                { -sy, 0, cy, 0 },
                { 0, 0, 0, 1 }
            };

            double[,] rotZM = {
                { cz, -sz, 0, 0 },
                { sz,  cz, 0, 0 },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            };

            double[,] transM = {
                { 1, 0, 0, position.X },
                { 0, 1, 0, position.Y },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            };

            // Матрица трансформации объекта: T * Ry * Rx * Rz* S
            double[,] M = MultiplyMatrices(scaleM, rotXM);
            M = MultiplyMatrices(M, rotYM);
            M = MultiplyMatrices(M, rotZM);
            M = MultiplyMatrices(M, transM);

            // Трансформируем вершины буквы
            for (int i = 0; i < originalVertices.Count; i++)
            {
                transformedVertices[i] = MultiplyMatrixVector(M, originalVertices[i]);
            }

            UpdateUIInfo();
            Render(M);
        }

        // Обновление отображения координат в интерфейсе
        private void UpdateUIInfo()
        {
            double degX = (rotX * 180 / Math.PI) % 360;
            double degY = (rotY * 180 / Math.PI) % 360;
            double degZ = (rotZ * 180 / Math.PI) % 360;

            TxtInfo.Text = $"КООРДИНАТЫ И СОСТОЯНИЕ:\n" +
                           $"----------------------------\n" +
                           $"Смещение X : {position.X,6:F1} px\n" +
                           $"Смещение Y : {position.Y,6:F1} px\n" +
                           $"Поворот Rx : {degX,6:F1}°\n" +
                           $"Поворот Ry : {degY,6:F1}°\n" +
                           $"Поворот Rz : {degZ,6:F1}° (Клавиши Q/E)\n" +
                           $"Масштаб    : {scale,6:F2}x";
        }

        // Отрисовка
        private void Render(double[,] objectMatrix)
        {
            MainCanvas.Children.Clear();

            double centerX = MainCanvas.ActualWidth / 2;
            double centerY = MainCanvas.ActualHeight / 2;
            if (centerX <= 0) centerX = 400;
            if (centerY <= 0) centerY = 300;

            double distance = 400.0;

            // Отрисовка локальных осей
            foreach (var axis in localAxes)
            {
                Vector4D tStart = MultiplyMatrixVector(objectMatrix, axis.Start);
                Vector4D tEnd = MultiplyMatrixVector(objectMatrix, axis.End);

                double z1 = tStart.Z + distance; if (z1 < 1) z1 = 1;
                double z2 = tEnd.Z + distance; if (z2 < 1) z2 = 1;

                Point p1 = new Point((tStart.X * distance) / z1 + centerX, (tStart.Y * distance) / z1 + centerY);
                Point p2 = new Point((tEnd.X * distance) / z2 + centerX, (tEnd.Y * distance) / z2 + centerY);

                Line line = new Line
                {
                    X1 = p1.X,
                    Y1 = p1.Y,
                    X2 = p2.X,
                    Y2 = p2.Y,
                    Stroke = axis.Color,
                    StrokeThickness = 3
                };
                MainCanvas.Children.Add(line);
            }

            // Отрисовка ребер буквы "Н"
            List<Point> projected = new List<Point>();
            foreach (var v in transformedVertices)
            {
                double z = v.Z + distance; if (z < 1) z = 1;
                projected.Add(new Point((v.X * distance) / z + centerX, (v.Y * distance) / z + centerY));
            }

            foreach (var edge in edges)
            {
                Line line = new Line
                {
                    X1 = projected[edge.Start].X,
                    Y1 = projected[edge.Start].Y,
                    X2 = projected[edge.End].X,
                    Y2 = projected[edge.End].Y,
                    Stroke = Brushes.Cyan,
                    StrokeThickness = 2
                };
                MainCanvas.Children.Add(line);
            }
        }

        // События ввода
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
            {
                StartAnimation();
                return;
            }

            if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right)
            {
                isDragging = true;
                lastMousePos = e.GetPosition(this);
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;

            Point currentPos = e.GetPosition(this);
            double dx = currentPos.X - lastMousePos.X;
            double dy = currentPos.Y - lastMousePos.Y;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                rotY += dx * 0.01;
                rotX += dy * 0.01;
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                position.X += dx;
                position.Y += dy;
            }

            lastMousePos = currentPos;
            UpdateTransformations();
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) scale *= 1.1;
            else scale *= 0.9;

            UpdateTransformations();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            double step = 0.05;
            if (e.Key == Key.Left) rotY -= step;
            if (e.Key == Key.Right) rotY += step;
            if (e.Key == Key.Up) rotX -= step;
            if (e.Key == Key.Down) rotX += step;
            if (e.Key == Key.Q) rotZ -= step;
            if (e.Key == Key.E) rotZ += step;
            UpdateTransformations();
        }

        // Анимация замедления (Вариант 1)
        private void StartAnimation()
        {
            if (animTimer.IsEnabled) return;

            animStartPos = position;
            Random rnd = new Random();
            animTargetPos = new Point(rnd.Next(-200, 200), rnd.Next(-150, 150));

            animProgress = 0;
            animTimer.Start();
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            animProgress += 1.0 / ANIM_DURATION_FRAMES;

            if (animProgress >= 1.0)
            {
                animProgress = 1.0;
                animTimer.Stop();
            }

            double easeOut = 1.0 - (1.0 - animProgress) * (1.0 - animProgress);

            position.X = animStartPos.X + (animTargetPos.X - animStartPos.X) * easeOut;
            position.Y = animStartPos.Y + (animTargetPos.Y - animStartPos.Y) * easeOut;

            UpdateTransformations();
        }
    }
}