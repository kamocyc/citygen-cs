// original author: tmwhere.com

using System;
using System.Collections.Generic;
using System.Linq;

namespace citygen_cs
{
    public class CollisionObjectProperties
    {
        public Point Start { get; set; }
        public Point End { get; set; }
        public double Width { get; set; }
        public Point[] Corners { get; set; }
        public double Radius { get; set; }
        public Point Center { get; set; }
    }

    public static class _
    {
        public static T Min<T>(IEnumerable<T> list, Func<T, double> func) where T : class
        {
            return list.Aggregate((i1, i2) => func(i1) < func(i2) ? i1 : i2);
        }

        public static T Max<T>(IEnumerable<T> list, Func<T, double> func) where T : class
        {
            return list.Aggregate((i1, i2) => func(i1) > func(i2) ? i1 : i2);
        }
    }

    public enum CollisionType
    {
        RECT,
        LINE,
        CIRCLE
    }

    public class CollisionObject
    {
        private object o;
        private CollisionType collisionType;
        private CollisionObjectProperties collisionProperties;
        private int collisionRevision;
        private int? limitsRevision;
        private Rectangle cachedLimits;

        public CollisionObject(object o, CollisionType collisionType, CollisionObjectProperties collisionProperties)
        {
            this.o = o;
            this.collisionType = collisionType;
            this.collisionProperties = collisionProperties;
            this.collisionRevision = 0;
            this.limitsRevision = null;
            this.cachedLimits = null;
        }

        public void UpdateCollisionProperties(CollisionObjectProperties props)
        {
            collisionRevision++;
            collisionProperties = new CollisionObjectProperties
            {
                Start = props.Start ?? collisionProperties.Start,
                End = props.End ?? collisionProperties.End,
                Corners = props.Corners ?? collisionProperties.Corners,
            };
        }

        public Rectangle Limits()
        {
            if (collisionRevision != limitsRevision)
            {
                limitsRevision = collisionRevision;
                switch (collisionType)
                {
                    case CollisionType.RECT:
                        var minX = _.Min(collisionProperties.Corners, o => o.X).X;
                        var minY = _.Min(collisionProperties.Corners, o => o.Y).Y;
                        cachedLimits = new Rectangle()
                        {
                            X = minX,
                            Y = minY,
                            Width = _.Max(collisionProperties.Corners, o => o.X).X - minX,
                            Height = _.Max(collisionProperties.Corners, o => o.Y).Y - minY,
                            O = o,
                        };
                        break;
                    case CollisionType.LINE:
                        cachedLimits = new Rectangle()
                        {
                            X = System.Math.Min(collisionProperties.Start.X, collisionProperties.End.X),
                            Y = System.Math.Min(collisionProperties.Start.Y, collisionProperties.End.Y),
                            Width = System.Math.Abs(collisionProperties.Start.X - collisionProperties.End.X),
                            Height = System.Math.Abs(collisionProperties.Start.Y - collisionProperties.End.Y),
                            O = o,
                        };
                        break;
                    case CollisionType.CIRCLE:
                        cachedLimits = new Rectangle()
                        {
                            X = collisionProperties.Center.X - collisionProperties.Radius,
                            Y = collisionProperties.Center.Y - collisionProperties.Radius,
                            Width = collisionProperties.Radius * 2,
                            Height = collisionProperties.Radius * 2,
                            O = o,
                        };
                        break;
                }
            }
            return cachedLimits;
        }

        public Point Collide(CollisionObject other)
        {
            var objLimits = Limits();
            var otherLimits = other.Limits();
            if (objLimits != null && otherLimits != null &&
                (objLimits.X + objLimits.Width < otherLimits.X || otherLimits.X + otherLimits.Width < objLimits.X) &&
                (objLimits.Y + objLimits.Height < otherLimits.Y || otherLimits.Y + otherLimits.Height < objLimits.Y))
            {
                return null;
            }

            switch (collisionType)
            {
                case CollisionType.CIRCLE:
                    switch (other.collisionType)
                    {
                        case CollisionType.RECT:
                            return RectCircleCollision(other.collisionProperties, collisionProperties);
                    }
                    break;
                case CollisionType.RECT:
                    switch (other.collisionType)
                    {
                        case CollisionType.RECT:
                            return RectRectIntersection(collisionProperties, other.collisionProperties);
                        case CollisionType.LINE:
                            return RectRectIntersection(collisionProperties, RectPropsFromLine(other.collisionProperties));
                        case CollisionType.CIRCLE:
                            return RectCircleCollision(collisionProperties, other.collisionProperties);
                    }
                    break;
                case CollisionType.LINE:
                    switch (other.collisionType)
                    {
                        case CollisionType.RECT:
                            return RectRectIntersection(RectPropsFromLine(collisionProperties), other.collisionProperties);
                        case CollisionType.LINE:
                            return RectRectIntersection(RectPropsFromLine(collisionProperties), RectPropsFromLine(other.collisionProperties));
                    }
                    break;
            }
            return null;
        }

