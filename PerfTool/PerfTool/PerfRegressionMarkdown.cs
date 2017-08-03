﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfTool
{
    class PerfRegressionMarkdown
    {
        private PerformanceTest _bench;
        private PerformanceTest _latest;
        private string _oldVersion;
        private int _threshold;
        private IList<KeyValuePair<TestItem, TestItem>> _matched;

        public PerfRegressionMarkdown(PerformanceTest bench, PerformanceTest latest, string odlVersion, int threshold)
        {
            string prefix = odlVersion + "." + bench.TestType + "." + bench.CreateDate + "." + bench.BuildId;

            ImageFileName = "OData." + bench.TestType; // always save the latest one

            LogFileName = prefix + ".md";
            LatestFile = "OData." + bench.TestType + ".latest.md";
            _oldVersion = odlVersion;
            _threshold = threshold;
            _bench = bench;
            _latest = latest;

            HistoryFileName = "OData."  + bench.TestType + ".History.md";
        }

        public string LatestFile { get; private set; }
        public string ImageFileName { get; private set; }
        public string LogFileName { get; private set; }
        public string HistoryFileName { get; private set; }

        public void CreateMarkdown()
        {
            Match();

            WriteLogFile();

            WriteImageFile();

            WriteLatestFile();

            UpdateHistory();
        }

        private void WriteImageFile()
        {
            string maxImage = ImageFileName + ".max.png";
            IEnumerable<double> maxes = _matched.Select(a => 100 * (a.Value.Max - a.Key.Max)/ a.Key.Max);
            WriteImageFile(maxImage, "Max", maxes);

            string meanImage = ImageFileName + ".mean.png";
            IEnumerable<double> means = _matched.Select(a => 100 * (a.Value.Mean - a.Key.Mean) / a.Key.Mean);
            WriteImageFile(meanImage, "Mean", means);

            string minImage = ImageFileName + ".min.png";
            IEnumerable<double> mins = _matched.Select(a => 100 * (a.Value.Min - a.Key.Min) / a.Key.Min);
            WriteImageFile(minImage, "Min", mins);
        }

        private void WriteImageFile(string imageFileName, string type, IEnumerable<double> percentages)
        {
            Bitmap bmap = new Bitmap(PerfImage.Width, PerfImage.Height);
            Graphics gph = Graphics.FromImage(bmap);
            gph.Clear(Color.White);

            PerfImage.Draw(gph, percentages, _threshold);

            // Draw Legend
            string legend = _bench.TestType + " / " + _bench.CreateDate + " / " + _bench.BuildId +  " / " + _threshold + " %";
            gph.DrawString(legend, new Font("Calibri", 12), Brushes.DarkBlue, new PointF(PerfImage.Width/2 - 110, 0));

            gph.DrawString(type + " [ V" + _oldVersion + " ]", new Font("Calibri", 14), Brushes.DarkGreen, new PointF(PerfImage.Width/2 - 25, PerfImage.Height - 30));
            bmap.Save(imageFileName, ImageFormat.Png);
        }

        private void WriteLogFile()
        {
            FileStream fs = new FileStream(LogFileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            sw.WriteLine("### " + _oldVersion + "." + _bench.TestType + "." + _bench.CreateDate + "." + _bench.BuildId + "\n---\n");
            WriteData(sw);

            sw.Flush();
            sw.Close();
            fs.Close();
        }

        private void WriteData(StreamWriter sw)
        {
            sw.WriteLine("| No. | TestName | Base Max | Latest Max | Max (%) | <=> | Base Mean | Latest Mean | Mean (%) | <=> | Base Min | Latest Min | Min (%) |");
            sw.WriteLine("|:----|:---------|---------:|-----------:|--------:|-----|----------:|------------:|---------:|-----|---------:|-----------:|--------:|");

            int index = 1;
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<TestItem, TestItem> pair in _matched)
            {
                TestItem b = pair.Key;
                TestItem c = pair.Value;
                sb.Clear();
                sb.Append("|" + index + "|").Append(pair.Key.Name.Replace("Microsoft.OData.Performance.", "")).Append("|")
                    .Append(b.Max).Append("|").Append(c.Max).Append("|").Append(GetPercentage(b.Max, c.Max)).Append("|").Append(" |")
                    .Append(b.Mean).Append("|").Append(c.Mean).Append("|").Append(GetPercentage(b.Mean, c.Mean)).Append("|").Append(" |")
                    .Append(b.Min).Append("|").Append(c.Min).Append("|").Append(GetPercentage(b.Min, c.Min)).Append("|");
                sw.WriteLine(sb.ToString());
                index++;
            }
        }

        private void WriteLatestFile()
        {
            FileStream fs = new FileStream(LatestFile, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            string head = "| VER Max | VER Mean | VER Min |".Replace("VER", _oldVersion);
            sw.WriteLine(head);
            sw.WriteLine("|:---:|:----:|:---:|");

            string maxImage = ImageFileName + ".max.png";
            string meanImage = ImageFileName + ".mean.png";
            string minImage = ImageFileName + ".min.png";
            sw.Write("|![image](./images/" + maxImage + ")|");
            sw.Write("![image](./images/" + meanImage + ")|");
            sw.WriteLine("![image](./images/" + minImage + ")|");
            sw.WriteLine("---");
            WriteData(sw);

            sw.Flush();
            sw.Close();
            fs.Close();
        }

        private void UpdateHistory()
        {
            IList<string> histories = null;
            if (File.Exists(HistoryFileName))
            {
                histories = ReadHistory();
            }

            if (histories == null)
            {
                histories = new List<string>();
            }

            const int MaxHistoryCount = 25;
            while (true)
            {
                if (histories.Count <= MaxHistoryCount)
                {
                    break;
                }

                // remove the last one
                histories.RemoveAt(histories.Count - 1);
            }
            histories.Insert(0, "- [" + _bench.CreateDate + "](./logs/" + LogFileName + ")");

            FileStream fs = new FileStream(HistoryFileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            sw.WriteLine("History:\n---");
            foreach(string history in histories)
            {
                sw.WriteLine(history);
            }

            sw.Flush();
            sw.Close();
            fs.Close();
        }

        private IList<string> ReadHistory()
        {
            IList<string> newReturns = new List<string>();
            try
            {
                StreamReader sr = new StreamReader(HistoryFileName, Encoding.Default);
                String line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("- ["))
                    {
                        newReturns.Add(line);
                    }
                }

                sr.Close();
            }
            catch
            {
                return newReturns;
            }

            return newReturns;
        }

        private void Match()
        {
            if (_matched != null)
            {
                return;
            }

            _matched = new List<KeyValuePair<TestItem, TestItem>>();
            foreach (TestItem item in _latest.Items)
            {
                TestItem baseItem = _bench.Find(item.Name);
                _matched.Add(new KeyValuePair<TestItem, TestItem>(baseItem, item));
            }
        }

        private static string GetPercentage(double b, double c)
        {
            double d = (c - b) / b;
            d *= 100.0;
            string ret = "";
            if (d < 0)
            {
                d *= -1;
                ret = "-";
            }

            d += 0.005;
            int integer = (int)d;
            int last = (int)((d - integer) * 100);
            return ret + integer + "." + last + "%";
        }
    }
}