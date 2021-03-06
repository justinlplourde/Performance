﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfTool
{
    public class TestItem
    {
        public string Name { get; set; }
        public double Min { get; set; }
        public double Mean { get; set; }
        public double Max { get; set; }
        public double MarginOfError { get; set; }
        public double StdDev { get; set; }

        public double GCMax { get; set; }
        public double GCMean { get; set; }
        public double GCMin { get; set; }
        public double GCMarginOfError { get; set; }
        public double GCStdDev { get; set; }
    }
}
