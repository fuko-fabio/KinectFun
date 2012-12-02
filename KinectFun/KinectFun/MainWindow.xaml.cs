namespace KinectFun
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Samples.Kinect.WpfViewers;
    using System.Runtime.InteropServices;
    using System.Threading;

    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty KinectSensorManagerProperty =
            DependencyProperty.Register(
                "KinectSensorManager",
                typeof(KinectSensorManager),
                typeof(MainWindow),
                new PropertyMetadata(null));

        public KinectSensorManager KinectSensorManager
        {
            get { return (KinectSensorManager)GetValue(KinectSensorManagerProperty); }
            set { SetValue(KinectSensorManagerProperty, value); }
        }

        private readonly KinectSensorChooser sensorChooser = new KinectSensorChooser();
        private Skeleton[] skeletonData;
        private Rect playerBounds;
        private Rect screenRect;

        private GameLogic gameLogic;

        private const int TimerResolution = 2;  // ms
        // Since the timer resolution defaults to about 10ms precisely, we need to
        // increase the resolution to get framerates above between 50fps with any
        // consistency.
        [DllImport("Winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern int TimeBeginPeriod(uint period);

        public MainWindow()
        {
            this.KinectSensorManager = new KinectSensorManager();
            this.KinectSensorManager.KinectSensorChanged += this.KinectSensorChanged;
            this.DataContext = this.KinectSensorManager;

            InitializeComponent();

            this.SensorChooserUI.KinectSensorChooser = sensorChooser;
            sensorChooser.Start();

            // Bind the KinectSensor from the sensorChooser to the KinectSensor on the KinectSensorManager
            var kinectSensorBinding = new Binding("Kinect") { Source = this.sensorChooser };
            BindingOperations.SetBinding(this.KinectSensorManager, KinectSensorManager.KinectSensorProperty, kinectSensorBinding);

            this.RestoreWindowState();
        }

        private void RestoreWindowState()
        {
            // Restore window state to that last used
            Rect bounds = Properties.Settings.Default.PreviousWindowPosition;
            if (bounds.Right != bounds.Left)
            {
                this.Top = bounds.Top;
                this.Left = bounds.Left;
                this.Height = bounds.Height;
                this.Width = bounds.Width;
            }
            this.WindowState = (WindowState)Properties.Settings.Default.WindowState;
        }

        private void WindowLoaded(object sender, EventArgs e)
        {
            this.playField.ClipToBounds = true;
            this.UpdatePlayFieldSize();
            this.gameLogic = new GameLogic(this.Dispatcher, this.screenRect, this.playField);

            //Game thread. Move this part to another method if needed
            TimeBeginPeriod(TimerResolution);
            var myGameThread = new Thread(this.gameLogic.GameThread);
            myGameThread.SetApartmentState(ApartmentState.STA);
            myGameThread.Start();

            FlyingText.NewFlyingText(this.screenRect.Width / 30, new Point(this.screenRect.Width / 2, this.screenRect.Height / 2), "Kinect Fun!");
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            sensorChooser.Stop();
            Properties.Settings.Default.PreviousWindowPosition = this.RestoreBounds;
            Properties.Settings.Default.WindowState = (int)this.WindowState;
            Properties.Settings.Default.Save();
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            this.KinectSensorManager.KinectSensor = null;
        }

        private void KinectSensorChanged(object sender, KinectSensorManagerEventArgs<KinectSensor> args)
        {
            if (args.OldValue != null)
            {
                this.UninitializeKinectServices(args.OldValue);
            }
            if (args.NewValue != null)
            {
                this.InitializeKinectServices(this.KinectSensorManager, args.NewValue);
            }
        }

        private void InitializeKinectServices(KinectSensorManager kinectSensorManager, KinectSensor sensor)
        {
            kinectSensorManager.ColorFormat = ColorImageFormat.RgbResolution640x480Fps30;
            kinectSensorManager.ColorStreamEnabled = true;

            kinectSensorManager.TransformSmoothParameters = new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.5f,
                Prediction = 0.5f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            };
            kinectSensorManager.SkeletonStreamEnabled = true;
            kinectSensorManager.KinectSensorEnabled = true;
        }

        private void UninitializeKinectServices(KinectSensor sensor)
        {
        }

        private void PlayFieldSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdatePlayFieldSize();
        }

        private void UpdatePlayFieldSize()
        {
            this.screenRect.X = 0;
            this.screenRect.Y = 0;
            this.screenRect.Width = this.playField.ActualWidth;
            this.screenRect.Height = this.playField.ActualHeight;

            BannerText.UpdateBounds(this.screenRect);

            this.playerBounds.X = 0;
            this.playerBounds.Width = this.playField.ActualWidth;
            this.playerBounds.Y = this.playField.ActualHeight * 0.2;
            this.playerBounds.Height = this.playField.ActualHeight * 0.75;

            if (this.gameLogic != null)
            {
                foreach (var player in this.gameLogic.players)
                {
                    player.Value.SetBounds(this.playerBounds);
                }
            }
        }

        private void SkeletonsReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    int skeletonSlot = 0;

                    if ((this.skeletonData == null) || (this.skeletonData.Length != skeletonFrame.SkeletonArrayLength))
                    {
                        this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    skeletonFrame.CopySkeletonDataTo(this.skeletonData);

                    foreach (Skeleton skeleton in this.skeletonData)
                    {
                        if (SkeletonTrackingState.Tracked == skeleton.TrackingState)
                        {
                            Player player;
                            if (this.gameLogic.players.ContainsKey(skeletonSlot))
                            {
                                player = this.gameLogic.players[skeletonSlot];
                            }
                            else
                            {
                                player = new Player(skeletonSlot);
                                player.SetBounds(this.playerBounds);
                                this.gameLogic.players.Add(skeletonSlot, player);
                            }

                            player.LastUpdated = DateTime.Now;

                            // Update player's bone and joint positions
                            if (skeleton.Joints.Count > 0)
                            {
                                player.IsAlive = true;

                                // Head, hands, feet (hit testing happens in order here)
                                player.UpdateJointPosition(skeleton.Joints, JointType.Head);
                                player.UpdateJointPosition(skeleton.Joints, JointType.HandLeft);
                                player.UpdateJointPosition(skeleton.Joints, JointType.HandRight);
                                player.UpdateJointPosition(skeleton.Joints, JointType.FootLeft);
                                player.UpdateJointPosition(skeleton.Joints, JointType.FootRight);

                                // Hands and arms
                                player.UpdateBonePosition(skeleton.Joints, JointType.HandRight, JointType.WristRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.WristRight, JointType.ElbowRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ElbowRight, JointType.ShoulderRight);

                                player.UpdateBonePosition(skeleton.Joints, JointType.HandLeft, JointType.WristLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.WristLeft, JointType.ElbowLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ElbowLeft, JointType.ShoulderLeft);

                                // Head and Shoulders
                                player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderCenter, JointType.Head);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderLeft, JointType.ShoulderCenter);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderCenter, JointType.ShoulderRight);

                                // Legs
                                player.UpdateBonePosition(skeleton.Joints, JointType.HipLeft, JointType.KneeLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.KneeLeft, JointType.AnkleLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.AnkleLeft, JointType.FootLeft);

                                player.UpdateBonePosition(skeleton.Joints, JointType.HipRight, JointType.KneeRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.KneeRight, JointType.AnkleRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.AnkleRight, JointType.FootRight);

                                player.UpdateBonePosition(skeleton.Joints, JointType.HipLeft, JointType.HipCenter);
                                player.UpdateBonePosition(skeleton.Joints, JointType.HipCenter, JointType.HipRight);

                                // Spine
                                player.UpdateBonePosition(skeleton.Joints, JointType.HipCenter, JointType.ShoulderCenter);
                            }
                        }
                        skeletonSlot++;
                    }
                }
            }
        }
    }
}
