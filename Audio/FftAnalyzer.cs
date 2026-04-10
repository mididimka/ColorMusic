using System;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace ColorMusic.Audio
{
    public class FftAnalyzer
    {
        private readonly int _fftSize;
        private readonly float[] _sampleBuffer;
        private int _bufferPos;

        public int SampleRate { get; }

        public event Action<double[]>? OnSpectrumReady;

        public FftAnalyzer(int sampleRate, int fftSize = 4096)
        {
            SampleRate = sampleRate;
            _fftSize = fftSize;
            _sampleBuffer = new float[_fftSize];
        }

        public void AddSamples(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return;

            foreach (var s in samples)
            {
                _sampleBuffer[_bufferPos++] = s;

                if (_bufferPos >= _fftSize)
                {
                    ComputeFFT();
                    _bufferPos = 0;
                }
            }
        }

        private void ComputeFFT()
        {
            Complex[] data = new Complex[_fftSize];

            for (int i = 0; i < _fftSize; i++)
            {
                double w = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (_fftSize - 1)));
                data[i] = new Complex(_sampleBuffer[i] * w, 0);
            }

            Fourier.Forward(data, FourierOptions.Matlab);

            int spectrumSize = _fftSize / 2;
            double[] spectrum = new double[spectrumSize];

            for (int i = 0; i < spectrumSize; i++)
                spectrum[i] = data[i].Magnitude;

            OnSpectrumReady?.Invoke(spectrum);
        }

        // Как в первом варианте: средняя амплитуда диапазона
        public double GetBandAmplitude(double[] spectrum, double minHz, double maxHz)
        {
            int minIndex = (int)(minHz * spectrum.Length / (SampleRate / 2.0));
            int maxIndex = (int)(maxHz * spectrum.Length / (SampleRate / 2.0));

            minIndex = Math.Clamp(minIndex, 0, spectrum.Length - 1);
            maxIndex = Math.Clamp(maxIndex, 0, spectrum.Length - 1);

            if (maxIndex <= minIndex)
                return 0;

            double sum = 0;
            int count = 0;

            for (int i = minIndex; i <= maxIndex; i++)
            {
                sum += spectrum[i];
                count++;
            }

            return count == 0 ? 0 : sum / count;
        }
    }
}