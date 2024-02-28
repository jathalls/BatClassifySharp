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
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace BatClassifySharp
{
    internal class AudioDecoder
    {
        public AudioDecoder() { }   

        public bool FileSupported(string filename)
        {
            bool result = false;
            string file=Path.GetFileName(filename);
            
            string ext=Path.GetExtension(file);

            if (ext.ToUpper().Contains("WAV"))
            {
                
                    if( File.Exists(filename))
                    {
                        result = true;
                    }
                    
                }
            
            return (result);
        }

        public void ReadFile(string filename,  ref List<float> data, float startTime = 0.0f, float segmentDuration = -1.0f,FilterParams? filterParams=null)
        {
            data = new List<float>();
            int sampleRate = 0; ;
            try
            {

                


                float[] buffer = new float[1024];
                //byte[] bbuffer=new byte[2048];
                using (WaveFileReader wavReader = new WaveFileReader(filename))
                {
                    var wavFormat = wavReader.WaveFormat;
                    sampleRate = wavFormat.SampleRate;
                    if (sampleRate < 192000) sampleRate *= 10; // assume time-expanded x10
                    int internalRate = 500000;
                    float ratio = (float)internalRate / (float)sampleRate;
                    ISampleProvider sampleProvider = wavReader.ToSampleProvider();
                    double max = double.MinValue;
                    Debug.WriteLine($"start={startTime}=>{(int)Math.Floor(startTime)} duration={segmentDuration}");
                    wavReader.Skip((int)Math.Floor(startTime));
                    if (segmentDuration > 30.0f) segmentDuration = 30.0f;
                    if (segmentDuration < 0.0f) segmentDuration = 30.0f;
                    int targetSamples = (int)(segmentDuration * internalRate);
                    Debug.WriteLine($"target samples={targetSamples}");
                    if (internalRate != sampleRate)
                    {
                        var outFormat = new WaveFormat(internalRate, wavFormat.Channels);
                        using (var resampler = new MediaFoundationResampler(wavReader, outFormat))
                        {
                            sampleProvider = resampler.ToSampleProvider();
                            int read = 1;
                            while (read > 0)
                            {
                                read = sampleProvider.Read(buffer, 0, buffer.Length);
                                if (read > 0)
                                {
                                    data.AddRange(buffer);
                                    if (buffer.Max() > max) max = data.Max();
                                }
                                if (data.Count > (targetSamples)) break;// no more than 30s
                            }
                        }
                    }
                    else
                    {
                        int read = 1;
                        while (read > 0)
                        {
                            read = sampleProvider.Read(buffer, 0, buffer.Length);
                            if (read > 0)
                            {
                                data.AddRange(buffer);
                                if (buffer.Max() > max) max = data.Max();
                            }
                            if (data.Count > targetSamples) break;
                        }
                    }

                    if(filterParams != null)
                    {
                        Tools.Filter(ref data, filterParams,internalRate);
                    }

                    Debug.WriteLine($"Max data={max}");
                    for (int i = 0; i < data.Count; i++) { data[i] = (float)((double)data[i] / max); }


                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

            }
            
        }

        
    }
}
