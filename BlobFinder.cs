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

*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatClassifySharp
{
    /// <summary>
    /// // Algorithm (Chang et al. 2003)
    /// "A linear-time component labeling algorithm using contour tracing technique"

    /// Code adapted from
    /// https://github.com/bramp/Connected-component-labelling/blob/master/connected-component-labelling.js
    /// Andrew Brampton (2011)
    /// </summary>
    internal class BlobFinder
    {
        
        private (int,int) Tracer(ref mImage data,int S,int p)
        {
            for(int d = 0; d < 8; d++)
            {
                int q = (p + d) % 8;
                int T = S + _pos[(int)q];

                if (T < 0 || T >= _max) continue;

                if (data.getPixel(T) >= 0.000001) return (T, q);

                _label[(int)T] = MARKED;
            }
            return (S, -1);
        }

        private void ContourTracing(ref mImage data,int S, int C,bool external)
        {
            int p = external ? 7 : 3;

            (int,int) tmp = Tracer(ref data, S, p);
            int T2 = tmp.Item1;
            int q = tmp.Item2;

            _label[(int)S] = C;
            if(!blobMap.ContainsKey(C)) { blobMap.Add(C, new Blob()); }
            blobMap[C].Push(S, data.getPixel(S));

            if (T2 == S)
            {
                return;

            }

            int Tnext = T2;
            int T = T2;

            while(T!=S || Tnext != T2)
            {
                _label[(int)Tnext] = C;
                if (!blobMap.ContainsKey(C)) blobMap.Add(C, new Blob());
                blobMap[C].Push(Tnext, data.getPixel(Tnext));
                if (blobMap[C].Area() > 2000) break;
                T = Tnext;
                p=(q+5) % 8;
                (int,int) tmp2 = Tracer(ref data, T, p);
                Tnext = tmp2.Item1;
                q=tmp2.Item2;
            }
        }

        private void Extract(ref mImage data)
        {
            int y = 1;
            do
            {
                int x = 0;
                do
                {
                    int offset = y * _w + x;
                    if (data.getPixel(offset) < .000001f) continue;

                    //Step1
                    if(data.getPixel(offset-_w)<.000001f && _label[offset] == UNSET)
                    {
                        ContourTracing(ref data, offset, _c, true);
                        ++_c;
                        //traced=true;
                    }

                    //Step2
                    if (data.getPixel(offset + _w) < 0.000001 && _label[(int)(offset + _w)] == UNSET)
                    {
                        int n = _label[(int)(offset - 1)];
                        if (_label[(int)offset] != UNSET) n = _label[(int)offset];

                        ContourTracing(ref data, offset, n, false);
                        //traced=true;
                    }

                    //Step3
                    if (_label[(int)offset] == UNSET)
                    {
                        int n = _label[ (int)(offset - 1)];
                        _label[(int)offset] = n;
                        if (!blobMap.ContainsKey(n)) blobMap.Add(n, new Blob());
                        blobMap[n].Push(offset, data.getPixel(offset));
                    }
                } while (++x < _w);
            } while (++y < _h - 1);
        }

        private int _w=0;
        private int _max=0;
        private int  _h = 0, _c=0;
        private List<int> _label=new List<int>();
        private List<int> _pos=new List<int>();
        private static readonly int UNSET = 0;
        private static readonly int MARKED = -1;
        private Dictionary<int, Blob> blobMap=new Dictionary<int, Blob>();

        public BlobFinder() 
        {
            _w = 0; _h = 0; _max = 0; _c = 0;

        }

        public Dictionary<int, Blob> Extraction(ref mImage data)
        {
            //data.Scale();

            Dictionary<int,Blob> result = new Dictionary<int,Blob>();
            _w = data.Height();
            _h = data.Width();
            _max = _w * _h;
            _pos=new int[8].ToList();
            _pos[0] = 1;
            _pos[1] = _w + 1;
            _pos[2] = _w;
            _pos[3] = _w - 1;
            _pos[4] = -1;
            _pos[5] = -_w - 1;
            _pos[6] = -_w;
            _pos[7] = -_w + 1;

            _label.Clear();
            _label = new int[_max].ToList();
            _c = 1;
            blobMap.Clear();

            // We change the border to be white. We could add a pixel around
            // but we are lazy and want to do this in place.
            // Set the outer rows/cols to background
            for (int j = 0; j < _w; ++j)
            {
                data.setPixel(j, 0.0f);
                data.setPixel((_h - 1) * _w + j, 0.0f);
            }
            for (int i = 0; i < _h; ++i)
            {
                data.setPixel(i * _w, 0.0f);
                data.setPixel(i * _w + (_w - 1), 0.0f);
            }

            Extract(ref data);
            return blobMap;


            
        }

        public void Mask(ref mImage data,(int,Blob) blob,ref mImage segment)
        {
            int height = data.Height();
            var xLimits = blob.Item2.X_Limits();
            int index = height * (int)Math.Floor(xLimits.minx / (float)height);
            int frames=1+(int)Math.Ceiling((xLimits.maxx-xLimits.Item1)/(float)height);
            int counter = 0;
            segment.Create(frames, height);
            for(var i=0;i< frames; ++i)
            {
                for(var j=0;j< height;j++)
                {
                    if (_label[counter+index]==blob.Item1)
                        segment.setPixel(counter,data.getPixel(counter+index));
                    ++counter;
                }
            }
        }

    }
}
