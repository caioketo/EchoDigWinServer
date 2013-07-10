using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LumiSoft.Media.Wave;

namespace EchoDigWinServer
{
    public class AudioIn
    {
        public Server server;
        private WaveIn m_pSoundReceiver = null;
        private byte[] bufferToProccess;

        public AudioIn(Server _server)
        {
            server = _server;
        }

        public void Start()
        {
            // G711 needs 8KHZ 16 bit 1 channel audio, 
            // 400kb buffer gives us 25ms audio frame.
            m_pSoundReceiver = new WaveIn(WaveIn.Devices[0],8000,16,1,400);
            m_pSoundReceiver.BufferFull += new BufferFullHandler 
                                             (m_pSoundReceiver_BufferFull);
            m_pSoundReceiver.Start();
        }

        /// <summary>
        /// This method is called when recording buffer is full 
        /// and we need to process it.
        /// </summary>
        /// <param name="buffer">Recorded data.</param>
        private void m_pSoundReceiver_BufferFull(byte[] buffer)
        {
            // Just store audio data or stream it over the network ... 
            bufferToProccess = buffer;

            double sum = 0;
            for (var i = 0; i < bufferToProccess.Length; i = i + 2)
            {
                double sample = BitConverter.ToInt16(bufferToProccess, i) / 32768.0;
                sum += (sample * sample);
            }
            double rms = Math.Sqrt(sum / (bufferToProccess.Length / 2));
            var decibel = 20 * Math.Log10(rms);

            foreach (WebConnection conn in server.connections)
            {
                conn.Send(bufferToProccess);
            }
        }
    }
}
