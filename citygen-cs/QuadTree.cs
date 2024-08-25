// original comment below:
/*
 * Javascript Quadtree 
 * @version 1.1
 * @author Timo Hausmann
 * https://github.com/timohausmann/quadtree-js/
 */

/*
 Copyright © 2012 Timo Hausmann

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENthis. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;

namespace citygen_cs
{
    public class QuadTree
    {
        private int max_objects;
        private int max_levels;
        private int level;
        private Rectangle bounds;
        private List<Rectangle> objects;
        private QuadTree[] nodes;

        public QuadTree(Rectangle bounds, int max_objects, int max_levels, int level = 0)
        {
            this.max_objects = max_objects;
            this.max_levels = max_levels;
            this.level = level;
            this.bounds = bounds;
            this.objects = new List<Rectangle>();
            this.nodes = new QuadTree[4];
        }

        public void Split()
        {
            int nextLevel = this.level + 1;
            int subWidth = (int)Math.Round(this.bounds.Width / 2.0);
            int subHeight = (int)Math.Round(this.bounds.Height / 2.0);
            int x = (int)(this.bounds.X);
            int y = (int)(this.bounds.Y);

            // top right node
            this.nodes[0] = new QuadTree(new Rectangle(x + subWidth, y, subWidth, subHeight), this.max_objects, this.max_levels, nextLevel);

            // top left node
            this.nodes[1] = new QuadTree(new Rectangle(x, y, subWidth, subHeight), this.max_objects, this.max_levels, nextLevel);

            // bottom left node
            this.nodes[2] = new QuadTree(new Rectangle(x, y + subHeight, subWidth, subHeight), this.max_objects, this.max_levels, nextLevel);

            // bottom right node
            this.nodes[3] = new QuadTree(new Rectangle(x + subWidth, y + subHeight, subWidth, subHeight), this.max_objects, this.max_levels, nextLevel);
        }

        public int GetIndex(Rectangle pRect)
        {
            int index = -1;
            double verticalMidpoint = this.bounds.X + (this.bounds.Width / 2.0);
            double horizontalMidpoint = this.bounds.Y + (this.bounds.Height / 2.0);

            // pRect can completely fit within the top quadrants
            bool topQuadrant = (pRect.Y < horizontalMidpoint && pRect.Y + pRect.Height < horizontalMidpoint);

            // pRect can completely fit within the bottom quadrants
            bool bottomQuadrant = (pRect.Y > horizontalMidpoint);

            // pRect can completely fit within the left quadrants
            if (pRect.X < verticalMidpoint && pRect.X + pRect.Width < verticalMidpoint)
            {
                if (topQuadrant)
                {
                    index = 1;
                }
                else if (bottomQuadrant)
                {
                    index = 2;
                }
            }
            // pRect can completely fit within the right quadrants
            else if (pRect.X > verticalMidpoint)
            {
                if (topQuadrant)
                {
                    index = 0;
                }
                else if (bottomQuadrant)
                {
                    index = 3;
                }
            }

            return index;
        }

        public void Insert(Rectangle pRect)
        {
            int index;

            // If we have subnodes ...
            if (this.nodes[0] != null)
            {
                index = this.GetIndex(pRect);

                if (index != -1)
                {
                    this.nodes[index].Insert(pRect);
                    return;
                }
            }

            this.objects.Add(pRect);

            if (this.objects.Count > this.max_objects && this.level < this.max_levels)
            {
                // Split if we don't already have subnodes
                if (this.nodes[0] == null)
                {
                    this.Split();
                }

                // Add all objects to their corresponding subnodes
                int i = 0;
                while (i < this.objects.Count)
                {
                    index = this.GetIndex(this.objects[i]);
                    if (index != -1)
                    {
                        this.nodes[index].Insert(this.objects[i]);
                        this.objects.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        public List<Rectangle> Retrieve(Rectangle pRect)
        {
            int index = this.GetIndex(pRect);
            List<Rectangle> returnObjects = new List<Rectangle>(this.objects);

            // If we have subnodes ...
            if (this.nodes[0] != null)
            {
                // If pRect fits into a subnode ...
                if (index != -1)
                {
                    returnObjects.AddRange(this.nodes[index].Retrieve(pRect));
                }
                // If pRect does not fit into a subnode, check it against all subnodes
                else
                {
                    for (int i = 0; i < this.nodes.Length; i++)
                    {
                        returnObjects.AddRange(this.nodes[i].Retrieve(pRect));
                    }
                }
            }

            return returnObjects;
        }

        public void Clear()
        {
            this.objects.Clear();

            for (int i = 0; i < this.nodes.Length; i++)
            {
                if (this.nodes[i] != null)
                {
                    this.nodes[i].Clear();
                    this.nodes[i] = null;
                }
            }
        }

    }

    public class Rectangle
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public object O { get; set; }

        public Rectangle(double x, double y, double width, double height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }
        public Rectangle() { }
    }
}
