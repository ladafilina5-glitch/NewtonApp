using System;

namespace NewtonApp.Models
{
    public class IntegralRecord
    {
        public int Id { get; set; }
        public DateTime CalcTime { get; set; }
        public double A { get; set; }
        public double B { get; set; }
        public int N { get; set; }
        public double Result { get; set; }
        public double ElapsedMs { get; set; }
        public int TaskCount { get; set; }
    }
}