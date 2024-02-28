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
