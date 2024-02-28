
/*************************************************************************

  Copyright 2024 Justin A T halls (jathalls@gmail.com)
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

*************************************************************************/using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatClassifySharp
{
    public class Blob
    {
        public Blob() { Clear(); }

        public Blob(int id)
        {
            Clear();
            _id = id;
        }

        public int ID() { return _id; }

        /// <summary>
        /// x-limits of the blob in spectra
        /// </summary>
        /// <returns></returns>
        public (int minx,int maxx) X_Limits() { return _index; }


        /// <summary>
        /// total area of the pulse in the blob
        /// </summary>
        /// <returns></returns>
        public int Area() { return _area; }

        /// <summary>
        /// the magnitude of the blob
        /// </summary>
        /// <returns></returns>
        public float Magnitude() { return _magnitude; }

        /// <summary>
        /// clears the contained values
        /// </summary>
        public void Clear()
        {
            _id = 0;
            _index.Item1 = int.MaxValue;
            _index.Item2 = 0;
            _area = 0;
            _magnitude = 0.0f;
        }

        /// <summary>
        /// Adds a pixel to the blob, increasing the area by 1 and the magnitude by the value of the pixel.
        /// If the x-value is beyond the blob the x-limits of the blob are adjusted
        /// </summary>
        /// <param name="xVal"></param>
        /// <param name="mag"></param>
        public void Push(int xVal, float mag = 0.0f)
        {
            _index.minx = xVal < _index.minx ? xVal : _index.minx;
            _index.maxx = xVal > _index.maxx ? xVal : _index.maxx;
            ++_area;
            _magnitude += mag;

            length = _index.maxx + 1  - _index.minx;
            int spectra = length / 128; //number of pixels divided by height to get number of spectra
            int samples = spectra * 128; //number of spectra * FFT advance per spectrum
            duration = (length*1000.0f) / 500000.0f; // divide by sample rate to get duration in secs, *1000 to get ms
            //var startInSpectra = _index.minx / 128; // divide pixelPosition by height to get x in spectra
            //var startInSamples = startInSpectra * 128; // multiply by sample advance per spectrum to get x in samples
            startInMs=(_index.minx*1000)/ 500000.0f;
        }

        /// <summary>
        /// increases the magnitude by a minimal amount to prevent division by zero errors
        /// </summary>
        internal void BumpMagnitude()
        {
            _magnitude += float.Epsilon;
        }

        private int _id=0;
        private (int minx,int maxx) _index=(int.MaxValue,0);
        private int _area=0;
        private float _magnitude=0.0f;

        public float startInMs { get; set; } = 0.0f;
        /// <summary>
        /// duration of the blob in ms
        /// </summary>
        public float duration;

        /// <summary>
        /// length of the blob in total number of pixels. Divide by height (128) to get number of spectra
        /// </summary>
        public int length;
    }
}
