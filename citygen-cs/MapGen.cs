// original author: tmwhere.com

using System;
using System.Collections.Generic;
using System.Linq;
using static citygen_cs.Segment;

namespace citygen_cs
{
    public class RoadRepresentation
    {
        public Point Start { get; set; }
        public Point End { get; set; }
        private CollisionObject Collider;
        private int RoadRevision;

        public RoadRepresentation(Point start, Point end, CollisionObject collider)
        {
            Start = start;
            End = end;
            this.Collider = collider;
            RoadRevision = 0;
        }

        public void SetStart(Point val)
        {
            Start = val;
            Collider.UpdateCollisionProperties(new CollisionObjectProperties { Start = Start });
            RoadRevision++;
        }

        public void SetEnd(Point val)
        {
            End = val;
            Collider.UpdateCollisionProperties(new CollisionObjectProperties { End = End });
            RoadRevision++;
        }
    }

    public class Links
    {
        public List<Segment> B { get; set; }
        public List<Segment> F { get; set; }
    }

    public class SegmentProperties
    {
        public bool Highway { get; set; }
        public int Color { get; set; }
        public bool Severed { get; set; }
        public double? t { get; set; } = null;
    }

    public class Segment
    {
        public enum End
        {
            START = 1,
            END = 2
        }

        public double Width { get; private set; }
        public CollisionObject Collider { get; private set; }
        public int RoadRevision { get; private set; }
        public int? DirRevision { get; private set; }
        public int? LengthRevision { get; private set; }
        public double CachedDir { get; private set; }
        public double CachedLength { get; private set; }
        public RoadRepresentation R { get; private set; }
        public double T { get; set; }
        public SegmentProperties Q { get; private set; }
        public Links Links { get; private set; }
        public List<object> Users { get; private set; }
        public int MaxSpeed { get; private set; }
        public int Capacity { get; private set; }
        public int Id { get; set; }
        public Action SetupBranchLinks { get; set; } = null;

        public Segment(Point start, Point end, double t, SegmentProperties q)
        {
            start = start.Clone();
            end = end.Clone();
            q = q ?? new SegmentProperties();

            Width = q.Highway ? Config.MapGeneration.HIGHWAY_SEGMENT_WIDTH : Config.MapGeneration.DEFAULT_SEGMENT_WIDTH;
            Collider = new CollisionObject(this, CollisionType.LINE, new CollisionObjectProperties { Start = start, End = end, Width = Width });

            RoadRevision = 0;
            DirRevision = null;
            LengthRevision = null;
            CachedDir = 0;
            CachedLength = 0;

            R = new RoadRepresentation(start, end, Collider);

            T = t;
            Q = q;
            Links = new Links { B = new List<Segment>(), F = new List<Segment>() };
            Users = new List<object>();

            if (q.Highway)
            {
                MaxSpeed = 1200;
                Capacity = 12;
            }
            else
            {
                MaxSpeed = 800;
                Capacity = 6;
            }
        }

        public double CurrentSpeed()
        {
            // subtract 1 from users length so that a single user can go full speed
            return Math.Min(Config.GameLogic.MinSpeedProportion, 1 - Math.Max(0, Users.Count - 1) / (double)Capacity) * MaxSpeed;
        }

        // clockwise direction
        public double Dir()
        {
            if (DirRevision != RoadRevision)
            {
                DirRevision = RoadRevision;
                Point vector = MathUtil.SubtractPoints(R.End, R.Start);
                CachedDir = -1 * MathUtil.Sign(MathUtil.CrossProduct(new Point(0, 1), vector)) * MathUtil.AngleBetween(new Point(0, 1), vector);
            }
            return CachedDir;
        }

        public double Length()
        {
            if (LengthRevision != RoadRevision)
            {
                LengthRevision = RoadRevision;
                CachedLength = MathUtil.Length(R.Start, R.End);
            }
            return CachedLength;
        }

        public void DebugLinks()
        {
            Q.Color = 0x00FF00;
            foreach (var backwards in Links.B)
            {
                backwards.Q.Color = 0xFF0000;
            }
            foreach (var forwards in Links.F)
            {
                forwards.Q.Color = 0x0000FF;
            }
        }

