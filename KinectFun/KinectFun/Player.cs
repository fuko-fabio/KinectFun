﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Kinect;
using KinectFun.Utils;

namespace KinectFun
{
    class Player
    {
        private readonly int id;
        private long points = 0;
        private int colorId;
        private Rect playerBounds;
        private Point playerCenter;
        private double playerScale;
        private const double boneSize = 0.01;
        private const double headSize = 0.075;
        private const double handSize = 0.03;
        private readonly Dictionary<Bone, BoneData> segments = new Dictionary<Bone, BoneData>();

        public readonly Brush jointsBrush;
        private readonly Brush bonesBrush;

        public Player(int skeletonSlot)
        {
            this.id = skeletonSlot;

            // Generate one of 7 colors for player
            int[] mixR = { 1, 1, 1, 0, 1, 0, 0 };
            int[] mixG = { 1, 1, 0, 1, 0, 1, 0 };
            int[] mixB = { 1, 0, 1, 1, 0, 0, 1 };
            byte[] jointCols = { 245, 200 };
            byte[] boneCols = { 235, 160 };

            int i = colorId;
            colorId = (colorId + 1) % mixR.Count();

            this.jointsBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(jointCols[mixR[i]], jointCols[mixG[i]], jointCols[mixB[i]]));
            this.bonesBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(boneCols[mixR[i]], boneCols[mixG[i]], boneCols[mixB[i]]));
            this.LastUpdated = DateTime.Now;
        }

        public bool IsAlive { get; set; }
        public DateTime LastUpdated { get; set; }
        public Point leftHandPosition { get; set; }
        public Point rightHandPosition { get; set; }

        public void AddPoints(int points)
        {
            this.points += points;
        }

        public long GetPoints()
        {
            return this.points;
        }

        public int GetId()
        {
            return this.id;
        }

        public void SetBounds(Rect r)
        {
            this.playerBounds = r;
            this.playerCenter.X = (this.playerBounds.Left + this.playerBounds.Right) / 2;
            this.playerCenter.Y = (this.playerBounds.Top + this.playerBounds.Bottom) / 2;
            this.playerScale = Math.Min(this.playerBounds.Width, this.playerBounds.Height / 2);
        }

        public void UpdateJointPosition(Microsoft.Kinect.JointCollection joints, JointType j)
        {
            if (j == JointType.HandLeft)
            {
                leftHandPosition = new Point((joints[j].Position.X * this.playerScale) + this.playerCenter.X , (((joints[j].Position.Y * -1) * this.playerScale) + this.playerCenter.Y));
            }
            if (j == JointType.HandRight)
            {
                rightHandPosition = new Point((joints[j].Position.X * this.playerScale) + this.playerCenter.X, (((joints[j].Position.Y * -1) * this.playerScale) + this.playerCenter.Y));
            }
            var seg = new Segment(
                (joints[j].Position.X * this.playerScale) + this.playerCenter.X,
                this.playerCenter.Y - (joints[j].Position.Y * this.playerScale)) { radius = this.playerBounds.Height * ((j == JointType.Head) ? headSize : handSize) / 2 };
            this.UpdateSegmentPosition(j, j, seg);
        }

        private void UpdateSegmentPosition(JointType j1, JointType j2, Segment seg)
        {
            var bone = new Bone(j1, j2);
            if (this.segments.ContainsKey(bone))
            {
                BoneData data = this.segments[bone];
                data.UpdateSegment(seg);
                this.segments[bone] = data;
            }
            else
            {
                this.segments.Add(bone, new BoneData(seg));
            }
        }

        public void UpdateBonePosition(Microsoft.Kinect.JointCollection joints, JointType j1, JointType j2)
        {
            var seg = new Segment(
                (joints[j1].Position.X * this.playerScale) + this.playerCenter.X,
                this.playerCenter.Y - (joints[j1].Position.Y * this.playerScale),
                (joints[j2].Position.X * this.playerScale) + this.playerCenter.X,
                this.playerCenter.Y - (joints[j2].Position.Y * this.playerScale)) { radius = Math.Max(3.0, this.playerBounds.Height * boneSize) / 2 };
            this.UpdateSegmentPosition(j1, j2, seg);
        }

        public void Draw(UIElementCollection children)
        {
            if (!this.IsAlive)
            {
                return;
            }

            // Draw all bones first, then circles (head and hands).
            DateTime cur = DateTime.Now;
            foreach (var segment in this.segments)
            {
                Segment seg = segment.Value.GetEstimatedSegment(cur);
                if (!seg.IsCircle())
                {
                    var line = new Line
                    {
                        StrokeThickness = seg.radius * 2,
                        X1 = seg.x1,
                        Y1 = seg.y1,
                        X2 = seg.x2,
                        Y2 = seg.y2,
                        Stroke = this.bonesBrush,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeStartLineCap = PenLineCap.Round
                    };
                    children.Add(line);
                }
            }

            foreach (var segment in this.segments)
            {
                Segment seg = segment.Value.GetEstimatedSegment(cur);
                if (seg.IsCircle())
                {
                    var circle = new Ellipse { Width = seg.radius * 2, Height = seg.radius * 2 };
                    circle.SetValue(Canvas.LeftProperty, seg.x1 - seg.radius);
                    circle.SetValue(Canvas.TopProperty, seg.y1 - seg.radius);
                    circle.Stroke = this.jointsBrush;
                    circle.StrokeThickness = 1;
                    circle.Fill = this.bonesBrush;
                    children.Add(circle);
                }
            }

            // Remove unused players after 1/2 second.
            if (DateTime.Now.Subtract(this.LastUpdated).TotalMilliseconds > 500)
            {
                this.IsAlive = false;
            }
        }
    }
}
