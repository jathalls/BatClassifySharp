using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatClassifySharp
{
    internal class STFT
    {
        public STFT() 
        {
            _fftSize = 512;
            _stepSize = 128;
            fft.SetSize(ref _fftSize);
        }

        public STFT(int fftSize,int stepSize) 
        { 
            fft.SetSize(ref fftSize);
            _fftSize= fftSize;
            _stepSize= stepSize;

        }

        public void CreateSpectrogram(ref List<float> samples, ref mImage spectro) 
        {
            List<float> maxima=new List<float>();
            int height = (int)(_fftSize * 0.25);
            int width = (int)Math.Ceiling((decimal)samples.Count /(decimal)_stepSize);
            spectro.Create(width, height);
            
            List<float> spectrum = new float[height].ToList();
            int index = 0;
            for(int x = 0; x < width; ++x, index += _stepSize)
            {
                spectrum = fft.Process(ref samples, index);
                for(int y = 0; y < 12; ++y)
                {
                    spectro.setPixel(x, y, 0.0f);
                }
                for (int y = 12; y < height; ++y)
                {
                    spectro.setPixel(x, y, spectrum[y]);
                    
                }
                float max = spectrum.Max();
                maxima.Add(max);
                //Debug.WriteLineIf(max > 0.5f, $"max={max} at x={x} {x / 3906.25f}secs");
            }
            //Debug.WriteLine($"Max in spectrogram={maxima.Max()}");

            

        }

        private FFT fft=new FFT();

        private int _fftSize;

        private int _stepSize;
    }
}