        public bool StartIsBackwards()
        {
            if (Links.B.Count > 0)
            {
                return MathUtil.EqualV(Links.B[0].R.Start, R.Start) || MathUtil.EqualV(Links.B[0].R.End, R.Start);
            }
            else
            {
                return MathUtil.EqualV(Links.F[0].R.Start, R.End) || MathUtil.EqualV(Links.F[0].R.End, R.End);
            }
        }
        public double Cost()
        {
            return Length() / CurrentSpeed();
        }

        public double CostTo(Segment other, double? fromFraction)
        {
            var segmentEnd = EndContaining(other);
            return Cost() * (fromFraction.HasValue ?
                (segmentEnd == Segment.End.START ? fromFraction.Value : 1 - fromFraction.Value) :
                0.5);
        }

        public List<Segment> Neighbours()
        {
            return Links.F.Concat(Links.B).ToList();
        }

        public Segment.End EndContaining(Segment segment)
        {
            bool startBackwards = StartIsBackwards();
            if (Links.B.Contains(segment))
            {
                return startBackwards ? Segment.End.START : Segment.End.END;
            }
            else if (Links.F.Contains(segment))
            {
                return startBackwards ? Segment.End.END : Segment.End.START;
            }
            else
            {
                throw new Exception("EndContaining: Segment not found in Links");
            }
        }

        public List<Segment> LinksForEndContaining(Segment segment)
        {
            if (Links.B.Contains(segment))
            {
                return Links.B;
            }
            else if (Links.F.Contains(segment))
            {
                return Links.F;
            }
            else
            {
                return null;
            }
        }

        public void Split(Point point, Segment segment, List<Segment> segmentList, QuadTree qTree)
        {
            bool startIsBackwards = StartIsBackwards();

            Segment splitPart = SegmentFactory.FromExisting(this);
            AddSegment(splitPart, segmentList, qTree);
            splitPart.R.SetEnd(point);
            this.R.SetStart(point);

            // Links are not copied using the preceding factory method
            // Copy link array for the split part, keeping references the same
            splitPart.Links.B = new List<Segment>(this.Links.B);
            splitPart.Links.F = new List<Segment>(this.Links.F);

            // Work out which Links correspond to which end of the split segment
            Segment firstSplit, secondSplit;
            List<Segment> fixLinks;
            if (startIsBackwards)
            {
                firstSplit = splitPart;
                secondSplit = this;
                fixLinks = splitPart.Links.B;
            }
            else
            {
                firstSplit = this;
                secondSplit = splitPart;
                fixLinks = splitPart.Links.F;
            }

            foreach (var link in fixLinks)
            {
                int index = link.Links.B.IndexOf(this);
                if (index != -1)
                {
                    link.Links.B[index] = splitPart;
                }
                else
                {
                    index = link.Links.F.IndexOf(this);
                    link.Links.F[index] = splitPart;
                }
            }

            firstSplit.Links.F = new List<Segment> { segment, secondSplit };
            secondSplit.Links.B = new List<Segment> { segment, firstSplit };

            segment.Links.F.Add(firstSplit);
            segment.Links.F.Add(secondSplit);
        }

        public static PointT DoRoadSegmentsIntersect(RoadRepresentation r1, RoadRepresentation r2)
        {
            return MathUtil.DoLineSegmentsIntersect(r1.Start, r1.End, r2.Start, r2.End, true);
        }

        public class DebugData : Dictionary<string, object>
        {

        }

