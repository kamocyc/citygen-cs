using citygen_cs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var rectAProps = new CollisionObjectProperties()
            {
                Corners = new Point[] {
                    new Point(4758.730559846348, -2670.2533388896254),
                    new Point(4836.103420643231, -2634.0130558962314),
                    new Point(4778.723608790986, -2511.507384629691),
                    new Point(4701.350747994103, -2547.747667623085)
                }
            };
            var rectBProps = new CollisionObjectProperties()
            {
                Corners = new Point[] {
                    new Point(4624.980060717344, -2722.4945862419286),
                    new Point(4709.622176911843, -2682.849493473536),
                    new Point(4654.539806419771, -2565.248850420546),
                    new Point(4569.897690225272, -2604.893943188939)
                }
            };

            var r = CollisionObject.RectRectIntersection(rectAProps, rectBProps);
            Assert.IsNull(r);
        }
    }
}

