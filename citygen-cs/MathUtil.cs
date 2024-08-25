// original comment below
/**
* @author Peter Kelley
* @author pgkelley4@gmail.com
*/

using System;

namespace citygen_cs
{
    public class Random
    {
        private static System.Random random;

        public static void Initialize(int seed)
        {
            random = new System.Random(seed);
        }

        public Random()
        {
        }

        public double NextDouble()
        {
            return random.NextDouble();
        }

        public double Next(int min, int max)
        {
            return random.Next(min, max);
        }

        public int Next()
        {
            return random.Next();
        }
    }

    public static class MathUtil
    {
        const double Epsilon = 0.00000001;

        public static double Clamp(double value, double min, double max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        /**
        * See if two line segments intersect. This uses the
        * vector cross product approach described below:
        * http://stackoverflow.com/a/565282/786339
        *
        * @param {Object} p point object with x and y coordinates
        *  representing the start of the 1st line.
        * @param {Object} p2 point object with x and y coordinates
        *  representing the end of the 1st line.
        * @param {Object} q point object with x and y coordinates
        *  representing the start of the 2nd line.
        * @param {Object} q2 point object with x and y coordinates
        *  representing the end of the 2nd line.
        */
        public static PointT DoLineSegmentsIntersect(Point p, Point p2, Point q, Point q2, bool omitEnds)
        {
            var r = SubtractPoints(p2, p);
            var s = SubtractPoints(q2, q);

            var uNumerator = CrossProduct(SubtractPoints(q, p), r);
            var denominator = CrossProduct(r, s);

            if (uNumerator == 0 && denominator == 0)
            {
                return null;
                // colinear, so do they overlap?
                // return ((q.x - p.x < 0) != (q.x - p2.x < 0) != (q2.x - p.x < 0) != (q2.x - p2.x < 0)) ||
                //   ((q.y - p.y < 0) != (q.y - p2.y < 0) != (q2.y - p.y < 0) != (q2.y - p2.y < 0));
            }

            if (denominator == 0)
            {
                // lines are parallel
                return null;
            }

            var u = uNumerator / denominator;
            var t = CrossProduct(SubtractPoints(q, p), s) / denominator;

            bool doSegmentsIntersect;
            if (!omitEnds)
            {
                doSegmentsIntersect = (t >= 0) && (t <= 1) && (u >= 0) && (u <= 1);
            }
            else
            {
                doSegmentsIntersect = (t > 0.001) && (t < 1 - 0.001) && (u > 0.001) && (u < 1 - 0.001);
            }

            if (doSegmentsIntersect)
            {
                return new PointT { X = p.X + t * r.X, Y = p.Y + t * r.Y, T = t };
            }

            return null;
        }

        public static bool EqualV(Point v1, Point v2)
        {
            var diff = SubtractPoints(v1, v2);
            var length2 = LengthV2(diff);
            return length2 < Epsilon;
        }

        public static Point AddPoints(Point point1, Point point2)
        {
            return new Point
            {
                X = point1.X + point2.X,
                Y = point1.Y + point2.Y
            };
        }

        public static Point SubtractPoints(Point point1, Point point2)
        {
            return new Point
            {
                X = point1.X - point2.X,
                Y = point1.Y - point2.Y
            };
        }

        public static double CrossProduct(Point point1, Point point2)
        {
            return point1.X * point2.Y - point1.Y * point2.X;
        }

        public static double DotProduct(Point point1, Point point2)
        {
            return point1.X * point2.X + point1.Y * point2.Y;
        }

        public static double Length(Point point1, Point point2)
        {
            var v = SubtractPoints(point2, point1);
            return LengthV(v);
        }

        public static double Length2(Point point1, Point point2)
        {
            var v = SubtractPoints(point2, point1);
            return LengthV2(v);
        }

        public static double LengthV(Point v)
        {
            return Math.Sqrt(LengthV2(v));
        }

        public static double LengthV2(Point v)
        {
            return v.X * v.X + v.Y * v.Y;
        }

        public static double AngleBetween(Point v1, Point v2)
        {
            var angleRad = Math.Acos((v1.X * v2.X + v1.Y * v2.Y) / (LengthV(v1) * LengthV(v2)));
            var angleDeg = angleRad * 180 / Math.PI;
            return angleDeg;
        }

        public static int Sign(double x)
        {
            if (x > 0)
            {
                return 1;
            }
            else if (x < 0)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        public static Point FractionBetween(Point v1, Point v2, double fraction)
        {
            var v1ToV2 = SubtractPoints(v2, v1);
            return new Point
            {
                X = v1.X + v1ToV2.X * fraction,
                Y = v1.Y + v1ToV2.Y * fraction
            };
        }

        public static double SinDegrees(double deg)
        {
            return Math.Sin(deg * Math.PI / 180);
        }

        public static double CosDegrees(double deg)
        {
            return Math.Cos(deg * Math.PI / 180);
        }

        public static double AtanDegrees(double val)
        {
            return Math.Atan(val) * 180 / Math.PI;
        }

        public static double RandomRange(double min, double max)
        {
            return new Random().NextDouble() * (max - min) + min;
        }

        public static Point MultVScalar(Point v, double n)
        {
            return new Point
            {
                X = v.X * n,
                Y = v.Y * n
            };
        }

        public static Point DivVScalar(Point v, double n)
        {
            return new Point
            {
                X = v.X / n,
                Y = v.Y / n
            };
        }

        public static DistanceResult OldDistanceToLine(Point p, Point q1, Point q2)
        {
            var qV = SubtractPoints(q2, q1);
            var length = LengthV(qV);
            var qVNorm = DivVScalar(qV, length);

            var eq2 = DotProduct(SubtractPoints(q1, p), qVNorm);
            var qVNormMult = MultVScalar(qVNorm, eq2);
            var vToLine = SubtractPoints(SubtractPoints(q1, p), qVNormMult);

            return new DistanceResult
            {
                Distance = LengthV(vToLine),
                PointOnLine = AddPoints(p, vToLine),
                LineProj = -eq2,
                Length = length
            };
        }

        public static DistanceResult NewDistanceToLine(Point P, Point A, Point B)
        {
            var AP = SubtractPoints(P, A);
            var AB = SubtractPoints(B, A);
            var result = Project(AP, AB);
            var AD = result.Projected;
            var D = AddPoints(A, AD);

            return new DistanceResult
            {
                Distance = Length(D, P),
                PointOnLine = D,
                LineProj = Sign(result.DotProduct) * LengthV(AD),
                Length = LengthV(AB)
            };
        }

        public static DistanceResult2 DistanceToLine(Point P, Point A, Point B)
        {
            var AP = SubtractPoints(P, A);
            var AB = SubtractPoints(B, A);
            var result = Project(AP, AB);
            var AD = result.Projected;
            var D = AddPoints(A, AD);

            return new DistanceResult2
            {
                Distance2 = Length2(D, P),
                PointOnLine = D,
                LineProj2 = Sign(result.DotProduct) * LengthV2(AD),
                Length2 = LengthV2(AB)
            };
        }

        public static ProjectionResult Project(Point v, Point onto)
        {
            var dotProduct = DotProduct(v, onto);
            return new ProjectionResult
            {
                DotProduct = dotProduct,
                Projected = MultVScalar(onto, dotProduct / LengthV2(onto))
            };
        }
    }

    public class PointT
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double T { get; set; }
    }

    public class DistanceResult
    {
        public double Distance { get; set; }
        public Point PointOnLine { get; set; }
        public double LineProj { get; set; }
        public double Length { get; set; }
    }
    public class DistanceResult2
    {
        public double Distance2 { get; set; }
        public Point PointOnLine { get; set; }
        public double LineProj2 { get; set; }
        public double Length2 { get; set; }
    }

    public class ProjectionResult
    {
        public double DotProduct { get; set; }
        public Point Projected { get; set; }
    }
}
