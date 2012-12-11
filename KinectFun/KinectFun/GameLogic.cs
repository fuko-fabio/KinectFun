using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace KinectFun
{
    public enum GameMode
    {
        Off = 0,
        OnePlayer = 1,
        TwoPlayer = 2
    }

    class GameLogic
    {
        private GameMode gameMode;
        private Rect rect;
        private bool runningGameThread;
        private DateTime predNextFrame;
        private double targetFramerate = 25;
        private double actualFrameTime;
        private DateTime lastFrameDrawn;
        private int frameCount;
        private const double MinFramerate = 15;
        private double TimerResolution = 2; //ms
        private Dispatcher dispatcher;
        private Canvas playField;
        public bool gameIsStarted { get; set; }
        public readonly Dictionary<int, Player> players = new Dictionary<int, Player>();

        public GameLogic(Dispatcher dispatcher, Rect rect, Canvas playField)
        {
            this.dispatcher = dispatcher;
            this.rect = rect;
            this.playField = playField;
            gameIsStarted = false;
        }
        public int playersAlive { get; set; }


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

            if (alive != this.playersAlive)
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

                if (this.playersAlive == 0)
                {
                    BannerText.NewBanner(
                        Properties.Resources.Vocabulary,
                        this.rect,
                        true,
                        Color.FromArgb(200, 255, 255, 255));
                    this.gameIsStarted = false;
                }

                this.playersAlive = alive;
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

        private void HandleGameTimer(int param)
        {
            playField.Children.Clear();

            if (gameIsStarted)
            {
                //TODO draw game scene and manage players actions
            }

            foreach (var player in this.players)
            {
                player.Value.Draw(playField.Children);
            }

            BannerText.Draw(playField.Children);
            FlyingText.Draw(playField.Children);

            this.CheckPlayers();
        }
    }
}
