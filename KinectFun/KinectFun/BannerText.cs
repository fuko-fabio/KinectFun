namespace KinectFun
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Microsoft.Kinect;

    // BannerText generates a scrolling or still banner of text (along the bottom of the screen).
    // Only one banner exists at a time.  Calling NewBanner() will erase the old one and start the new one.
    public class BannerText
    {
        private readonly System.Windows.Media.Color color;
        private readonly string text;
        private readonly bool doScroll;
        private static BannerText myBannerText;
        private System.Windows.Media.Brush brush;
        private Label label;
        private Rect boundsRect;
        private Rect renderedRect;
        private double offset;

        public BannerText(string s, Rect rect, bool scroll, System.Windows.Media.Color col)
        {
            this.text = s;
            this.boundsRect = rect;
            this.doScroll = scroll;
            this.brush = null;
            this.label = null;
            this.color = col;
            this.offset = this.doScroll ? 1.0 : 0.0;
        }

        public static void NewBanner(string s, Rect rect, bool scroll, System.Windows.Media.Color col)
        {
            myBannerText = (s != null) ? new BannerText(s, rect, scroll, col) : null;
        }

        public static void UpdateBounds(Rect rect)
        {
            if (myBannerText == null)
            {
                return;
            }

            myBannerText.boundsRect = rect;
            myBannerText.label = null;
        }

        public static void Draw(UIElementCollection children)
        {
            if (myBannerText == null)
            {
                return;
            }

            Label text = myBannerText.GetLabel();
            if (text == null)
            {
                myBannerText = null;
                return;
            }

            children.Add(text);
        }

        private Label GetLabel()
        {
            if (this.brush == null)
            {
                this.brush = new SolidColorBrush(this.color);
            }

            if (this.label == null)
            {
                this.label = MakeSimpleLabel(this.text, this.boundsRect, this.brush);
                if (this.doScroll)
                {
                    this.label.FontSize = Math.Max(20, this.boundsRect.Height / 30);
                    this.label.Width = 10000;
                }
                else
                {
                    this.label.FontSize = Math.Min(
                        Math.Max(10, this.boundsRect.Width * 2 / this.text.Length), Math.Max(10, this.boundsRect.Height / 20));
                }

                this.label.VerticalContentAlignment = VerticalAlignment.Bottom;
                this.label.HorizontalContentAlignment = this.doScroll
                                                            ? HorizontalAlignment.Left
                                                            : HorizontalAlignment.Center;
                this.label.SetValue(Canvas.LeftProperty, this.offset * this.boundsRect.Width);
            }

            this.renderedRect = new Rect(this.label.RenderSize);

            if (this.doScroll)
            {
                this.offset -= 0.0015;
                if (this.offset * this.boundsRect.Width < this.boundsRect.Left - 10000)
                {
                    return null;
                }

                this.label.SetValue(Canvas.LeftProperty, (this.offset * this.boundsRect.Width) + this.boundsRect.Left);
            }

            return this.label;
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
