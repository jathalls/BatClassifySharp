using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatClassifySharp
{
    internal class Blob
    {
        public Blob() { Clear(); }

        public Blob(int id)
        {
            Clear();
            _id = id;
        }

        public int ID() { return _id; }

        public (int minx,int maxx) X_Limits() { return _index; }



        public int Area() { return _area; }

        public float Magnitude() { return _magnitude; }

        public void Clear()
        {
            _id = 0;
            _index.Item1 = int.MaxValue;
            _index.Item2 = 0;
            _area = 0;
            _magnitude = 0.0f;
        }

        public void Push(int xVal, float mag = 0.0f)
        {
            _index.minx = xVal < _index.minx ? xVal : _index.minx;
            _index.maxx = xVal > _index.maxx ? xVal : _index.maxx;
            ++_area;
            _magnitude += mag;

            length = _index.maxx + 1  - _index.minx;
            int spectra = length / 128; //number of pixels divided by height to get number of spectra
            int samples = spectra * 128; //number of spectra * FFT advance per spectrum
            duration = (length*1000.0f) / 500000.0f; // divide by sampe rate to get duration in secs, *1000 to get ms
        }

        internal void BumpMagnitude()
        {
            _magnitude += float.Epsilon;
        }

        private int _id=0;
        private (int minx,int maxx) _index=(int.MaxValue,0);
        private int _area=0;
        private float _magnitude=0.0f;
        public float duration;
        public int length;
    }
}
