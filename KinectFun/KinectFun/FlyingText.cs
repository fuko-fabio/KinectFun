﻿//------------------------------------------------------------------------------
// <copyright file="FlyingText.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KinectFun
{
    // FlyingText creates text that flys out from a given point, and fades as it gets bigger.
    // NewFlyingText() can be called as often as necessary, and there can be many texts flying out at once.
    public class FlyingText
    {
        private static readonly List<FlyingText> FlyingTexts = new List<FlyingText>();
        private readonly double fontGrow;
        private readonly string text;
        private System.Windows.Point center;
        private System.Windows.Media.Brush brush;
        private double fontSize;
        private double alpha;
        private Label label;

        public FlyingText(string s, double size, System.Windows.Point center)
        {
            this.text = s;
            this.fontSize = Math.Max(1, size);
            this.fontGrow = Math.Sqrt(size) * 0.4;
            this.center = center;
            this.alpha = 1.0;
            this.label = null;
            this.brush = null;
        }

        public static void NewFlyingText(double size, System.Windows.Point center, string s)
        {
            FlyingTexts.Add(new FlyingText(s, size, center));
        }

        public static void Draw(UIElementCollection children)
        {
            for (int i = 0; i < FlyingTexts.Count; i++)
            {
                FlyingText flyout = FlyingTexts[i];
                if (flyout.alpha <= 0)
                {
                    FlyingTexts.Remove(flyout);
                    i--;
                }
            }

            foreach (var flyout in FlyingTexts)
            {
                flyout.Advance();
                children.Add(flyout.label);
            }
        }

        private void Advance()
        {
            this.alpha -= 0.01;
            if (this.alpha < 0)
            {
                this.alpha = 0;
            }

            if (this.brush == null)
            {
                this.brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            }

            if (this.label == null)
            {
                this.label = MakeSimpleLabel(this.text, new Rect(0, 0, 0, 0), this.brush);
            }

            this.brush.Opacity = Math.Pow(this.alpha, 1.5);
            this.label.Foreground = this.brush;
            this.fontSize += this.fontGrow;
            this.label.FontSize = Math.Max(1, this.fontSize);
            Rect renderRect = new Rect(this.label.RenderSize);
            this.label.SetValue(Canvas.LeftProperty, this.center.X - (renderRect.Width / 2));
            this.label.SetValue(Canvas.TopProperty, this.center.Y - (renderRect.Height / 2));
        }

        public static Label MakeSimpleLabel(string text, Rect bounds, System.Windows.Media.Brush brush)
        {
            Label label = new Label { Content = text };
            if (bounds.Width != 0)
            {
                label.SetValue(Canvas.LeftProperty, bounds.Left);
                label.SetValue(Canvas.TopProperty, bounds.Top);
                label.Width = bounds.Width;
                label.Height = bounds.Height;
            }

            label.Foreground = brush;
            label.FontFamily = new System.Windows.Media.FontFamily("Arial");
            label.FontWeight = FontWeight.FromOpenTypeWeight(600);
            label.FontStyle = FontStyles.Normal;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            return label;
        }
    }


}
