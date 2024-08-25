using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace citygen_cs
{
    public partial class Form1 : Form
    {
        private MapGeneratorResult mapGeneratorResult;
        private List<Building> buildings;

        private System.Drawing.Point? mousePoint;
        private float offsetX = 200;
        private float offsetY = 200;
        private float zoom = 0.05f;

        public Form1()
        {
            InitializeComponent();

            Initialize(1);

            // double buffering
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.UserPaint, true);
        }

        private void Initialize(int seed)
        {
            mapGeneratorResult = MapGenerator.Generate(seed);

            buildings = new List<Building>();
            QuadTree qTree = mapGeneratorResult.QTree;
            for (int i = 0; i < mapGeneratorResult.Segments.Count; i += 10)
            {
                var segment = mapGeneratorResult.Segments[i];

                var newBuildings = BuildingFactory.AroundSegment(
                    () => BuildingFactory.FromProbability(DateTime.Now.Ticks),
                    segment, 10, 400, mapGeneratorResult.QTree
                );

                foreach (var building in newBuildings)
                {
                    mapGeneratorResult.QTree.Insert(building.Collider.Limits());
                }

                buildings.AddRange(newBuildings);
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                mousePoint = new System.Drawing.Point(e.X, e.Y);
            }

            if ((e.Button & MouseButtons.Right) == MouseButtons.Right)
            {
                Initialize(2);

                this.Invalidate();
            }

            if ((e.Button & MouseButtons.Middle) == MouseButtons.Middle)
            {
                Initialize(new Random().Next());

                this.Invalidate();
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left && mousePoint != null)
            {
                offsetX += e.X - mousePoint.Value.X;
                offsetY += e.Y - mousePoint.Value.Y;
                mousePoint = new System.Drawing.Point(e.X, e.Y);
                this.Invalidate();
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            mousePoint = null;
        }

        // zoom
        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                zoom *= 1.1f;
            }
            else
            {
                zoom /= 1.1f;
            }

            if (zoom < 0.01)
            {
                zoom = 0.01f;
            }

            this.Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);

            // draw heatmap
            int windowWidth = this.Width;
            int windowHeight = this.Height;
            int rectSize = 20;
            for (int x = 0; x < windowWidth; x += rectSize)
            {
                for (int y = 0; y < windowHeight; y += rectSize)
                {
                    var heat = Heatmap.PopulationAt((x - offsetX) / zoom, (y - offsetY) / zoom);
                    int color = (int)(255 * heat);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(0, color, 0)), x, y, rectSize, rectSize);
                }
            }

            // draw segments
            foreach (var segment in mapGeneratorResult.Segments)
            {
                var pen = segment.Q.Highway ? Pens.Pink : Pens.White;
                g.DrawLine(pen, (float)segment.R.Start.X * zoom + offsetX, (float)segment.R.Start.Y * zoom + offsetY, (float)segment.R.End.X * zoom + offsetX, (float)segment.R.End.Y * zoom + offsetY);
            }

            // draw buildings
            foreach (var building in buildings)
            {
                // draw by 4 lines
                var pen = Pens.Red;
                g.DrawLine(pen, (float)building.Corners[0].X * zoom + offsetX, (float)building.Corners[0].Y * zoom + offsetY, (float)building.Corners[1].X * zoom + offsetX, (float)building.Corners[1].Y * zoom + offsetY);
                g.DrawLine(pen, (float)building.Corners[1].X * zoom + offsetX, (float)building.Corners[1].Y * zoom + offsetY, (float)building.Corners[2].X * zoom + offsetX, (float)building.Corners[2].Y * zoom + offsetY);
                g.DrawLine(pen, (float)building.Corners[2].X * zoom + offsetX, (float)building.Corners[2].Y * zoom + offsetY, (float)building.Corners[3].X * zoom + offsetX, (float)building.Corners[3].Y * zoom + offsetY);
                g.DrawLine(pen, (float)building.Corners[3].X * zoom + offsetX, (float)building.Corners[3].Y * zoom + offsetY, (float)building.Corners[0].X * zoom + offsetX, (float)building.Corners[0].Y * zoom + offsetY);
            }
        }
    }
}
