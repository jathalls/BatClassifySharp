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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatClassifySharp
{
    public class mImage
    {
        private int width;
        private int height;
        private int N;
        private float[] pixels;
        

        public mImage(int w=0,int h = 0)
        {
            width = w;
            height = h;
            N = width * height;
            pixels = new float[N];
        }

        public mImage(int w,int h, float val)
        {
            width=w;
            height=h;
            N= width * height;
            pixels = new float[N];
        }

        public mImage(ref mImage cSource)
        {
            width=(int)cSource.Width();
            height=(int)cSource.Height();
            N= width * height;
            pixels = new float[N];
        }

        public void Create(int w, int h)
        {
            width = w;
            height = h;
            N= width * height;
            pixels = new float[N];
        }

        public void Clear()
        {
            width = 0;
            height = 0;
            N = 0;
            pixels=new float[N];
        }

        public void SegmentFeatures(out List<float> result)
        {
            result = new List<float>();

            List<float> power_spectrum = new float[height].ToList();
            List<float> temporal_envelope = new float[width].ToList();
            List<float> histogram = new float[2 * height].ToList();
            float area = 0.0f;
            int prev_peak = 0;
            for(int x = 0; x < width; x++)
            {
                int offset = x * height;
                List<float> spectrum = new float[height].ToList();
                float sum = 0.0f;
                for (int y = 0; y < height; y++)
                {
                    spectrum[y] = pixels[offset + y];
                    sum += spectrum[y];
                }
                temporal_envelope[(int)x] = sum;

                int cur_peak = 0;
                if (sum > float.Epsilon)
                {
                    Tools.MaskSpectrum(ref spectrum,0,spectrum.Count, 8);
                    sum = 0;
                    float centroid = 0.0f;
                    for(int y = 1; y < height-1; y++)
                    {
                        power_spectrum[y] += spectrum[y];
                        area += spectrum[y] > 0.000001f ? 1.0f : 0.0f;
                        sum += spectrum[y];
                        centroid += spectrum[y] * y;
                    }
                    centroid /= sum;
                    cur_peak=(int)Math.Round(centroid);
                }
                if(cur_peak>0 && prev_peak > 0)
                {
                    int bin = (cur_peak - prev_peak) + height;
                    if(bin>0 && bin<histogram.Count-1)
                    histogram[bin] += spectrum[cur_peak];
                    histogram[bin - 1] += spectrum[cur_peak - 1];
                    histogram[bin + 1] += spectrum[cur_peak + 1];
                }
                prev_peak = cur_peak;
            }
            result.Add(area);
            Tools.Moments(power_spectrum,0,power_spectrum.Count, ref result);
            var freq = result[1];
            var bw = result[2] > 1.0f ? result[2] : 1.0f;
            float Q = freq / bw;
            result.Add(Q);
            result.Add(Tools.GiniImpurity(power_spectrum,0,power_spectrum.Count));
            Tools.Quantiles(power_spectrum,0,power_spectrum.Count, ref result);
            Tools.Moments(temporal_envelope,0,temporal_envelope.Count, ref result);
            result.Add(Tools.GiniImpurity(temporal_envelope,0,temporal_envelope.Count));
            Tools.Moments(histogram,0,histogram.Count,ref result);
            result.Add(Tools.GiniImpurity(histogram,0,histogram.Count));

            return;
        }

        public mImage Extract(int from, int to)
        {
            mImage roi=new mImage(1+to-from,height);
            int index = 0;

            for(int x = from; x <= to; x++)
            {
                int offset = x * height;
                for(int y= 0; y < height; y++)
                {
                    roi.setPixel(index, pixels[offset+y]);
                    ++index;
                }
            }
            return roi;
            
        }

        public void Load(string filename)
        {
            //Not Implemented
        }

        public void Save(string? filePath,string? fileName,float scale=0.4f)
        {
            filePath=Path.GetDirectoryName(filePath??"");
            Debug.WriteLine($"Save {fileName} to {filePath}, size={width},{height}");
            float peak = 0.0f;
            float max = float.MinValue;
            float min = float.MaxValue;
            int xStart = 0;
            int swidth = width;
            
            
            for (int x = 0; x < width; x++)
            {
                for(int y=0;y<height; y++)
                {
                    max = pixels[x * height + y] > max ? pixels[x * height + y] : max;
                    min = pixels[x * height + y] < min ? pixels[x * height + y] : min;
                }
            }
            peak = max > 0 ? 1.0f / max : 1.0f;
            Debug.WriteLine($"Min={min} Max={max} Peak={peak} Factor={peak*255}");
            peak = 1.0f / max;
            peak *= 255;
            
            int count = 0;
            string extFile = fileName + "--" + count;
            while (File.Exists(Path.Combine(filePath??@"C:\BRMTestData\", extFile + "-0.png")))
            {
                count++;
                extFile = fileName + "--" + count;
            }
            xStart = 0;
            int index = 0;
            using (Bitmap bmp = new Bitmap((width>2000?2000:width) + 1, height))
            {
                do
                {
                    Debug.WriteLine($"Bitmap {count}-{index} start={xStart} {bmp.Width}x{bmp.Height}");
                    int bmpX = 0;
                    for (int x = xStart; x < (2000+xStart) && x < width && bmpX<bmp.Width; x++)
                    {
                        int row = height - 1;
                        for (int y = 0; y < height; y++, row--)
                        {
                            int mag = 255 - (int)Math.Abs((pixels[x * height + y] * peak));
                            if (mag > 255) mag = 255;
                            if(mag<0) mag = 0;
                            bmp.SetPixel(bmpX, row, Color.FromArgb(mag, mag, mag));
                        }
                        bmpX++;
                    }

                    
                    

                    bmp.Save(Path.Combine(filePath ?? "", extFile + $"-{index}.png"), ImageFormat.Png);
                    xStart += 2000;
                    index++;
                } while (xStart < width);
            }
        }

        public void AppendToFile(string fileName)
        {
            //Not Imlemented
        }

        public void VerticalMask(int length = 7)
        {

        }

        public void ContrastBoost()
        {
            float scale = 1.0f / (height - 7.0f);
            float[] spectrum = new float[height];
            for (int x = 0; x < width; x++)
            {
                int offset = x * height;
                pixels[offset] = 0.0f; ;
                pixels[offset + height - 1] = 0.0f;
                pixels[offset + 1] = 0.0f;
                pixels[offset + height - 2] = 0.0f;
                pixels[offset + 2] = 0.0f;
                pixels[offset + height - 3] = 0.0f;
                int a = offset;
                int  s=0;
                for (;a<offset+height; a++, s++) spectrum[s] = pixels[a];
                float sum = spectrum.Sum();
                for(int y = 3; y < height - 3; ++y)
                {
                    pixels[offset + y] -= (sum - spectrum[y] -
                        spectrum[y - 1] - spectrum[y + 1] -
                        spectrum[y + 2] - spectrum[y - 2] -
                        spectrum[y + 3] - spectrum[y - 3]) * scale;
                    pixels[offset + y] = pixels[offset + y] < 0.0f ? 0.0f : pixels[offset + y];
                }
            }
        }

        public void LogCompress(float compression = 20.0f)
        {
            int i;
            for (i = 0; i < N; ++i)
            {
                pixels[i]=(float)Math.Log10(compression* pixels[i]+1);
            }
        }

        public void BackgroundSubtract()
        {
            RunningStat[] stats = new RunningStat[height];
            for(int i=0;i<height;i++) stats[i] = new RunningStat();
            float C = 1.5f;
            for(int x = 0; x < width; x++)
            {
                int offset = x * height;
                for(int y = 0; y < height; y++)
                {
                    float tmp = pixels[offset + y];
                    pixels[offset + y] = Tools.PositiveHalfWaveRectify((float)(tmp - C * stats[y].Mean()));
                    stats[y].Push(tmp);
                }
            }
        }
        
        public void Blur()
        {
            float[] spectrum = new float[height];
            for(int x=0;x<width; x++)
            {
                int offset = x * height;
                for(int y=0;y<height; y++)
                {
                    spectrum[y] = pixels[offset + y];
                }
                for (int y = 1; y < height-1; y++)
                {
                    pixels[offset + y] = 2 * spectrum[y] + spectrum[y - 1] + spectrum[y + 1];
                }
            }
        }

        public void PostMask()
        {
            float alpha = 0.9f;
            float beta = 1.0f - alpha;
            float[] threshold = new float[height];
            for(int x=0;x<width; x++)
            {
                int offset= x * height;
                for(int y = 0; y < height; y++)
                {
                    float tmp = pixels[offset + y];
                    float decayed = alpha * threshold[y] + beta * tmp;
                    threshold[y]=tmp>decayed?tmp:decayed;
                    pixels[offset + y] = (2.0f * tmp) < threshold[y] ? 0.0f : tmp;
                }
            }
        }

        public float getPixel(int x, int y)
        {
            return (pixels[x * height + y]);
        }

        public float getPixel(int index)
        {
            return (pixels[index]);
        }

        public void setPixel(int x, int y, float val)
        {
            pixels[x * height + y] = val;
        }

        public void setPixel(int index,float val)
        {
            pixels[index] = val;
        }

        public int Width() { return width; }

        public int Height() { return height; }

        public int Size() { return(pixels.Length); }

        public float[] Pixels() { return pixels; }

        internal void Scale()
        {
            float max = pixels.Max();
            
            for (int n = 0; n < pixels.Length; n++) pixels[n] = (pixels[n] / max)*255;
        }
    }
}
