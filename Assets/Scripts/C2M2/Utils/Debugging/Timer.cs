using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;

namespace C2M2
{
    namespace Utils
    {       
        public class Timer
        {
            public List<TimerNode> timerNodes { get; private set; }
            private DateTime t0;
            private DateTime t1;
            private Stopwatch stopwatch;

            public struct TimerNode
            {
                public string name;

                public long Ticks => stopwatch.ElapsedTicks;
                public double Milliseconds => (double)stopwatch.ElapsedTicks / TimeSpan.TicksPerMillisecond;
                public double Seconds => (double)stopwatch.ElapsedTicks / TimeSpan.TicksPerSecond;
                public double Minutes => (double)stopwatch.ElapsedTicks / TimeSpan.TicksPerMinute;
                public double Hours => (double)stopwatch.ElapsedTicks / TimeSpan.TicksPerHour;
                public double Days => (double)stopwatch.ElapsedTicks / TimeSpan.TicksPerDay;

                private Stopwatch stopwatch;

                public TimerNode(Stopwatch stopwatch, string name)
                {
                    this.stopwatch = stopwatch;
                    this.name = name;
                }
            }

            public Timer() => timerNodes = new List<TimerNode>();
            public Timer(int size) => timerNodes = new List<TimerNode>(size);
            /// <summary> Create a new timer node and mark its creation time </summary>
            public void StartTimer() => stopwatch = Stopwatch.StartNew();
            /// <summary> Stop the current time node and calculate the time since the node was created </summary>
            /// <returns> Time in milliseconds since the time node was created. </returns>
            public double StopTimer(string name)
            {
                stopwatch.Stop();

                // Create new data point, add it to the log
                TimerNode newNode = new TimerNode(stopwatch, name);
                timerNodes.Add(newNode);

                // Return the completed node time;
                return newNode.Milliseconds;
            }
            public double StopTimer() => StopTimer("");
		
	    public void ExportCSV_path(string filePath)
            {
                string timerInfo = ToString();
                CSVBuilder csv = new CSVBuilder();
                csv.ExportCSV(timerInfo, filePath, overwrite: true);
            }

            public void ExportCSV(string newFileName, bool buildSimpleStats = false)
            {
                string timerInfo = ToStringPlus();
         
                CSVBuilder csv = new CSVBuilder();
                char separator = System.IO.Path.DirectorySeparatorChar;
                string filePath = Application.dataPath + separator + "TimerResults" + separator + newFileName;
                csv.ExportCSV(timerInfo, filePath, overwrite: true);
            }

            public override string ToString()
            {
                // TODO: Use StringBuilder here and use timerNodes.Count to estimate StringBuilder size;
                string s = String.Format("{0},{1}", "name", "time (ms)");

                string formatString = "\n{0},{1:0.0000}";
                foreach (TimerNode node in timerNodes)
                {
                    s += String.Format(formatString, node.name, node.Milliseconds);
                }
                return s;
            }
            public string ToStringPlus()
            {
                // TODO: Use StringBuilder here and use timerNodes.Count to estimate StringBuilder size;
                string s = String.Format("{0},{1},{2},{3},{4},{5}", "name", "time (ms)", "max (ms)", "avg (ms)", "std.dev (ms)", "min (ms)");

                string[] names = new string[timerNodes.Count];
                double[] times = new double[timerNodes.Count];

                for(int i = 0; i < timerNodes.Count; i++)
                {
                    names[i] = timerNodes[i].name;
                    times[i] = timerNodes[i].Milliseconds;
                }
                string formatString = "\n{0},{1:0.#####},{2:0.#####},{3:0.#####},{4:0.#####},{5:0.####}";
                for (int i = 0; i < timerNodes.Count; i++)
                {
                    string newLine;
                    if (i == 0)
                    {
                        newLine = String.Format(formatString, names[i], times[i], times.Max(), times.Avg(), times.StdDev(), times.Min());
                        formatString = "\n{0},{1:0.#####}";
                    }
                    else newLine = String.Format(formatString, names[i], times[i]);
                    s += newLine;
                }
                return s;
            }
        }
        public class TimeNodeNotFoundException : Exception
        {
            public TimeNodeNotFoundException() { }
            public TimeNodeNotFoundException(string message) : base(message) { }
            public TimeNodeNotFoundException(string message, Exception inner) : base(message, inner) { }
        }
    }
}