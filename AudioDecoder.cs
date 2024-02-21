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

        public void ReadFile(string filename, ref List<float> data)
        {
            data = new List<float>();
            int sampleRate = 0; ;
            try
            {

                


                float[] buffer = new float[1024];
                byte[] bbuffer=new byte[2048];
                WaveFileReader wavReader = new WaveFileReader(filename);
                var wavFormat = wavReader.WaveFormat;
                sampleRate = wavFormat.SampleRate;
                if (sampleRate < 192000) sampleRate *= 10; // assume time-expanded x10
                int internalRate = 500000;
                float ratio = (float)internalRate / (float)sampleRate;
                ISampleProvider sampleProvider = wavReader.ToSampleProvider();
                double max = double.MinValue;
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
                            if (data.Count > (internalRate * 30)) break;// no more than 30s
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
                        if (data.Count > (internalRate * 30)) break;
                    }
                }

                Debug.WriteLine($"Max data={max}");
                for(int i=0;i<data.Count;i++) { data[i] = (float)((double)data[i] / max); }




            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

            }
            if (data.Count > 0 && sampleRate > 19000)
            {

            }
        }

        
    }
}
