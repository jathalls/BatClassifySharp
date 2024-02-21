using NAudio.CoreAudioApi.Interfaces;
using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;




namespace BatClassifySharp
{
    internal class FFT
    {
        public FFT() 
        { 

        }

        /// <summary>
        /// set up the fftw plan
        /// </summary>
        /// <param name="fftSize"></param>
        public void SetSize(ref int fftSize)
        {
            this.fft_size = fftSize;
            window_function=new float[fft_size].ToList<float>();
            float window_sum = BlackmanHarris(ref window_function);
            normalise = 1.0f / window_sum;
            fft_bins = fftSize /2;
            original=new float[fft_size].ToList();
            transformed=new float[fft_size].ToList();
            spectral_magnitude=new float[fft_bins].ToList();
            spectral_magnitude[0] = 0.0f;
            

        }

        /*
         * List<float> paddedData = new List<float>();
            for (int i = 0; i < FFTSize; i++)
            {
                if (i < data.Count())
                {
                    paddedData.Add(data[i]);
                }
                else
                {
                    paddedData.Add(0.0f);
                }
            }
            data = paddedData.ToArray();

            for (int i = 0; i < FFTSize; i++)
            {
                rawFFT[i].X = (float)(data[i] * scale * FastFourierTransform.HammingWindow(i, FFTSize));
                rawFFT[i].Y = 0.0f;
            }
            FastFourierTransform.FFT(true, FFTOrder, rawFFT);
            double spectSum = 0.0d;
            for (int i = 0; i < FFTSize / 2; i++)
            {
                fft[i] = Math.Sqrt((rawFFT[i].X * rawFFT[i].X) + (rawFFT[i].Y * rawFFT[i].Y));
                spectSum += fft[i];
                if (fft[i] > PeakValue)
                {
                    PeakValue = fft[i];
                    PeakBin = i;
                    PeakFrequency = i * HzPerBin;
                }
            }
        */

        public List<float> Process(ref List<float> samples,int index)
        {
            int N = samples.Count;
            Complex[] data= new Complex[fft_size];
            for(int i=0; i <fft_size; i++,index++)
            {
                original[i] = index < N ? samples[(int)index] : 0.0f;
                data[i].X = (float)(original[i] * FastFourierTransform.BlackmannHarrisWindow(i, fft_size));
                data[i].Y = 0.0f;
            }
            //float bh=BlackmanHarris(ref  original);
            FastFourierTransform.FFT(true, Order(fft_size), data);

            index = fft_size;
            for(int i = 0; i < fft_bins; i++)
            {
                --index;
                float real1=  (float)Math.Sqrt((data[i].X * data[i].X) + (data[i].Y * data[i].Y));
                float real2 = (float)Math.Sqrt((data[index].X * data[index].X) + (data[index].Y * data[index].Y));
                    float real = (float)Math.Sqrt((real1 * real1) + (real2 * real2));
                spectral_magnitude[i] = normalise * (float)Math.Abs(real);
                //spectral_magnitude[i] = 20.0f * (float)Math.Log10(spectral_magnitude[i]);
            }

            return (spectral_magnitude);
        }

        private int Order(int fftSize)
        {
            switch (fftSize)
            {
                case 2048: return 11;
                case 1024: return 10;
                case 512: return 9;
                case 256: return 8;
                case 128: return 7;
                default:
                    {
                        fft_size = 512;
                        fft_bins = 256;
                        return 9;                     }
            }
            /*
            int order = 1;
            int Size = 2;
            while (fftSize <= Size)
            {
                order++;
                Size *= 2;
            }
            fft_size = Size;
            fft_bins = Size / 2;
            return order;*/
        }

        public int Size()
        {
            return (fft_size);
        }

        public List<float> Spectrum()
        {
            return (spectral_magnitude);
        }

        private float BlackmanHarris(ref List<float> data)
        {
            int N = data.Count;
            float arg = (float)(8.0f * Math.Atan(1.0f) / (N - 1));
            float sum = 0;
            for(int i=0;i<N;i++)
            {

                data[i] = (float)(0.35875f - 0.48829f * Math.Cos(arg * i) + 0.14128f * Math.Cos(2 * arg * i) - 0.01168 * Math.Cos(3 * arg * i));
                sum += data[i];
            }
            return (sum);
        }

        private List<float> window_function=new List<float>();

        private int fft_size=0;

        private int fft_bins=0;

        private List<float> original=new List<float>();

        private List<float> transformed = new List<float>();

        private List<float> spectral_magnitude = new List<float>();

        private float normalise=0.0f;

        //private fftwf_plan plan;
    }
}
