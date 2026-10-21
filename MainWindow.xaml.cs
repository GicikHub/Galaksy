using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApp3
{
    public partial class MainWindow : Window
    {
        private readonly Random random = new Random();
        private readonly DispatcherTimer renderTimer = new DispatcherTimer();
        private readonly DispatcherTimer clockTimer = new DispatcherTimer();

        private readonly List<Ellipse> stars = new List<Ellipse>();
        private readonly List<double> starSpeeds = new List<double>();
        private readonly List<Ellipse> dustParticles = new List<Ellipse>();
        private readonly List<Point> dustVelocities = new List<Point>();

        private double angleX = 0.25;
        private double angleY = 0.35;
        private double angleZ = 0;
        private double warp = 1.0;
        private double zoom = 1.0;

        private int energy = 75;
        private int actions = 0;
        private int dimension = 0;
        private int currentMode = 1; // 1: Spiral, 2: Abyssal Rift, 3: Solar System, 4: Ring Galaxy, 5: Black Hole
        private int currentObjectIndex = 0;

        private bool warpEnabled = false;
        private bool paused = false;

        private readonly Color[] cosmicColors =
        {
            Color.FromRgb(0, 245, 255),
            Color.FromRgb(236, 72, 153),
            Color.FromRgb(168, 85, 247),
            Color.FromRgb(59, 130, 246),
            Color.FromRgb(244, 63, 94),
            Color.FromRgb(250, 204, 21)
        };

        private double[,] vertices = null!;
        private int[,] edges = null!;
        private int[][] faces = null!;

        private readonly double[,] cubeVertices = {
            { -1, -1, -1 }, { 1, -1, -1 }, { 1, 1, -1 }, { -1, 1, -1 },
            { -1, -1, 1 }, { 1, -1, 1 }, { 1, 1, 1 }, { -1, 1, 1 }
        };
        private readonly int[,] cubeEdges = {
            {0,1},{1,2},{2,3},{3,0},{4,5},{5,6},{6,7},{7,4},{0,4},{1,5},{2,6},{3,7}
        };
        private readonly int[][] cubeFaces = {
            new[] {0,1,2,3}, new[] {4,5,6,7}, new[] {0,1,5,4}, new[] {2,3,7,6}, new[] {1,2,6,5}, new[] {0,3,7,4}
        };

        public MainWindow()
        {
            InitializeComponent();
            LoadObjectShape(0);

            Loaded += MainWindow_Loaded;
            SizeChanged += MainWindow_SizeChanged;

            renderTimer.Interval = TimeSpan.FromMilliseconds(16);
            renderTimer.Tick += RenderTimer_Tick;

            clockTimer.Interval = TimeSpan.FromSeconds(1);
            clockTimer.Tick += ClockTimer_Tick;
            clockTimer.Start();

            ClockBlock.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void LoadObjectShape(int index)
        {
            currentObjectIndex = index;
            vertices = cubeVertices;
            edges = cubeEdges;
            faces = cubeFaces;
            ObjectTitleBlock.Text = "PRISM CUBE / 3D RENDER";
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CreateStars();
            CreateDustParticles();
            DrawScene();
            renderTimer.Start();
            Log("Abyssal Multiverse engine online.");
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsLoaded) return;
            DrawScene();
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) zoom += 0.15;
            else zoom -= 0.15;

            zoom = Math.Max(0.4, Math.Min(4.0, zoom));
            ZoomBlock.Text = "ZOOM: " + zoom.ToString("0.0") + "x";
            DrawScene();
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            ClockBlock.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void RenderTimer_Tick(object? sender, EventArgs e)
        {
            if (paused) return;

            angleX += 0.004 * warp;
            angleY += 0.007 * warp;
            angleZ += 0.001 * warp;

            if (angleX > Math.PI * 2) angleX -= Math.PI * 2;
            if (angleY > Math.PI * 2) angleY -= Math.PI * 2;
            if (angleZ > Math.PI * 2) angleZ -= Math.PI * 2;

            MoveStars();
            MoveDustParticles();
            DrawScene();

            RotationBlock.Text = "X " + Degrees(angleX).ToString("000") + "° / Y " + Degrees(angleY).ToString("000") + "°";
            WarpBlock.Text = warp.ToString("0.0") + "x";
        }

        private double Degrees(double radians) => radians * 180.0 / Math.PI;

        private void CreateStars()
        {
            StarCanvas.Children.Clear();
            stars.Clear();
            starSpeeds.Clear();

            for (int i = 0; i < 160; i++)
            {
                double size = random.Next(1, 4);
                Ellipse star = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(cosmicColors[random.Next(cosmicColors.Length)]),
                    Opacity = 0.4 + random.NextDouble() * 0.6
                };

                stars.Add(star);
                starSpeeds.Add(0.3 + random.NextDouble() * 1.5);
                StarCanvas.Children.Add(star);

                Canvas.SetLeft(star, random.Next(0, 950));
                Canvas.SetTop(star, random.Next(0, 700));
            }
            StarsBlock.Text = (stars.Count + dustParticles.Count).ToString();
        }

        private void CreateDustParticles()
        {
            ParticleCanvas.Children.Clear();
            dustParticles.Clear();
            dustVelocities.Clear();

            for (int i = 0; i < 70; i++)
            {
                Ellipse p = new Ellipse
                {
                    Width = 2,
                    Height = 2,
                    Fill = Brushes.Cyan,
                    Opacity = 0.8
                };
                dustParticles.Add(p);
                dustVelocities.Add(new Point((random.NextDouble() - 0.5) * 2.5, (random.NextDouble() - 0.5) * 2.5));
                ParticleCanvas.Children.Add(p);
            }
        }

        private void MoveStars()
        {
            double width = StarCanvas.ActualWidth;
            double height = StarCanvas.ActualHeight;
            if (width <= 0 || height <= 0) return;

            Point center = new Point(width / 2, height / 2);

            for (int i = 0; i < stars.Count; i++)
            {
                Ellipse star = stars[i];
                double x = Canvas.GetLeft(star);
                double y = Canvas.GetTop(star);

                if (currentMode == 5) // Black hole gravity pull
                {
                    double dx = center.X - x;
                    double dy = center.Y - y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < 15)
                    {
                        x = random.Next(0, (int)width);
                        y = random.Next(0, (int)height);
                    }
                    else
                    {
                        x += (dx / dist) * (starSpeeds[i] * warp * 3.0);
                        y += (dy / dist) * (starSpeeds[i] * warp * 3.0);
                    }
                }
                else
                {
                    x -= starSpeeds[i] * warp;
                    if (x < 0)
                    {
                        x = width;
                        y = random.Next(0, (int)height);
                    }
                }

                Canvas.SetLeft(star, x);
                Canvas.SetTop(star, y);
            }
        }

        private void MoveDustParticles()
        {
            double width = ParticleCanvas.ActualWidth;
            double height = ParticleCanvas.ActualHeight;
            if (width <= 0 || height <= 0) return;

            for (int i = 0; i < dustParticles.Count; i++)
            {
                Ellipse p = dustParticles[i];
                double x = Canvas.GetLeft(p) + dustVelocities[i].X * warp;
                double y = Canvas.GetTop(p) + dustVelocities[i].Y * warp;

                if (x < 0 || x > width || y < 0 || y > height)
                {
                    x = width / 2;
                    y = height / 2;
                }

                Canvas.SetLeft(p, x);
                Canvas.SetTop(p, y);
            }
        }

        private void DrawScene()
        {
            if (CubeCanvas == null) return;
            CubeCanvas.Children.Clear();

            if (currentMode == 0) DrawObject3D();
            else if (currentMode == 1) DrawSpiralGalaxy3D();
            else if (currentMode == 2) DrawAbyssalRift3D(); // 2. Nowy wymiar: ABYSSAL RIFT
            else if (currentMode == 3) DrawDetailedSolarSystem3D();
            else if (currentMode == 4) DrawRingGalaxy3D();
            else if (currentMode == 5) DrawRealisticBlackHole();
        }

        private Point Project3DPoint(double x, double y, double z, double width, double height, double scale)
        {
            double currentScale = scale * zoom;

            double cosX = Math.Cos(angleX), sinX = Math.Sin(angleX);
            double cosY = Math.Cos(angleY), sinY = Math.Sin(angleY);
            double cosZ = Math.Cos(angleZ), sinZ = Math.Sin(angleZ);

            double y1 = y * cosX - z * sinX;
            double z1 = y * sinX + z * cosX;
            double x2 = x * cosY + z1 * sinY;
            double z2 = -x * sinY + z1 * cosY;
            double x3 = x2 * cosZ - y1 * sinZ;
            double y3 = x2 * sinZ + y1 * cosZ;

            double camera = 4.5;
            double perspective = camera / (camera - z2);

            return new Point(
                width / 2 + x3 * currentScale * perspective,
                height / 2 + y3 * currentScale * perspective);
        }

        // 1. ZAAWANSOWANA GALAKTYKA SPIRALNA 3D
        private void DrawSpiralGalaxy3D()
        {
            double width = CubeCanvas.ActualWidth;
            double height = CubeCanvas.ActualHeight;
            if (width <= 0 || height <= 0) return;

            double scale = Math.Min(width, height) * 0.3;

            Point corePos = Project3DPoint(0, 0, 0, width, height, scale);
            Ellipse core = new Ellipse
            {
                Width = 50,
                Height = 50,
                Fill = new SolidColorBrush(Color.FromRgb(255, 245, 220)),
                Opacity = 0.95
            };
            Canvas.SetLeft(core, corePos.X - 25);
            Canvas.SetTop(core, corePos.Y - 25);
            CubeCanvas.Children.Add(core);

            int arms = 4;
            int particlesPerArm = 60;

            for (int a = 0; a < arms; a++)
            {
                double armOffset = a * (Math.PI * 2 / arms);
                for (int p = 0; p < particlesPerArm; p++)
                {
                    double r = (p / (double)particlesPerArm) * 3.2;
                    double theta = r * 4.0 + armOffset + angleX;

                    double x = Math.Cos(theta) * r;
                    double z = Math.Sin(theta) * r;
                    double y = (random.NextDouble() - 0.5) * 0.25;

                    Point pt = Project3DPoint(x, y, z, width, height, scale);

                    double starSize = 2.5 + (1.0 - (p / (double)particlesPerArm)) * 3.5;
                    Ellipse star = new Ellipse
                    {
                        Width = starSize,
                        Height = starSize,
                        Fill = new SolidColorBrush(cosmicColors[(a + p) % cosmicColors.Length]),
                        Opacity = 0.8
                    };
                    Canvas.SetLeft(star, pt.X - starSize / 2);
                    Canvas.SetTop(star, pt.Y - starSize / 2);
                    CubeCanvas.Children.Add(star);
                }
            }
        }

        // 2. NOWY WYMIAR: ABYSSAL RIFT (Mroczny Wymiar / Szczelina Otchłani w 3D)
        private void DrawAbyssalRift3D()
        {
            double width = CubeCanvas.ActualWidth;
            double height = CubeCanvas.ActualHeight;
            if (width <= 0 || height <= 0) return;

            double scale = Math.Min(width, height) * 0.3;

            // Pulsujący, mroczny portal w centrum
            Point riftCenter = Project3DPoint(0, 0, 0, width, height, scale);
            Ellipse coreGlow = new Ellipse
            {
                Width = 70,
                Height = 70,
                Fill = new SolidColorBrush(Color.FromArgb(100, 128, 0, 128))
            };
            Canvas.SetLeft(coreGlow, riftCenter.X - 35);
            Canvas.SetTop(coreGlow, riftCenter.Y - 35);
            CubeCanvas.Children.Add(coreGlow);

            Ellipse coreDark = new Ellipse
            {
                Width = 35,
                Height = 35,
                Fill = Brushes.Black,
                Stroke = Brushes.DarkViolet,
                StrokeThickness = 3
            };
            Canvas.SetLeft(coreDark, riftCenter.X - 17.5);
            Canvas.SetTop(coreDark, riftCenter.Y - 17.5);
            CubeCanvas.Children.Add(coreDark);

            // Zakrzywione, pionowe i poziome pierścienie kwantowe otchłani (efekt szczeliny)
            int rings = 5;
            for (int i = 0; i < rings; i++)
            {
                double radius = 1.2 + i * 0.5;
                Polyline riftRing = new Polyline
                {
                    Stroke = new SolidColorBrush(Color.FromArgb((byte)(120 - i * 15), 180, 0, 255)),
                    StrokeThickness = 2
                };
                PointCollection ringPoints = new PointCollection();
                for (int step = 0; step <= 36; step++)
                {
                    double theta = step * (Math.PI * 2 / 36);
                    double rx = Math.Cos(theta) * radius;
                    double rz = Math.Sin(theta) * radius;
                    double ry = Math.Sin(theta * 2 + angleX) * 0.4; // falowanie przestrzeni
                    ringPoints.Add(Project3DPoint(rx, ry, rz, width, height, scale));
                }
                riftRing.Points = ringPoints;
                CubeCanvas.Children.Add(riftRing);
            }

            // Mroczne cząsteczki energii otaczające szczelinę
            int abyssalParticles = 80;
            for (int i = 0; i < abyssalParticles; i++)
            {
                double u = i * (Math.PI * 2 / abyssalParticles) + angleY * 2;
                double d = 1.0 + (i % 4) * 0.6;
                double px = Math.Cos(u) * d;
                double pz = Math.Sin(u) * d;
                double py = Math.Sin(u * 3) * 0.8;

                Point pt = Project3DPoint(px, py, pz, width, height, scale);
                Ellipse energyBit = new Ellipse
                {
                    Width = 2.5,
                    Height = 2.5,
                    Fill = Brushes.Magenta,
                    Opacity = 0.9
                };
                Canvas.SetLeft(energyBit, pt.X - 1.25);
                Canvas.SetTop(energyBit, pt.Y - 1.25);
                CubeCanvas.Children.Add(energyBit);
            }
        }

        // 3. SZCZEGÓŁOWY UKŁAD SŁONECZNY 3D
        private void DrawDetailedSolarSystem3D()
        {
            double width = CubeCanvas.ActualWidth;
            double height = CubeCanvas.ActualHeight;
            if (width <= 0 || height <= 0) return;

            double scale = Math.Min(width, height) * 0.25;

            Point sunPos = Project3DPoint(0, 0, 0, width, height, scale);
            Ellipse sun = new Ellipse
            {
                Width = 44,
                Height = 44,
                Fill = new SolidColorBrush(Color.FromRgb(255, 140, 0)),
                Stroke = new SolidColorBrush(Color.FromRgb(255, 220, 0)),
                StrokeThickness = 3
            };
            Canvas.SetLeft(sun, sunPos.X - 22);
            Canvas.SetTop(sun, sunPos.Y - 22);
            CubeCanvas.Children.Add(sun);

            double[] radii = { 0.7, 1.1, 1.6, 2.1, 2.8, 3.6, 4.3, 5.0 };
            double[] sizes = { 6, 9, 11, 8, 20, 16, 13, 12 };
            Color[] pColors = {
                Color.FromRgb(128, 128, 128), Color.FromRgb(235, 185, 110),
                Color.FromRgb(30, 144, 255), Color.FromRgb(204, 68, 34),
                Color.FromRgb(210, 160, 120), Color.FromRgb(235, 200, 120),
                Color.FromRgb(135, 206, 235), Color.FromRgb(65, 105, 225)
            };

            for (int i = 0; i < radii.Length; i++)
            {
                double r = radii[i];
                Polyline orbitLine = new Polyline
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
                    StrokeThickness = 1
                };
                PointCollection orbitPoints = new PointCollection();
                for (int step = 0; step <= 40; step++)
                {
                    double theta = step * (Math.PI * 2 / 40);
                    orbitPoints.Add(Project3DPoint(Math.Cos(theta) * r, 0, Math.Sin(theta) * r, width, height, scale));
                }
                orbitLine.Points = orbitPoints;
                CubeCanvas.Children.Add(orbitLine);

                double planetAngle = angleX * (2.5 / (i + 0.8)) + i * 1.3;
                Point pPos = Project3DPoint(Math.Cos(planetAngle) * r, 0, Math.Sin(planetAngle) * r, width, height, scale);

                if (i == 5) // Saturn rings
                {
                    Ellipse ring = new Ellipse
                    {
                        Width = sizes[i] * 2.4,
                        Height = sizes[i] * 0.8,
                        Stroke = new SolidColorBrush(Color.FromArgb(180, 210, 180, 140)),
                        StrokeThickness = 2.5
                    };
                    Canvas.SetLeft(ring, pPos.X - sizes[i] * 1.2);
                    Canvas.SetTop(ring, pPos.Y - sizes[i] * 0.4);
                    CubeCanvas.Children.Add(ring);
                }

                Ellipse planet = new Ellipse
                {
                    Width = sizes[i],
                    Height = sizes[i],
                    Fill = new SolidColorBrush(pColors[i]),
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(planet, pPos.X - sizes[i] / 2);
                Canvas.SetTop(planet, pPos.Y - sizes[i] / 2);
                CubeCanvas.Children.Add(planet);
            }
        }

        // 4. GALAKTYKA PIERŚCIENIOWA (RING GALAXY)
        private void DrawRingGalaxy3D()
        {
            double width = CubeCanvas.ActualWidth;
            double height = CubeCanvas.ActualHeight;
            if (width <= 0 || height <= 0) return;

            double scale = Math.Min(width, height) * 0.3;

            Point corePos = Project3DPoint(0, 0, 0, width, height, scale);
            Ellipse core = new Ellipse
            {
                Width = 30,
                Height = 30,
                Fill = new SolidColorBrush(Color.FromRgb(255, 255, 200)),
                Opacity = 0.9
            };
            Canvas.SetLeft(core, corePos.X - 15);
            Canvas.SetTop(core, corePos.Y - 15);
            CubeCanvas.Children.Add(core);

            int ringParticles = 100;
            double ringRadius = 2.2;

            for (int i = 0; i < ringParticles; i++)
            {
                double theta = i * (Math.PI * 2 / ringParticles) + angleX * 1.5;
                double x = Math.Cos(theta) * ringRadius + (random.NextDouble() - 0.5) * 0.2;
                double z = Math.Sin(theta) * ringRadius + (random.NextDouble() - 0.5) * 0.2;
                double y = (random.NextDouble() - 0.5) * 0.15;

                Point pt = Project3DPoint(x, y, z, width, height, scale);

                double size = 2 + random.NextDouble() * 2.5;
                Ellipse star = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(cosmicColors[i % cosmicColors.Length]),
                    Opacity = 0.85
                };
                Canvas.SetLeft(star, pt.X - size / 2);
                Canvas.SetTop(star, pt.Y - size / 2);
                CubeCanvas.Children.Add(star);
            }
        }

        // 5. REALISTYCZNA CZARNA DZIURA
        private void DrawRealisticBlackHole()
        {
            double width = CubeCanvas.ActualWidth;
            double height = CubeCanvas.ActualHeight;
            if (width <= 0 || height <= 0) return;

            Point center = new Point(width / 2, height / 2);

            for (int i = 0; i < 8; i++)
            {
                double rx = 55 + i * 16;
                double ry = 22 + i * 6;
                Ellipse ring = new Ellipse
                {
                    Width = rx * 2,
                    Height = ry * 2,
                    Stroke = new SolidColorBrush(cosmicColors[i % cosmicColors.Length]),
                    StrokeThickness = 2.5,
                    Opacity = 0.85
                };
                Canvas.SetLeft(ring, center.X - rx);
                Canvas.SetTop(ring, center.Y - ry);
                CubeCanvas.Children.Add(ring);
            }

            Ellipse eventHorizon = new Ellipse
            {
                Width = 85,
                Height = 85,
                Fill = Brushes.Black,
                Stroke = Brushes.Cyan,
                StrokeThickness = 3
            };
            Canvas.SetLeft(eventHorizon, center.X - 42.5);
            Canvas.SetTop(eventHorizon, center.Y - 42.5);
            CubeCanvas.Children.Add(eventHorizon);
        }

        private void DrawObject3D()
        {
            double width = CubeCanvas.ActualWidth;
            double height = CubeCanvas.ActualHeight;
            if (width <= 0 || height <= 0) return;

            Point[] points = TransformVertices();
            Color mainColor = cosmicColors[dimension];

            foreach (int[] f in faces)
            {
                Polygon polygon = new Polygon();
                PointCollection collection = new PointCollection();
                foreach (int vIdx in f) collection.Add(points[vIdx]);
                polygon.Points = collection;

                Color faceColor = cosmicColors[(dimension + f.Length) % cosmicColors.Length];
                polygon.Fill = new SolidColorBrush(Color.FromArgb(50, faceColor.R, faceColor.G, faceColor.B));
                polygon.Stroke = new SolidColorBrush(Color.FromArgb(180, mainColor.R, mainColor.G, mainColor.B));
                polygon.StrokeThickness = 1;
                CubeCanvas.Children.Add(polygon);
            }

            for (int i = 0; i < edges.GetLength(0); i++)
            {
                Point a = points[edges[i, 0]];
                Point b = points[edges[i, 1]];
                Line edge = new Line
                {
                    X1 = a.X,
                    Y1 = a.Y,
                    X2 = b.X,
                    Y2 = b.Y,
                    Stroke = new SolidColorBrush(cosmicColors[(dimension + i) % cosmicColors.Length]),
                    StrokeThickness = 2,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                CubeCanvas.Children.Add(edge);
            }
        }

        private Point[] TransformVertices()
        {
            int count = vertices.GetLength(0);
            Point[] projected = new Point[count];
            double width = CubeCanvas.ActualWidth;
            double height = CubeCanvas.ActualHeight;
            double scale = Math.Min(width, height) * 0.28;

            for (int i = 0; i < count; i++)
            {
                projected[i] = Project3DPoint(vertices[i, 0], vertices[i, 1], vertices[i, 2], width, height, scale);
            }
            return projected;
        }

        private void Warp_Click(object sender, RoutedEventArgs e)
        {
            warpEnabled = !warpEnabled;
            warp = warpEnabled ? 3.5 : 1.0;
            RenderModeBlock.Text = warpEnabled ? "WARP SPEED ACTIVE" : "NORMAL SPEED";
            StatusBlock.Text = warpEnabled ? "● WARP DRIVE ENGAGED" : "● SIMULATION ONLINE";
            RegisterAction(warpEnabled ? "Warp speed engaged." : "Warp disengaged.");
        }

        private void SpiralGalaxy_Click(object sender, RoutedEventArgs e)
        {
            currentMode = 1;
            ObjectTitleBlock.Text = "SPIRAL GALAXY / 3D RENDER";
            CoreGlow.Fill = new SolidColorBrush(Color.FromArgb(100, 168, 85, 247));
            DrawScene();
            RegisterAction("Switched to Spiral Galaxy.");
        }

        // Nowa metoda obsługi przycisku Abyssal Rift
        private void AbyssalRift_Click(object sender, RoutedEventArgs e)
        {
            currentMode = 2;
            ObjectTitleBlock.Text = "ABYSSAL RIFT / QUANTUM PORTAL";
            CoreGlow.Fill = new SolidColorBrush(Color.FromArgb(100, 128, 0, 128));
            DrawScene();
            RegisterAction("Opened Abyssal Rift dimension.");
        }

        private void SolarSystem_Click(object sender, RoutedEventArgs e)
        {
            currentMode = 3;
            ObjectTitleBlock.Text = "3D SOLAR SYSTEM / DETAILED";
            CoreGlow.Fill = new SolidColorBrush(Color.FromArgb(80, 255, 140, 0));
            DrawScene();
            RegisterAction("Switched to Detailed Solar System 3D.");
        }

        private void RingGalaxy_Click(object sender, RoutedEventArgs e)
        {
            currentMode = 4;
            ObjectTitleBlock.Text = "RING GALAXY";
            CoreGlow.Fill = new SolidColorBrush(Color.FromArgb(90, 0, 245, 255));
            DrawScene();
            RegisterAction("Switched to Ring Galaxy.");
        }

        private void BlackHole_Click(object sender, RoutedEventArgs e)
        {
            currentMode = 5;
            ObjectTitleBlock.Text = "REALISTIC BLACK HOLE SINGULARITY";
            CoreGlow.Fill = new SolidColorBrush(Color.FromArgb(120, 59, 7, 100));
            DrawScene();
            RegisterAction("Realistic Black Hole activated!");
        }

        private void Object_Click(object sender, RoutedEventArgs e)
        {
            currentMode = 0;
            LoadObjectShape(0);
            CoreGlow.Fill = new SolidColorBrush(Color.FromRgb(59, 7, 100));
            DrawScene();
            RegisterAction("Switched to 3D Prism Cube.");
        }

        private void Charge_Click(object sender, RoutedEventArgs e)
        {
            energy = Math.Min(100, energy + 15);
            UpdateEnergy();
            RegisterAction("Nebula core charged: " + energy + "%.");
        }

        private void Release_Click(object sender, RoutedEventArgs e)
        {
            if (energy < 10) { RegisterAction("Insufficient energy."); return; }
            energy = Math.Max(0, energy - 20);
            UpdateEnergy();
            RegisterAction("Energy pulse discharged!");
        }

        private void Dimension_Click(object sender, RoutedEventArgs e)
        {
            dimension = (dimension + 1) % cosmicColors.Length;
            Color color = cosmicColors[dimension];
            Brush brush = new SolidColorBrush(color);
            StatusBlock.Foreground = brush;
            EnergyPercent.Foreground = brush;
            EnergyBlock.Foreground = brush;
            CoreGlow.Fill = brush;
            DrawScene();
            RegisterAction("Galaxy color hue shifted.");
        }

        private void StarBurst_Click(object sender, RoutedEventArgs e)
        {
            CreateStars();
            CreateDustParticles();
            RegisterAction("Star field regenerated.");
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            paused = !paused;
            StatusBlock.Text = paused ? "● SIMULATION PAUSED" : "● SIMULATION ONLINE";
            RegisterAction(paused ? "Simulation paused." : "Simulation resumed.");
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            angleX = 0.25; angleY = 0.35; angleZ = 0;
            warp = 1.0; warpEnabled = false; paused = false;
            zoom = 1.0; dimension = 0; energy = 75; currentMode = 1;
            ZoomBlock.Text = "ZOOM: 1.0x";
            UpdateEnergy();
            CreateStars();
            CreateDustParticles();
            DrawScene();
            RegisterAction("Universe reset to defaults.");
        }

        private void UpdateEnergy()
        {
            EnergyBlock.Text = energy.ToString("D3");
            EnergyBar.Value = energy;
            EnergyPercent.Text = energy + " / 100";
        }

        private void RegisterAction(string message)
        {
            actions++;
            ActionsBlock.Text = "ACTIONS: " + actions;
            Log(message);
        }

        private void Log(string message)
        {
            if (LogBlock == null) return;
            LogBlock.Text = "> " + DateTime.Now.ToString("HH:mm:ss") + " " + message;
        }

        protected override void OnClosed(EventArgs e)
        {
            renderTimer.Stop();
            clockTimer.Stop();
            base.OnClosed(e);
        }
    }
}