        public class LocalAction
        {
            public int Priority { get; set; }
            public Func<bool> Func { get; set; }
            public SegmentProperties Q { get; set; }

        }
        public static bool LocalConstraints(Segment segment, List<Segment> segments, QuadTree qTree, DebugData debugData)
        {
            var action = new LocalAction()
            {
                Priority = 0,
                Func = null,
                Q = new SegmentProperties()
            };

            var matches = qTree.Retrieve(segment.Collider.Limits());
            for (int i = 0; i < matches.Count(); i++)
            {
                Segment other = matches[i].O as Segment;

                // intersection check
                if (action.Priority <= 4)
                {
                    var intersection = DoRoadSegmentsIntersect(segment.R, other.R);
                    if (intersection != null)
                    {
                        if (!action.Q.t.HasValue || intersection.T < action.Q.t.Value)
                        {
                            action.Q.t = intersection.T;

                            action.Priority = 4;
                            action.Func = () =>
                            {
                                // if intersecting lines are too similar don't continue
                                if (Utility.MinDegreeDifference(other.Dir(), segment.Dir()) < Config.MapGeneration.MINIMUM_INTERSECTION_DEVIATION)
                                    return false;

                                other.Split(new Point(intersection.X, intersection.Y), segment, segments, qTree);
                                segment.R.End = new Point(intersection.X, intersection.Y);
                                segment.Q.Severed = true;

                                if (debugData != null)
                                {
                                    //if (debugData["intersections"] == null)
                                    //    debugData["intersections"] = new List<Point>();
                                    //(debugData["intersections"] as List<Point>).Add(new Point { X = intersection.X, Y = intersection.Y });
                                }

                                return true;
                            };
                        }
                    }
                }

                if (action.Priority <= 3)
                {
                    // current segment's start must have been checked to have been created.
                    // other segment's start must have a corresponding end.
                    if (MathUtil.Length(segment.R.End, other.R.End) <= Config.MapGeneration.ROAD_SNAP_DISTANCE)
                    {
                        var point = other.R.End;
                        action.Priority = 3;
                        action.Func = () =>
                        {
                            segment.R.End = point;
                            segment.Q.Severed = true;

                            // update Links of otherSegment corresponding to other.r.end
                            var links = other.StartIsBackwards() ? other.Links.F : other.Links.B;

                            // check for duplicate lines, don't add if it exists
                            // this should be done before Links are setup, to avoid having to undo that step
                            if (links.Any(link =>
                                (MathUtil.EqualV(link.R.Start, segment.R.End) && MathUtil.EqualV(link.R.End, segment.R.Start)) ||
                                (MathUtil.EqualV(link.R.Start, segment.R.Start) && MathUtil.EqualV(link.R.End, segment.R.End))))
                            {
                                return false;
                            }

                            foreach (var link in links)
                            {
                                // pick Links of remaining segments at junction corresponding to other.r.end
                                link.LinksForEndContaining(other).Add(segment);

                                // add junction segments to snapped segment
                                segment.Links.F.Add(link);
                            }

                            links.Add(segment);
                            segment.Links.F.Add(other);

                            if (debugData != null)
                            {
                                //if (debugData.snaps == null)
                                //{
                                //    debugData.snaps = new List<Point>();
                                //}
                                //debugData.snaps.Add(new Point { x = point.x, y = point.y });
                            }

                            return true;
                        };
                    }
                }

                if (action.Priority <= 2)
                {
                    var result = MathUtil.DistanceToLine(segment.R.End, other.R.Start, other.R.End);
                    var distance2 = result.Distance2;
                    var pointOnLine = result.PointOnLine;
                    var lineProj2 = result.LineProj2;
                    var length2 = result.Length2;

                    if (distance2 < Config.MapGeneration.ROAD_SNAP_DISTANCE * Config.MapGeneration.ROAD_SNAP_DISTANCE &&
                        lineProj2 >= 0 && lineProj2 <= length2)
                    {
                        var point = pointOnLine;
                        action.Priority = 2;
                        action.Func = () =>
                        {
                            segment.R.End = point;
                            segment.Q.Severed = true;

                            // if intersecting lines are too similar don't continue
                            if (Utility.MinDegreeDifference(other.Dir(), segment.Dir()) < Config.MapGeneration.MINIMUM_INTERSECTION_DEVIATION)
                                return false;

                            other.Split(point, segment, segments, qTree);

                            if (debugData != null)
                            {
                                //if (debugData.intersectionsRadius == null)
                                //    debugData.intersectionsRadius = new List<Point>();
                                //debugData.intersectionsRadius.Add(new Point { x = point.x, y = point.y });
                            }

                            return true;
                        };
                    }
                }
            }

            if (action.Func != null)
                return action.Func();

            return true;
        }

