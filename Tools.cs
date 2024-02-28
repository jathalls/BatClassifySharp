/*************************************************************************
  Copyright 2024 Justin A T Halls (jathalls@gmail.com)
  Copyright 2011-2014 Chris Scott (fbscds@gmail.com)

  This program is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with This program.  If not, see <http://www.gnu.org/licenses/>.

*************************************************************************/
using NAudio.Dsp;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BatClassifySharp
{
    /// <summary>
    /// called Utilities in the original with 'inline' functions, implemented here as
    /// static functions of the Tools static class
    /// </summary>
    internal static class Tools
    {
        public static float PositiveHalfWaveRectify(float x) { return ((float)(x + Math.Abs(x) * 0.5f)); }

        public static void Moments(List<float> data, int start, int end, ref List<float> result)
        {
            float[] index = new float[end - start];

            for (int i = 0; i < index.Length; i++) { index[i] = i + 1; }
            float[] x = new float[end - start];
            data.CopyTo(start, x, 0, end - start);

            float sum = x.Sum();
            float epsilon = float.Epsilon;
            sum = sum > epsilon ? sum : epsilon;

            for (int i = 0; i < x.Length; i++) x[i] /= sum;

            //double[] dIndex = new double[end - start];
            //for (int i = 0; i < dIndex.Length; i++) { dIndex[i] = (double)index[i]; }
            //Analyze(dIndex, out DescriptiveResult desc);

            double centroid = 0.0d;
            for (int i = 0; i < x.Length; i++)
            {// inner_product of x and index
                centroid += x[i] * index[i];
            }


            float bandwidth = 0.0f;
            float skew = 0.0f;
            float kurtosis = 0.0f;

            for (int i = 0; i < x.Length; ++i)
            {
                float delta = (float)(index[i] - centroid);
                float tmp = delta * delta * x[i];
                bandwidth += tmp;
                tmp *= delta;
                skew += tmp;
                tmp *= delta;
                kurtosis += tmp;
            }

            bandwidth = (float)Math.Sqrt(bandwidth);
            skew = (float)(bandwidth > float.Epsilon ? (skew / Math.Pow(bandwidth, 3.0d)) : 0.0f);
            kurtosis=(float)(bandwidth>float.Epsilon?(kurtosis/Math.Pow(bandwidth,4.0d)):3.0d);
            kurtosis -= 3.0f;
            result.Add((float)centroid);
            result.Add(bandwidth); 
            result.Add(skew);
            result.Add(kurtosis);


        }

        private static float Centroid(List<float> data)
        {
            double sum = 0.0d;
            double scale = 0.0d;
            for (int i = 0; i < data.Count; i++)
            {
                sum += data[i] * i;
                scale += data[i];
            }
            return (float)(sum / scale);
        }

       

        public static void Quantiles(List<float> data, int start, int end, ref List<float> result)
        {
            
            float[] x = new float[end - start];
            data.ToList().CopyTo(start, x, 0, end - start);
            List<float> tmp = new List<float>();
            float acc = 0.0f;
            foreach (var val in x)
            {
                acc += val;
                tmp.Add(acc);
            }


            float sum = tmp.Last();

            float[] quantiles = new float[] { 0.025f, 0.25f, 0.5f, 0.75f, 0.975f };
            List<float> freq = new List<float>();
            foreach (var q in quantiles)
            {
                float threshold = q * sum;
                var it = tmp.IndexOf(tmp.Where(y => y >= threshold).First());
                freq.Add(it);
                result.Add(it);
            }
            result.Add(freq[4] - freq[0]);
            result.Add(freq[3] - freq[1]);


        }

        public static float GiniImpurity(List<float> data,int start,int end)
        {
            var fData = data.GetRange(start, end - start);
            float sum=fData.Sum();
            float gini = 1;
            if (sum > float.Epsilon)
            {
                foreach(var f in fData)
                {
                    float p = f / sum;
                    gini -= p * p;
                }
            }
            return gini;
        }

        public static void ExpSmoothSpectrum(ref List<float> data,int start,int end,float gain)
        {
            float alpha = 1 - gain;
            float beta = gain;

            int N = end - start;
            int it = end;
            float Prev = data[--it];

            for(int i=0;i<N-1;i++)
            {
                --it;
                data[it] = (alpha * data[it]) + (beta * Prev);
                Prev = data[it];
            }
            it = start;
            Prev = data[it];
            for(int i = 0; i < N; i++)
            {
                ++it;
                data[it]=(alpha * data[it]) + (beta*Prev);
                Prev= data[it];
            }
        }

        public static void MaskSpectrum(ref List<float> data, int start, int end, int width)
        {
            float maxData = float.MinValue;



            int max_it = -1;


            for (int i = start; i < end; i++) if (data[i] > maxData) { maxData = data[i]; max_it = i; }
            if (max_it >= end) return;

            int dist = end - max_it;
            if (dist > width)
            {
                int it = max_it;
                it += width;
                while (++it != end)
                {
                    data[it] = 0.0f;
                }
            }

            dist = max_it - start;
            if (dist > width)
            {
                int it = max_it;
                it -= width;
                while (--it != start)
                {
                    data[it] = 0.0f;
                }
            }
        }

       

        /// <summary>
        /// Partitions the given list around a pivot element such that all elements on left of pivot are <= pivot
        /// and the ones at thr right are > pivot. This method can be used for sorting, N-order statistics such as
        /// as median finding algorithms.
        /// Pivot is selected ranodmly if random number generator is supplied else its selected as last element in the list.
        /// Reference: Introduction to Algorithms 3rd Edition, Corman et al, pp 171
        /// </summary>
        private static int Partition<T>(this IList<T> list, int start, int end, Random? rnd = null) where T : IComparable<T>
        {
            if (rnd != null)
                list.Swap(end, rnd?.Next(start, end + 1)??0);

            var pivot = list[end];
            var lastLow = start - 1;
            for (var i = start; i < end; i++)
            {
                if (list[i].CompareTo(pivot) <= 0)
                    list.Swap(i, ++lastLow);
            }
            list.Swap(end, ++lastLow);
            return lastLow;
        }

        /// <summary>
        /// Returns Nth smallest element from the list. Here n starts from 0 so that n=0 returns minimum, n=1 returns 2nd smallest element etc.
        /// Note: specified list would be mutated in the process.
        /// Reference: Introduction to Algorithms 3rd Edition, Corman et al, pp 216
        /// </summary>
        public static T NthOrderStatistic<T>(this IList<T> list, int n, Random? rnd = null) where T : IComparable<T>
        {
            return NthOrderStatistic(list, n, 0, list.Count - 1, rnd);
        }
        private static T NthOrderStatistic<T>(this IList<T> list, int n, int start, int end, Random? rnd) where T : IComparable<T>
        {
            while (true)
            {
                var pivotIndex = list.Partition(start, end, rnd);
                if (pivotIndex == n)
                    return list[pivotIndex];

                if (n < pivotIndex)
                    end = pivotIndex - 1;
                else
                    start = pivotIndex + 1;
            }
        }

        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            if (i == j)   //This check is not required but Partition function may make many calls so its for perf reason
                return;
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        /// <summary>
        /// Note: specified list would be mutated in the process.
        /// </summary>
        public static T Median<T>(this IList<T> list) where T : IComparable<T>
        {
            return list.NthOrderStatistic((list.Count - 1) / 2);
        }

        public static double Median<T>(this IEnumerable<T> sequence, Func<T, double> getValue)
        {
            var list = sequence.Select(getValue).ToList();
            var mid = (list.Count - 1) / 2;
            return list.NthOrderStatistic(mid);
        }

        public static string NumberToString(int num)
        {
            return (num.ToString());
        }

        public static void Filter(ref List<float> alldata,FilterParams? filterParams,int sampleRate)
        {
            bool? hpRunTwice = false;
            bool? lpRunTwice = false;
            if (filterParams != null)
            {
                hpRunTwice = (filterParams.HighPassFilterIterations == 2);
                lpRunTwice = (filterParams.LowPassFilterIterations == 2);
                int hiCutoff = filterParams.LowPassFilterFrequency;

                var filterLP = BiQuadFilter.LowPassFilter(sampleRate, hiCutoff, 1);

                for (int i = 0; i < alldata.Count; i++)
                {
                    alldata[i] = filterLP.Transform(alldata[i]);
                }
                if (lpRunTwice ?? false)
                {
                    for (int i = 0; i < alldata.Count; i++)
                    {
                        alldata[i] = filterLP.Transform(alldata[i]);
                    }
                }



                int lowCutoff = filterParams.HighPassFilterFrequency;


                var filterHP = BiQuadFilter.HighPassFilter(sampleRate, lowCutoff, 1);
                for (int i = 0; i < alldata.Count; i++)
                {
                    alldata[i] = filterHP.Transform(alldata[i]);
                }
                if (hpRunTwice ?? false)
                {
                    for (int i = 0; i < alldata.Count; i++)
                    {
                        alldata[i] = filterHP.Transform(alldata[i]);
                    }
                }
            }
        }


    }

    public struct DescriptiveResult
    {
        public uint Count;
        public double Min;
        public double Max;
        public double Range;
        public double Sum;
        public double Mean;
        public double GeometricMean;
        public double HarmonicMean;
        public double Variance;
        public double StdDev;
        public double Skewness;
        public double Kurtosis;
        public double IQR;
        public double Median;
        public double FirstQuartile;
        public double ThirdQuartile;
        public double SumOfError;
        public double SumOfErrorSquare;
        /// <summary>

    }

    public class FilterParams
    {
        public int HighPassFilterFrequency { get; set; } = 15000;
        public int LowPassFilterFrequency { get; set; } = 192000;

        public double FilterQ { get; set; } = 1.0d;

        public int HighPassFilterIterations { get; set; } = 1;
        public int LowPassFilterIterations { get; set; } = 1;
    }
}
