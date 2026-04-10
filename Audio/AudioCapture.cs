using System;
using NAudio.Wave;

namespace ColorMusic.Audio
{
    public class AudioCapture : IDisposable
    {
        private WasapiLoopbackCapture? _capture;

        public event Action<float[]>? OnSamples;

        public int SampleRate { get; private set; }
        public int Channels { get; private set; }

        public void Start()
        {
            _capture = new WasapiLoopbackCapture();
            SampleRate = _capture.WaveFormat.SampleRate;
            Channels = _capture.WaveFormat.Channels;

            _capture.DataAvailable += Capture_DataAvailable;
            _capture.StartRecording();
        }

        private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_capture == null) return;

            var format = _capture.WaveFormat;

            // Первый вариант нормально работал, когда устройство было float.
            // Здесь добавляем безопасную обработку stereo->mono.
            if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
            {
                int totalFloatSamples = e.BytesRecorded / 4;
                float[] interleaved = new float[totalFloatSamples];
                Buffer.BlockCopy(e.Buffer, 0, interleaved, 0, e.BytesRecorded);

                if (Channels <= 1)
                {
                    OnSamples?.Invoke(interleaved);
                    return;
                }

                int monoCount = totalFloatSamples / Channels;
                float[] mono = new float[monoCount];

                int src = 0;
                for (int i = 0; i < monoCount; i++)
                {
                    float sum = 0;
                    for (int ch = 0; ch < Channels; ch++)
                        sum += interleaved[src++];

                    mono[i] = sum / Channels;
                }

                OnSamples?.Invoke(mono);
                return;
            }

            // fallback для PCM16
            if (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample == 16)
            {
                int total16 = e.BytesRecorded / 2;
                short[] pcm = new short[total16];
                Buffer.BlockCopy(e.Buffer, 0, pcm, 0, e.BytesRecorded);

                if (Channels <= 1)
                {
                    float[] mono = new float[total16];
                    for (int i = 0; i < total16; i++)
                        mono[i] = pcm[i] / 32768f;

                    OnSamples?.Invoke(mono);
                    return;
                }

                int monoCount = total16 / Channels;
                float[] mixed = new float[monoCount];

                int src = 0;
                for (int i = 0; i < monoCount; i++)
                {
                    float sum = 0;
                    for (int ch = 0; ch < Channels; ch++)
                        sum += pcm[src++] / 32768f;

                    mixed[i] = sum / Channels;
                }

                OnSamples?.Invoke(mixed);
            }
        }

        public void Dispose()
        {
            if (_capture != null)
            {
                try { _capture.StopRecording(); } catch { }
                _capture.Dispose();
                _capture = null;
            }
        }
    }
}