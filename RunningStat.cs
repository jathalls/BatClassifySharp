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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatClassifySharp
{
    internal class RunningStat
    {
        public RunningStat() { }

        public void Clear()
        {
            m_n = 0;
        }

        public void Push(double x)
        {
            ++m_n;

            if (m_n == 1)
            {
                m_oldM = x;
                m_newM = x;
                m_oldS = 0.0;
            }
            else
            {
                m_newM = m_oldM + (x - m_oldM) / m_n;
                m_newS = m_oldS + (x - m_oldM) * (x - m_newM);

                m_oldM = m_newM;
                m_oldS = m_newS;
            }
        }

        public int NumDataValues() { return m_n; }

        public double Mean()
        {
            return (m_n > 0 ? m_newM : 0.0);
        }

        public double Variance()
        {
            return (m_n > 1 ? m_newS / (m_n - 1) : 0.0);
        }

        public double StandardDeviation()
        {
            return(Math.Sqrt(Variance()));
        }

        private int m_n=0;
        
        private double m_oldM;
        private double m_newM;
        private double m_oldS;
        private double m_newS;
    }
}