        public static Func<Segment, List<Segment>> globalGoals_Generate = (previousSegment) =>
        {
            var newBranches = new List<Segment>();
            if (!previousSegment.Q.Severed)
            {
                Func<double, double, double, SegmentProperties, Segment> template = (direction, length, t, q) =>
                    SegmentFactory.UsingDirection(previousSegment.R.End, direction, length, t, q);

                // used for highways or going straight on a normal branch
                Func<double, Segment> templateContinue = (dir) => template(dir, previousSegment.Length(), 0, previousSegment.Q);
                // not using q, i.e. not highways
                Func<double, Segment> templateBranch = (dir) => template(dir,
                    Config.MapGeneration.DEFAULT_SEGMENT_LENGTH,
                    previousSegment.Q.Highway ? Config.MapGeneration.NORMAL_BRANCH_TIME_DELAY_FROM_HIGHWAY : 0, null
                );

                var continueStraight = templateContinue(previousSegment.Dir());
                var straightPop = Heatmap.PopOnRoad(continueStraight.R);

                if (previousSegment.Q.Highway)
                {
                    var randomStraight = templateContinue(previousSegment.Dir() + Config.MapGeneration.RANDOM_STRAIGHT_ANGLE());
                    var randomPop = Heatmap.PopOnRoad(randomStraight.R);
                    double roadPop;

                    if (randomPop > straightPop)
                    {
                        newBranches.Add(randomStraight);
                        roadPop = randomPop;
                    }
                    else
                    {
                        newBranches.Add(continueStraight);
                        roadPop = straightPop;
                    }

                    if (roadPop > Config.MapGeneration.HIGHWAY_BRANCH_POPULATION_THRESHOLD)
                    {
                        if (new Random().NextDouble() < Config.MapGeneration.HIGHWAY_BRANCH_PROBABILITY)
                        {
                            var leftHighwayBranch = templateContinue(previousSegment.Dir() - 90 + Config.MapGeneration.RANDOM_BRANCH_ANGLE());
                            newBranches.Add(leftHighwayBranch);
                        }
                        else if (new Random().NextDouble() < Config.MapGeneration.HIGHWAY_BRANCH_PROBABILITY)
                        {
                            var rightHighwayBranch = templateContinue(previousSegment.Dir() + 90 + Config.MapGeneration.RANDOM_BRANCH_ANGLE());
                            newBranches.Add(rightHighwayBranch);
                        }
                    }
                }
                else if (straightPop > Config.MapGeneration.NORMAL_BRANCH_POPULATION_THRESHOLD)
                {
                    //var branchProb = MathUtil.Clamp((straightPop - 0.1) * 10, 0, 1);
                    //if (new Random().NextDouble() < branchProb)
                    //{
                    newBranches.Add(continueStraight);
                    //}
                }

                if (straightPop > Config.MapGeneration.NORMAL_BRANCH_POPULATION_THRESHOLD)
                {
                    var branchProb = Config.MapGeneration.DEFAULT_BRANCH_PROBABILITY;//
                    //var branchProb = MathUtil.Clamp((straightPop ) / 2, 0, Config.MapGeneration.DEFAULT_BRANCH_PROBABILITY);
                    if (new Random().NextDouble() < branchProb)
                    {
                        var leftBranch = templateBranch(previousSegment.Dir() - 90 + Config.MapGeneration.RANDOM_BRANCH_ANGLE());
                        newBranches.Add(leftBranch);
                    }
                    else if (new Random().NextDouble() < branchProb)
                    {
                        var rightBranch = templateBranch(previousSegment.Dir() + 90 + Config.MapGeneration.RANDOM_BRANCH_ANGLE());
                        newBranches.Add(rightBranch);
                    }
                }
            }

            foreach (var branch in newBranches)
            {
                branch.SetupBranchLinks = () =>
                {
                    // setup Links between each current branch and each existing branch stemming from the previous segment
                    foreach (var link in previousSegment.Links.F)
                    {
                        branch.Links.B.Add(link);
                        link.LinksForEndContaining(previousSegment).Add(branch);
                    }

                    previousSegment.Links.F.Add(branch);
                    branch.Links.B.Add(previousSegment);
                };
            }

            return newBranches;
        };

        public static void AddSegment(Segment segment, List<Segment> segmentList, QuadTree qTree)
        {
            segmentList.Add(segment);
            qTree.Insert(segment.Collider.Limits());
        }
    }

