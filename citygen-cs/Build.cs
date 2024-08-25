// original author: tmwhere.com

using System;
using System.Collections.Generic;
using System.Linq;

namespace citygen_cs
{
    public class Building
    {
        public enum Type
        {
            RESIDENTIAL,
            IMPORT
        }

        private static int idCounter = 0;

        public int Id { get; private set; }
        public Point Center { get; private set; }
        public double Dir { get; private set; }
        public double Diagonal { get; private set; }
        public Type BuildingType { get; private set; }
        public double AspectDegree { get; private set; }
        public Point[] Corners { get; private set; }
        public CollisionObject Collider { get; private set; }
        public List<object> Supply { get; private set; }
        public List<object> Demand { get; private set; }

        public Building(Point center, double dir, double diagonal, Type type, double aspectRatio = 1)
        {
            Center = center;
            Dir = dir;
            Diagonal = diagonal;
            BuildingType = type;
            AspectDegree = Math.Atan(aspectRatio) * (180 / Math.PI);
            Corners = GenerateCorners();
            Collider = new CollisionObject(this, CollisionType.RECT, new CollisionObjectProperties { Corners = Corners });
            Supply = new List<object>();
            Demand = new List<object>();
            Id = idCounter++;
        }

        private Point[] GenerateCorners()
        {
            return new Point[]
            {
                new Point(Center.X + Diagonal * Math.Sin(DegreesToRadians(+AspectDegree + Dir)), Center.Y + Diagonal * Math.Cos(DegreesToRadians(+AspectDegree + Dir))),
                new Point(Center.X + Diagonal * Math.Sin(DegreesToRadians(-AspectDegree + Dir)), Center.Y + Diagonal * Math.Cos(DegreesToRadians(-AspectDegree + Dir))),
                new Point(Center.X + Diagonal * Math.Sin(DegreesToRadians(180 + AspectDegree + Dir)), Center.Y + Diagonal * Math.Cos(DegreesToRadians(180 + AspectDegree + Dir))),
                new Point(Center.X + Diagonal * Math.Sin(DegreesToRadians(180 - AspectDegree + Dir)), Center.Y + Diagonal * Math.Cos(DegreesToRadians(180 - AspectDegree + Dir)))
            };
        }

        public void SetCenter(Point val)
        {
            Center = val;
            Corners = GenerateCorners();
            Collider.UpdateCollisionProperties(new CollisionObjectProperties { Corners = Corners });
        }

        public void SetDir(double val)
        {
            Dir = val;
            Corners = GenerateCorners();
            Collider.UpdateCollisionProperties(new CollisionObjectProperties { Corners = Corners });
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }

    public static class BuildingUtils
    {
        public static List<Building> BuildingsInRangeOf(Point location, QuadTree qTree)
        {
            var matches = qTree.Retrieve(new Rectangle
            {
                X = location.X - Config.GameLogic.DEFAULT_PICKUP_RANGE,
                Y = location.Y - Config.GameLogic.DEFAULT_PICKUP_RANGE,
                Width = Config.GameLogic.DEFAULT_PICKUP_RANGE * 2,
                Height = Config.GameLogic.DEFAULT_PICKUP_RANGE * 2
            });

            var buildings = new List<Building>();
            var range = new CollisionObject(null, CollisionType.CIRCLE, new CollisionObjectProperties { Center = location, Radius = Config.GameLogic.DEFAULT_PICKUP_RANGE });

            foreach (var match in matches)
            {
                if (!(match.O is Building matchO))
                {
                    continue;
                }
                if (matchO.Supply != null && matchO.Demand != null && range.Collide(matchO.Collider) != null)
                {
                    buildings.Add(matchO);
                }
            }

            return buildings;
        }
    }

    public class BuildingFactory
    {
        public static Building FromProbability(double time)
        {
            return new Random().NextDouble() < 0.4 ? ByType(Building.Type.IMPORT, time) : ByType(Building.Type.RESIDENTIAL, time);
        }

        public static Building ByType(Building.Type type, double time)
        {
            Building building = null;
            switch (type)
            {
                case Building.Type.RESIDENTIAL:
                    building = new Building(new Point(0, 0), 0, 80, Building.Type.RESIDENTIAL, RandomRange(0.5, 2));
                    break;
                case Building.Type.IMPORT:
                    building = new Building(new Point(0, 0), 0, 150, Building.Type.IMPORT, RandomRange(0.5, 2));
                    break;
            }
            return building;
        }

        public static List<Building> AroundSegment(Func<Building> buildingTemplate, Segment segment, int count, double radius, QuadTree quadtree)
        {
            var buildings = new List<Building>();
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                double randomAngle = random.NextDouble() * 360;
                double randomRadius = random.NextDouble() * radius;
                var buildingCenter = new Point(
                    0.5 * (segment.R.Start.X + segment.R.End.X) + randomRadius * Math.Sin(DegreesToRadians(randomAngle)),
                    0.5 * (segment.R.Start.Y + segment.R.End.Y) + randomRadius * Math.Cos(DegreesToRadians(randomAngle))
                );

                var building = buildingTemplate();
                building.SetCenter(buildingCenter);
                building.SetDir(segment.Dir());

                bool permitBuilding = false;
                for (int j = 0; j < Config.MapGeneration.BUILDING_PLACEMENT_LOOP_LIMIT; j++)
                {
                    int collisionCount = 0;
                    var potentialCollisions = quadtree.Retrieve(building.Collider.Limits()).Select(r => r.O).Concat(buildings).ToList();

                    foreach (var obj in potentialCollisions)
                    {
                        CollisionObject objCollider;
                        if (obj is Segment segment1)
                        {
                            objCollider = segment1.Collider;
                        }
                        else if (obj is Building building1)
                        {
                            objCollider = building1.Collider;
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        var result = building.Collider.Collide(objCollider);
                        if (result != null)
                        {
                            collisionCount++;
                            if (j == Config.MapGeneration.BUILDING_PLACEMENT_LOOP_LIMIT - 1)
                                break;

                            building.SetCenter(AddPoints(building.Center, result));
                        }
                    }

                    if (collisionCount == 0)
                    {
                        permitBuilding = true;
                        break;
                    }
                }

                if (permitBuilding)
                {
                    buildings.Add(building);
                }
            }

            return buildings;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        private static double RandomRange(double min, double max)
        {
            var random = new Random();
            return random.NextDouble() * (max - min) + min;
        }

        private static Point AddPoints(Point p1, Point p2)
        {
            return new Point(p1.X + p2.X, p1.Y + p2.Y);
        }
    }
}
