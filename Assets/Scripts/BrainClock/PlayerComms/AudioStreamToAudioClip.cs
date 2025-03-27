using Steamworks;
using System.IO;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    /// <summary>
    /// Converts an audio stream into clip data for an AudioSource.
    /// Expects Steam to be initialized uses Steam capture Rate data and decompression.
    /// </summary>
    public class AudioStreamToAudioClip : MonoBehaviour, IAudioStreamReceiver
    {
        public AudioSource AudioSource;

        public string AudioClipName;

        private MemoryStream uncompressedStream;
        private MemoryStream compressedStream;

        private float[] audioclipBuffer;
        private int audioclipBufferSize;

        private int playbackBuffer;
        private int dataPosition;
        private int dataReceived;

        [Tooltip("Steam AppId you want to initialize")]
        public uint AppId = 0;

        public int dataRate;

        public bool IsReady = false;

        private void Awake()
        {
            uncompressedStream = new MemoryStream();
            compressedStream = new MemoryStream();

            if (!SteamClient.IsValid)
                return;
        }

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("AudioStreamToAudioClip checking Steam");
            // Try to initialize Steam if not done already.
            if (!SteamClient.IsValid)
                SteamClient.Init(AppId, true);

            if (!SteamClient.IsValid)
                return;

            Debug.Log("AudioStreamToAudioClip.Start() Steam is valid");

            IsReady = true;

            dataRate = (int)SteamUser.OptimalSampleRate;

            audioclipBufferSize = dataRate * 5;
            audioclipBuffer = new float[audioclipBufferSize];

            // Here optimalRate * 2 seems to be what fixes the playback issues
            AudioSource.clip = AudioClip.Create(AudioClipName, dataRate * 2, 1, dataRate, true, OnAudioRead, null);
            AudioSource.loop = true;
            AudioSource.Play();
        }

        /// <summary>
        /// Processing version using Bytes (Network messages will enter here).
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        public void ReceiveAudioStreamData(byte[] data, int length)
        {
            if (data == null || length == 0)
                return;

            // Create the clip again if it has been destroyed.
            if (AudioSource.clip == null)
            {
                Debug.Log("AudioClip empty, creating one");
                // Here optimalRate * 2 seems to be what fixes the playback issues
                AudioSource.clip = AudioClip.Create(AudioClipName, dataRate * 2, 1, dataRate, true, OnAudioRead, null);
                AudioSource.loop = true;
                AudioSource.enabled = true;
                AudioSource.Play();
            }

            // Convert byteArray into stream and reset pointer to the stream start
            compressedStream.Position = 0;
            compressedStream.Write(data, 0, length);
            compressedStream.Position = 0;

            // Decompress audio int output stream.
            uncompressedStream.Position = 0;
            int uncompressedWritten = SteamUser.DecompressVoice(compressedStream, length, uncompressedStream);

            // Convert to byte Array
            byte[] outputBuffer = uncompressedStream.GetBuffer();
            if (outputBuffer != null &&  outputBuffer.Length > 0)
                WriteToClip(outputBuffer, uncompressedWritten);
        }

        /// <summary>
        /// Processing version using Stream (faster for testing, only local use).
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        public void ReceiveAudioStreamData(MemoryStream stream, int length)
        {
            if (length == 0)
                return;

            // Decompress audio int output stream.
            uncompressedStream.Position = 0;
            int uncompressedWritten = SteamUser.DecompressVoice(stream, length, uncompressedStream);

            // Convert to byte Array
            byte[] outputBuffer = uncompressedStream.GetBuffer();
            if (outputBuffer != null && outputBuffer.Length > 0)
                WriteToClip(outputBuffer, uncompressedWritten);
        }

        /// <summary>
        /// Convert bytes data to float audio signal
        /// </summary>
        /// <param name="data"></param>
        /// <param name="iSize"></param>
        private void WriteToClip(byte[] data, int iSize)
        {
            for (int i = 0; i < iSize; i += 2)
            {
                // insert converted float to buffer
                float converted = (short)(data[i] | data[i + 1] << 8) / 32767.0f;
                audioclipBuffer[dataReceived] = converted;

                // buffer loop
                dataReceived = (dataReceived + 1) % audioclipBufferSize;

                playbackBuffer++;
            }
        }

        /// <summary>
        /// Adjust clip audio position to accommodate for the new data
        /// </summary>
        /// <param name="data"></param>
        private void OnAudioRead(float[] data)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                // start with silence
                data[i] = 0;

                // do I  have anything to play?
                if (playbackBuffer > 0)
                {
                    // current data position playing
                    dataPosition = (dataPosition + 1) % audioclipBufferSize;

                    data[i] = audioclipBuffer[dataPosition];

                    // Adjust playBack buffer offset
                    playbackBuffer--;
                }
            }

        }


    }
}
