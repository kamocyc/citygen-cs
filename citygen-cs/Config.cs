// original author: tmwhere.com

using System;

namespace citygen_cs.Config
{
    public static class ConfigUtils
    {
        public const int BranchAngleDev = 3;
        public const int ForwardAngleDev = 15;

        public static double RandomAngle(int limit)
        {
            // non-linear distribution
            double nonUniformNorm = Math.Pow(Math.Abs(limit), 3);
            double val = 0;
            Random random = new Random();
            while (val == 0 || random.NextDouble() < Math.Pow(Math.Abs(val), 3) / nonUniformNorm)
            {
                val = RandomRange(-limit, limit, random);
            }
            return val;
        }

        private static double RandomRange(int min, int max, Random random)
        {
            return random.Next(min, max) + random.NextDouble();
        }
    }

    public static class MapGeneration
    {
        public const int BUILDING_PLACEMENT_LOOP_LIMIT = 3;
        public const int DEFAULT_SEGMENT_LENGTH = 300;
        public const int HIGHWAY_SEGMENT_LENGTH = 400;
        public const int DEFAULT_SEGMENT_WIDTH = 6;
        public const int HIGHWAY_SEGMENT_WIDTH = 16;
        public static double RANDOM_BRANCH_ANGLE() => ConfigUtils.RandomAngle(ConfigUtils.BranchAngleDev);
        public static double RANDOM_STRAIGHT_ANGLE() => ConfigUtils.RandomAngle(ConfigUtils.ForwardAngleDev);
        public const double DEFAULT_BRANCH_PROBABILITY = 0.4;
        public const double HIGHWAY_BRANCH_PROBABILITY = 0.05;
        public const double HIGHWAY_BRANCH_POPULATION_THRESHOLD = 0.1;
        public const double NORMAL_BRANCH_POPULATION_THRESHOLD = 0.1;
        public const int NORMAL_BRANCH_TIME_DELAY_FROM_HIGHWAY = 5;
        public const int MINIMUM_INTERSECTION_DEVIATION = 30; // degrees
        public const int SEGMENT_COUNT_LIMIT = 5000;    // road count
        public const int DEBUG_DELAY = 0; // ms
        public const int ROAD_SNAP_DISTANCE = 50;
        public const int HEAT_MAP_PIXEL_DIM = 50; // px
        public const bool DRAW_HEATMAP = false;
        public static readonly Rectangle QUADTREE_PARAMS = new Rectangle
        {
            X = -20000,
            Y = -20000,
            Width = 40000,
            Height = 40000
        };
        public const int QUADTREE_MAX_OBJECTS = 10;
        public const int QUADTREE_MAX_LEVELS = 10;
        public const bool DEBUG = false;
    }

    public static class GameLogic
    {
        public const int SelectPanThreshold = 50; // px, limit beyond which a click becomes a drag
        public const int SelectionRange = 50; // px
        public const int DEFAULT_PICKUP_RANGE = 150; // world units
        public const int DefaultCargoCapacity = 1;
        public const double MinSpeedProportion = 0.1; // the minimum reduction of the speed of a road when congested
    }
}
