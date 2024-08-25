using System;
using System.Collections.Generic;
using System.Linq;

namespace citygen_cs
{
    public static class Utility
    {
        public static T DefaultFor<T>(T arg, T val, bool deep)
        {
            if (arg != null)
            {
                return deep ? DeepClone(arg) : arg;
            }
            else
            {
                return deep ? DeepClone(val) : val;
            }
        }

        private static T DeepClone<T>(T obj)
        {
            // Implement deep clone logic here
            // This is a placeholder for deep clone functionality
            return obj;
        }

        public static List<T> JoinArrayGeneric<T>(List<T> array, T joinElement)
        {
            var copy = new List<T>(array);
            for (int i = 1; i < copy.Count * 2 - 1; i += 2)
            {
                copy.Insert(i, joinElement);
            }
            return copy;
        }

        public static double MinDegreeDifference(double d1, double d2)
        {
            double diff = Math.Abs(d1 - d2) % 180;
            return Math.Min(diff, Math.Abs(diff - 180));
        }

        public static (double, int) ExtendedMin(IEnumerable<double> collection)
        {
            double minObj = collection.First();
            int minObjIndex = 0;
            int index = 0;

            foreach (var obj in collection)
            {
                if (obj.CompareTo(minObj) < 0)
                {
                    minObj = obj;
                    minObjIndex = index;
                }
                index++;
            }

            return (minObj, minObjIndex);
        }

        public static (double, int) ExtendedMax(IEnumerable<double> collection)
        {
            double maxObj = collection.First();
            int maxObjIndex = 0;
            int index = 0;

            foreach (var obj in collection)
            {
                if (obj.CompareTo(maxObj) > 0)
                {
                    maxObj = obj;
                    maxObjIndex = index;
                }
                index++;
            }

            return (maxObj, maxObjIndex);
        }

        public class PriorityQueue<T>
        {
            private List<(T item, int priority)> list = new List<(T item, int priority)>();

            public void Put(T item, int priority)
            {
                var newPair = (item, priority);
                int index = list.FindIndex(pair => pair.priority > newPair.priority);
                if (index == -1)
                {
                    list.Add(newPair);
                }
                else
                {
                    list.Insert(index, newPair);
                }
            }

            public T Get()
            {
                var item = list[0].item;
                list.RemoveAt(0);
                return item;
            }

            public int Length => list.Count;
        }
    }
}