    public class MapGeneratorResult
    {
        public List<Segment> Segments { get; set; }
        public QuadTree QTree { get; set; }
        public Heatmap Heatmap { get; set; }
        public DebugData DebugData { get; set; }
    }

    public static class MapGenerator
    {
        public static MapGeneratorResult Generate(int seed)
        {
            var debugData = new DebugData();

            Random.Initialize(seed);
            Noise.Seed(new Random().Next());

            var priorityQ = new List<Segment>();
            // setup first segments in queue
            {
                var rootSegment = new Segment(new Point(0, 0), new Point(Config.MapGeneration.HIGHWAY_SEGMENT_LENGTH, 0), 0, new SegmentProperties { Highway = false });
                var oppositeDirection = SegmentFactory.FromExisting(rootSegment);
                var newEnd = new Point(
                    rootSegment.R.Start.X - Config.MapGeneration.HIGHWAY_SEGMENT_LENGTH,
                    oppositeDirection.R.End.Y
                );
                oppositeDirection.R.SetEnd(newEnd);
                oppositeDirection.Links.B.Add(rootSegment);
                rootSegment.Links.B.Add(oppositeDirection);
                priorityQ.Add(rootSegment);
                priorityQ.Add(oppositeDirection);
            }

            var segments = new List<Segment>();
            var qTree = new QuadTree(Config.MapGeneration.QUADTREE_PARAMS,
                Config.MapGeneration.QUADTREE_MAX_OBJECTS, Config.MapGeneration.QUADTREE_MAX_LEVELS);

            while (priorityQ.Count > 0 && segments.Count < Config.MapGeneration.SEGMENT_COUNT_LIMIT)
            {
                // pop smallest r(ti, ri, qi) from Q (i.e., smallest ‘t’)
                Segment minSegment = null;
                int minT_i = 0;
                for (int i = 0; i < priorityQ.Count; i++)
                {
                    var segment = priorityQ[i];
                    if (minSegment == null || segment.T < minSegment.T)
                    {
                        minSegment = segment;
                        minT_i = i;
                    }
                }

                priorityQ.RemoveAt(minT_i);

                var accepted = LocalConstraints(minSegment, segments, qTree, debugData);
                if (accepted)
                {
                    minSegment.SetupBranchLinks?.Invoke();
                    AddSegment(minSegment, segments, qTree);
                    foreach (var newSegment in globalGoals_Generate(minSegment))
                    {
                        newSegment.T = minSegment.T + 1 + newSegment.T;
                        priorityQ.Add(newSegment);
                    }
                }
            }

            int id = 0;
            foreach (var segment in segments)
            {
                segment.Id = id++;
            }

            Console.WriteLine($"{segments.Count} segments generated.");

            return new MapGeneratorResult
            {
                Segments = segments,
                QTree = qTree,
                Heatmap = new Heatmap(),
                DebugData = debugData
            };
        }
    }

    public class SegmentFactory
    {
        public static Segment FromExisting(Segment segment)
        {
            var t = segment.T;
            var r = segment.R;
            var q = segment.Q;

            return new Segment(r.Start, r.End, t, q);
        }

        public static Segment UsingDirection(Point start, double dir, double length, double t, SegmentProperties q)
        {
            var end = new Point
            {
                X = start.X + length * MathUtil.SinDegrees(dir),
                Y = start.Y + length * MathUtil.CosDegrees(dir)
            };

            return new Segment(start, end, t, q);
        }
    }

    public class Heatmap
    {
        public static double PopOnRoad(RoadRepresentation r)
        {
            return (PopulationAt(r.Start.X, r.Start.Y) + PopulationAt(r.End.X, r.End.Y)) / 2;
        }

        public static double PopulationAt(double x, double y)
        {
            double value1 = (Noise.Simplex2(x / 10000, y / 10000) + 1) / 2;
            double value2 = (Noise.Simplex2(x / 20000 + 500, y / 20000 + 500) + 1) / 2;
            double value3 = (Noise.Simplex2(x / 20000 + 1000, y / 20000 + 1000) + 1) / 2;
            return Math.Pow((value1 * value2 + value3) / 2, 2);
        }
    }

    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }
        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
        public Point() { }

        public Point Clone()
        {
            return new Point(X, Y);
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ")";
        }
    }
}