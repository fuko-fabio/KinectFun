using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace KinectFun
{
    public enum GameMode
    {
        Off = 0,
        OnePlayer = 1,
        TwoPlayer = 2
    }

    class Game
    {
        #region ::::: POLA :::::
        private GameMode gameMode;
        private Rect rect;
        private bool runningGameThread;
        private DateTime predNextFrame;
        private double targetFramerate = 20;
        private double actualFrameTime;
        private DateTime lastFrameDrawn;
        private int frameCount;
        private const double MinFramerate = 15;
        private double TimerResolution = 2; //ms
        private Dispatcher dispatcher;
        private Canvas playField;
        public bool GameIsStarted { get; set; }
        public readonly Dictionary<int, Player> players = new Dictionary<int, Player>();
        private Random random = new Random();
        private List<Path> pathList { get; set; }
        private List<EllipseGeometry> ellipseGeometryList { get; set; }
        public int baloons { get; set; }
        private MediaPlayer player = new MediaPlayer();
        private PointAnimationUsingPath up = new PointAnimationUsingPath();
        private PolyBezierSegment pBezierSegment = new PolyBezierSegment();
        private PathGeometry animationPath = new PathGeometry();
        private PathFigure pFigure = new PathFigure();
        private Line needle = new Line();
        private Line needle2 = new Line();
        private int j = 0;
        private ArrayList times;
        private int timesIndex = 0;
        private Label label;
        #endregion

        public Game(Label label, Dispatcher dispatcher, Rect rect, Canvas playField)
        {
            this.label = label;
            this.dispatcher = dispatcher;
            this.rect = rect;
            this.playField = playField;
            GameIsStarted = false;
            baloons = 50;
        }
        public int PlayersAlive { get; set; }


        internal void SetGameMode(GameMode gameMode)
        {
            this.gameMode = gameMode;
        }

        private void CheckPlayers()
        {
            foreach (var player in this.players)
            {
                if (!player.Value.IsAlive)
                {
                    // Player left scene since we aren't tracking it anymore, so remove from dictionary
                    this.players.Remove(player.Value.GetId());
                    break;
                }
            }

            // Count alive players
            int alive = this.players.Count(player => player.Value.IsAlive);

            if (alive != this.PlayersAlive)
            {
                if (alive == 2)
                {
                    this.SetGameMode(GameMode.TwoPlayer);
                }
                else if (alive == 1)
                {
                    this.SetGameMode(GameMode.OnePlayer);
                }
                else if (alive == 0)
                {
                    this.SetGameMode(GameMode.Off);
                }

                if (this.PlayersAlive == 0)
                {
                    BannerText.NewBanner(
                        Properties.Resources.Vocabulary,
                        this.rect,
                        true,
                        Color.FromArgb(200, 255, 255, 255));
                    this.GameIsStarted = false;
                }

                this.PlayersAlive = alive;
            }
        }

        public void StartGame()
        {
            GameIsStarted = true;            
            times = RandomNumbers(baloons);
            InitializeBaloons();      
            needle.StrokeEndLineCap = PenLineCap.Triangle;
            if (!playField.Children.Contains(needle))
            {
                playField.Children.Add(needle);
            }
            needle2.StrokeEndLineCap = PenLineCap.Triangle;
            if (!playField.Children.Contains(needle2))
            {
                playField.Children.Add(needle2);
            }
            foreach (EllipseGeometry geometry in ellipseGeometryList)
            {
                PrepareAnimation(geometry);
            }
           
            foreach (Path path in pathList)
            {
                RandomBaloonStyle(path);
            }
        }

        public void GameThread()
        {
            this.runningGameThread = true;
            this.predNextFrame = DateTime.Now;
            this.actualFrameTime = 1000.0 / this.targetFramerate;

            // Try to dispatch at as constant of a framerate as possible by sleeping just enough since
            // the last time we dispatched.
            while (this.runningGameThread)
            {
                // Calculate average framerate.  
                DateTime now = DateTime.Now;
                if (this.lastFrameDrawn == DateTime.MinValue)
                {
                    this.lastFrameDrawn = now;
                }

                double ms = now.Subtract(this.lastFrameDrawn).TotalMilliseconds;
                this.actualFrameTime = (this.actualFrameTime * 0.95) + (0.05 * ms);
                this.lastFrameDrawn = now;

                // Adjust target framerate down if we're not achieving that rate
                this.frameCount++;
                if ((this.frameCount % 100 == 0) && (1000.0 / this.actualFrameTime < this.targetFramerate * 0.92))
                {
                    this.targetFramerate = Math.Max(MinFramerate, (this.targetFramerate + (1000.0 / this.actualFrameTime)) / 2);
                }

                if (now > this.predNextFrame)
                {
                    this.predNextFrame = now;
                }
                else
                {
                    double milliseconds = this.predNextFrame.Subtract(now).TotalMilliseconds;
                    if (milliseconds >= TimerResolution)
                    {
                        System.Threading.Thread.Sleep((int)(milliseconds + 0.5));
                    }
                }

                this.predNextFrame += TimeSpan.FromMilliseconds(1000.0 / this.targetFramerate);             
                this.dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send, new Action<int>(this.HandleGameTimer), 0);
            }
        }

        public void ClearPlayField()
        {
            playField.Children.Clear();
        }

        private void HandleGameTimer(int param)
        {

            if (!GameIsStarted)
            {
                playField.Children.Clear();
                foreach (var player in this.players)
                {
                    player.Value.Draw(playField.Children);
                }
                BannerText.Draw(playField.Children);
                FlyingText.Draw(playField.Children);
            }
            else
            {
                CheckColisions();
            }

            this.CheckPlayers();
        }

        private void CheckColisions()
        {
            foreach (Player player in this.players.Values)
            {
                Point left = player.leftHandPosition;
                Point right = player.rightHandPosition;
                needle.Stroke = player.jointsBrush;
                needle2.Stroke = player.jointsBrush;
                needle.X1 = left.X;
                needle.X2 = left.X + 40;
                needle.Y1 = left.Y;
                needle.Y2 = left.Y + 40;
                needle2.X1 = right.X;
                needle2.X2 = right.X - 40;
                needle2.Y1 = right.Y;
                needle2.Y2 = right.Y + 40;

                foreach (Path p in this.pathList)
                {
                    if (p.Data.FillContains(left))
                    {
                        playField.Children.Remove(p);
                        player.AddPoints(10);
                        label.Content = player.GetPoints();
                        break;
                    }
                    if (p.Data.FillContains(right))
                    {
                        playField.Children.Remove(p);
                        player.AddPoints(10);
                        label.Content = player.GetPoints();
                        break;
                    }
                }

            }
        }

        /// <summary>
        /// Funkja przygotowywująca animację na podstawie rozmiaru obiektu canvas
        /// </summary>
        /// <param name="geometry">Aktualny obiekt geometry</param>
        private void PrepareAnimation(EllipseGeometry geometry)
        {
            double space = playField.ActualWidth / baloons;
            j += (int)space + 50;
            if (j > playField.ActualWidth)
            {
                j = Convert.ToInt32(j % playField.ActualWidth);
            }

            geometry.Center = new Point(j, -50);
            up = new PointAnimationUsingPath();
            pBezierSegment = new PolyBezierSegment();
            animationPath = new PathGeometry();
            pFigure = new PathFigure();

            pFigure.StartPoint = new Point(j, -50);
            for (int i = 0; (i < playField.ActualHeight); i += 25)
            {
                int randomSign = random.Next(1);
                if (randomSign == 0)
                {
                    pBezierSegment.Points.Add(new Point(j - random.Next(30), i));
                }
                else if (randomSign == 1)
                {
                    pBezierSegment.Points.Add(new Point(j + random.Next(30), i));
                }
            }
            pBezierSegment.Points.Add(new Point(j, playField.ActualHeight + 50));

            pFigure.Segments.Add(pBezierSegment);
            animationPath.Figures.Add(pFigure);
            up.PathGeometry = animationPath;
            if (timesIndex != times.Count)
            {
                up.BeginTime = TimeSpan.FromSeconds(Convert.ToInt32(times[timesIndex]));
                timesIndex++;
            }

            up.Duration = TimeSpan.FromSeconds(10);
            up.SpeedRatio = 0.5;
            up.RepeatBehavior = RepeatBehavior.Forever;
            animationPath.Freeze();
            geometry.BeginAnimation(EllipseGeometry.CenterProperty, up);
        }




        /// <summary>
        /// Funkcja tworzy obiekty balonów i losowo ustala ich promień, następnie dodaje je do canvas
        /// </summary>
        private void InitializeBaloons()
        {
            ellipseGeometryList = new List<EllipseGeometry>(baloons);
            pathList = new List<Path>(baloons);
            
            for (int i = 0; i < baloons; i++)
            {
                ellipseGeometryList.Add(new EllipseGeometry());
                ellipseGeometryList[i].RadiusX = (40.0 - random.Next(15));
                ellipseGeometryList[i].RadiusY = (40.0 - random.Next(15));
                pathList.Add(new Path());
            }

            for (int i = 0; i < pathList.Count; i++)
            {
                pathList[i].Data = ellipseGeometryList[i];
                RandomBaloonStyle(pathList[i]);
                playField.Children.Add(pathList[i]);
            }
        }

        /// <summary>
        /// Funkcja zmieniająca losowo kolory i przezroczystość balonów
        /// </summary>
        /// <param name="path">"Ścieżka" - czyli obrys elipsy (balona)</param>
        private void RandomBaloonStyle(Path path)
        {
            SolidColorBrush brush = new SolidColorBrush(Color.FromArgb((byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255)));
            path.Fill = brush;
            path.Stroke = brush;
            path.StrokeThickness = 2;
        }

        /// <summary>
        /// Funkcja losująca (bez powtórzeń) liczby z zadanego przedziału ( w celu ustalenia indywidualnych czasów startów animacji )
        /// </summary>
        /// <param name="max"></param>
        /// <returns></returns>
        public ArrayList RandomNumbers(int max)
        {
            ArrayList lstNumbers = new ArrayList();
            Random rndNumber = new Random();


            int number = rndNumber.Next(1, max + 1);
            lstNumbers.Add(number);
            int count = 0;

            do
            {
                number = rndNumber.Next(1, max + 1);

                if (!lstNumbers.Contains(number))
                {
                    lstNumbers.Add(number);
                }


                count++;
            } while (count <= 10 * max);

            return lstNumbers;
        }
    }

}