        private static Point RectCircleCollision(CollisionObjectProperties rectProps, CollisionObjectProperties circleProps)
        {
            var corners = rectProps.Corners;

            // check for corner intersections with circle
            for (int i = 0; i < corners.Length; i++)
            {
                if (MathUtil.Length2(corners[i], circleProps.Center) <= circleProps.Radius * circleProps.Radius)
                {
                    throw new Exception("return true");
                }
            }

            // check for edge intersections with circle
            // from http://stackoverflow.com/a/1079478
            for (int i = 0; i < corners.Length; i++)
            {
                var start = corners[i];
                var end = corners[(i + 1) % corners.Length];
                var result = MathUtil.DistanceToLine(circleProps.Center, start, end);
                if (result.LineProj2 > 0 && result.LineProj2 < result.Length2 && result.Distance2 <= circleProps.Radius * circleProps.Radius)
                {
                    throw new Exception("return true");
                }
            }

            // check that circle is not enclosed by rectangle
            var axes = new[]
            {
                MathUtil.SubtractPoints(corners[3], corners[0]),
                MathUtil.SubtractPoints(corners[3], corners[2])
            };

            var projections = new[]
            {
                MathUtil.Project(MathUtil.SubtractPoints(circleProps.Center, corners[0]), axes[0]),
                MathUtil.Project(MathUtil.SubtractPoints(circleProps.Center, corners[2]), axes[1])
            };

            if (projections[0].DotProduct < 0 || MathUtil.LengthV2(projections[0].Projected) > MathUtil.LengthV2(axes[0]) ||
                projections[1].DotProduct < 0 || MathUtil.LengthV2(projections[1].Projected) > MathUtil.LengthV2(axes[1]))
            {
                return null;
            }

            throw new Exception("return true");
        }

        private static CollisionObjectProperties RectPropsFromLine(CollisionObjectProperties lineProps)
        {
            var dir = MathUtil.SubtractPoints(lineProps.End, lineProps.Start);
            var perpDir = new Point { X = -dir.Y, Y = dir.X };
            var halfWidthPerpDir = MathUtil.MultVScalar(perpDir, 0.5 * lineProps.Width / MathUtil.LengthV(perpDir));
            var props = new CollisionObjectProperties
            {
                Corners = new Point[]
                {
                    MathUtil.AddPoints(lineProps.Start, halfWidthPerpDir),
                    MathUtil.SubtractPoints(lineProps.Start, halfWidthPerpDir),
                    MathUtil.SubtractPoints(lineProps.End, halfWidthPerpDir),
                    MathUtil.AddPoints(lineProps.End, halfWidthPerpDir)
                }
            };
            return props;
        }

        public static Point RectRectIntersection(CollisionObjectProperties rectAProps, CollisionObjectProperties rectBProps)
        {
            var cA = rectAProps.Corners;
            var cB = rectBProps.Corners;
            // generate axes
            var axes = new[] {
                MathUtil.SubtractPoints(cA[3], cA[0]),
                MathUtil.SubtractPoints(cA[3], cA[2]),
                MathUtil.SubtractPoints(cB[0], cB[1]),
                MathUtil.SubtractPoints(cB[0], cB[3])
            };

            // list used to find axis with the minimum overlap
            // that axis is used as the response translation vector
            var axisOverlaps = new List<Point>();

            foreach (var axis in axes)
            {
                // project rectangle points to axis
                var projectedVectorsA = new List<Point>();
                var projectedVectorsB = new List<Point>();

                foreach (var corner in cA)
                    projectedVectorsA.Add(MathUtil.Project(corner, axis).Projected);
                foreach (var corner in cB)
                    projectedVectorsB.Add(MathUtil.Project(corner, axis).Projected);

                // calculate relative positions of rectangles on axis
                var positionsOnAxisA = new List<double>();
                var positionsOnAxisB = new List<double>();

                foreach (var v in projectedVectorsA)
                    positionsOnAxisA.Add(MathUtil.DotProduct(v, axis));
                foreach (var v in projectedVectorsB)
                    positionsOnAxisB.Add(MathUtil.DotProduct(v, axis));

                var (maxA, maxA_i) = Utility.ExtendedMax(positionsOnAxisA);
                var (minA, minA_i) = Utility.ExtendedMin(positionsOnAxisA);
                var (maxB, maxB_i) = Utility.ExtendedMax(positionsOnAxisB);
                var (minB, minB_i) = Utility.ExtendedMin(positionsOnAxisB);
                // if the rectangles don't overlap on at least one axis
                // they are not colliding
                if (maxA < minB || maxB < minA)
                {
                    return null;
                }
                else
                {
                    // calculate the overlap between the rectangles on this axis
                    var diff1 = MathUtil.SubtractPoints(projectedVectorsA[maxA_i], projectedVectorsB[minB_i]);
                    var diff2 = MathUtil.SubtractPoints(projectedVectorsB[maxB_i], projectedVectorsA[minA_i]);

                    if (MathUtil.LengthV2(diff1) < MathUtil.LengthV2(diff2))
                    {
                        axisOverlaps.Add(diff1);
                    }
                    else
                    {
                        // the rectangles overlap on the other side
                        // invert the vector so that it will push out of the collision
                        axisOverlaps.Add(MathUtil.MultVScalar(diff2, -1));
                    }

                }
            }

            // find axis with the minimum overlap
            var minVector = _.Min(axisOverlaps, (v) =>
              MathUtil.LengthV2(v)
            );

            // return displacement required to pull rectA from collision
            return MathUtil.MultVScalar(minVector, -1);
        }
    }
}
