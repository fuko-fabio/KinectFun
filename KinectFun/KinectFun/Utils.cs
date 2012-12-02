using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectFun.Utils
{
    public struct Bone
    {
        public JointType joint1;
        public JointType joint2;

        public Bone(JointType joint1, JointType joint2)
        {
            this.joint1 = joint1;
            this.joint2 = joint2;
        }
    }

    public struct Segment
    {
        public double x1;
        public double y1;
        public double x2;
        public double y2;
        public double radius;

        public Segment(double x, double y)
        {
            this.radius = 1;
            this.x1 = this.x2 = x;
            this.y1 = this.y2 = y;
        }

        public Segment(double x1, double y1, double x2, double y2)
        {
            this.radius = 1;
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
        }

        public bool IsCircle()
        {
            return (this.x1 == this.x2) && (this.y1 == this.y2);
        }
    }

    public struct BoneData
    {
        public Segment segment;
        public Segment lastSegment;
        public double xVelocity1;
        public double yVelocity1;
        public double xVelocity2;
        public double yVelocity2;
        public DateTime timeLastUpdated;

        private const double smoothing = 0.8;

        public BoneData(Segment s)
        {
            this.segment = this.lastSegment = s;
            this.xVelocity1 = this.yVelocity1 = 0;
            this.xVelocity2 = this.yVelocity2 = 0;
            this.timeLastUpdated = DateTime.Now;
        }

        // Update the segment's position and compute a smoothed velocity for the circle or the
        // endpoints of the segment based on  the time it took it to move from the last position
        // to the current one.  The velocity is in pixels per second.
        public void UpdateSegment(Segment s)
        {
            this.lastSegment = this.segment;
            this.segment = s;

            DateTime cur = DateTime.Now;
            double fMs = cur.Subtract(this.timeLastUpdated).TotalMilliseconds;
            if (fMs < 10.0)
            {
                fMs = 10.0;
            }

            double fps = 1000.0 / fMs;
            this.timeLastUpdated = cur;

            if (this.segment.IsCircle())
            {
                this.xVelocity1 = (this.xVelocity1 * smoothing) + ((1.0 - smoothing) * (this.segment.x1 - this.lastSegment.x1) * fps);
                this.yVelocity1 = (this.yVelocity1 * smoothing) + ((1.0 - smoothing) * (this.segment.y1 - this.lastSegment.y1) * fps);
            }
            else
            {
                this.xVelocity1 = (this.xVelocity1 * smoothing) + ((1.0 - smoothing) * (this.segment.x1 - this.lastSegment.x1) * fps);
                this.yVelocity1 = (this.yVelocity1 * smoothing) + ((1.0 - smoothing) * (this.segment.y1 - this.lastSegment.y1) * fps);
                this.xVelocity2 = (this.xVelocity2 * smoothing) + ((1.0 - smoothing) * (this.segment.x2 - this.lastSegment.x2) * fps);
                this.yVelocity2 = (this.yVelocity2 * smoothing) + ((1.0 - smoothing) * (this.segment.y2 - this.lastSegment.y2) * fps);
            }
        }

        // Using the velocity calculated above, estimate where the segment is right now.
        public Segment GetEstimatedSegment(DateTime cur)
        {
            Segment estimate = this.segment;
            double fMs = cur.Subtract(this.timeLastUpdated).TotalMilliseconds;
            estimate.x1 += fMs * this.xVelocity1 / 1000.0;
            estimate.y1 += fMs * this.yVelocity1 / 1000.0;
            if (this.segment.IsCircle())
            {
                estimate.x2 = estimate.x1;
                estimate.y2 = estimate.y1;
            }
            else
            {
                estimate.x2 += fMs * this.xVelocity2 / 1000.0;
                estimate.y2 += fMs * this.yVelocity2 / 1000.0;
            }

            return estimate;
        }
    }
}
