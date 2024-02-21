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
