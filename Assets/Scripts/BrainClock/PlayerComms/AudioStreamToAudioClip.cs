using Steamworks;
using System.IO;
using UnityEngine;
using System;

namespace BrainClock.PlayerComms
{
  public class AudioStreamToAudioClip : MonoBehaviour, IAudioStreamReceiver
  {
    public AudioSource AudioSource;
    public string AudioClipName = "VoiceAudio";
    public uint AppId = 544550;
    public int dataRate;

    [Tooltip("Size of audio buffer in seconds")]
    [SerializeField] private int bufferSizeSeconds = 5;
    [SerializeField] private bool logBufferStats = false;

    private readonly object bufferLock = new();
    private CircularBuffer audioCircularBuffer;
    private MemoryStream uncompressedStream;
    private MemoryStream compressedStream;
    private float[] audioClipData;
    private int writePosition;
    private int readPosition;
    private int availableSamples;
    private int samplesProcessed;
    private bool isInitialized;

    private void Awake()
    {
      uncompressedStream = new MemoryStream(8192);
      compressedStream = new MemoryStream(8192);
    }

    void Start()
    {
      InitializeSteam();
      InitializeAudioSystem();
    }

    private void InitializeSteam()
    {
      if (!SteamClient.IsValid)
      {
        SteamClient.Init(AppId, true);
      }
    }

    private void InitializeAudioSystem()
    {
      if (!SteamClient.IsValid) return;

      dataRate = (int)SteamUser.OptimalSampleRate;
      int bufferSize = dataRate * bufferSizeSeconds;

      lock (bufferLock)
      {
        audioCircularBuffer = new CircularBuffer(bufferSize);
        audioClipData = new float[bufferSize];
        writePosition = 0;
        readPosition = 0;
        availableSamples = 0;
      }

      AudioSource.clip = AudioClip.Create(
          AudioClipName,
          bufferSize,
          1,
          dataRate,
          true,
          OnAudioRead
      );

      AudioSource.loop = true;
      AudioSource.Play();
      isInitialized = true;
    }

    public void ReceiveAudioStreamData(byte[] data, int length)
    {
      if (!isInitialized || data == null || length == 0) return;

      try
      {
        compressedStream.SetLength(0);
        compressedStream.Write(data, 0, length);
        compressedStream.Position = 0;

        ProcessCompressedStream(compressedStream, length);
      }
      catch (Exception e)
      {
        Debug.LogError($"Error processing byte array audio data: {e}");
      }
    }

    public void ReceiveAudioStreamData(MemoryStream stream, int length)
    {
      if (!isInitialized || stream == null || length == 0) return;

      try
      {

        stream.Position = 0;
        ProcessCompressedStream(stream, length);
      }
      catch (Exception e)
      {
        Debug.LogError($"Error processing MemoryStream audio data: {e}");
      }
    }

    private void ProcessCompressedStream(MemoryStream stream, int length)
    {
      uncompressedStream.SetLength(0);
      int bytesWritten = SteamUser.DecompressVoice(stream, length, uncompressedStream);

      if (bytesWritten > 0)
      {
        byte[] pcmData = uncompressedStream.GetBuffer();
        ProcessPcmData(pcmData, bytesWritten);
      }
    }

    private void ProcessPcmData(byte[] pcmData, int byteLength)
    {
      int sampleCount = byteLength / 2;
      int samplesWritten = 0;

      lock (bufferLock)
      {
        for (int i = 0; i < byteLength; i += 2)
        {
          float sample = ConvertByteToSample(pcmData, i);
          audioCircularBuffer.Write(sample);
          samplesWritten++;
        }

        availableSamples += samplesWritten;
        samplesProcessed += samplesWritten;

        if (logBufferStats && samplesProcessed % 1000 == 0)
        {
          Debug.Log($"Audio Buffer Stats: " +
                   $"Available: {availableSamples}/{audioCircularBuffer.Capacity} " +
                   $"Processed: {samplesProcessed}");
        }
      }
    }

    private float ConvertByteToSample(byte[] data, int index)
    {

      short rawValue = (short)(data[index] | (data[index + 1] << 8));
      return Mathf.Clamp(rawValue / 32768f, -1f, 1f);
    }

    private void OnAudioRead(float[] data)
    {
      if (!isInitialized) return;

      lock (bufferLock)
      {
        int samplesNeeded = data.Length;
        int samplesAvailable = Mathf.Min(availableSamples, samplesNeeded);

        for (int i = 0; i < samplesNeeded; i++)
        {
          if (i < samplesAvailable)
          {
            data[i] = audioCircularBuffer.Read();
            availableSamples--;
          }
          else
          {
            data[i] = 0;
          }
        }

        if (logBufferStats && samplesNeeded > samplesAvailable)
        {
          Debug.LogWarning($"Audio buffer underrun! Needed: {samplesNeeded}, Available: {samplesAvailable}");
        }
      }
    }

    private void OnDestroy()
    {
      if (SteamClient.IsValid)
      {
        SteamClient.Shutdown();
      }

      uncompressedStream?.Dispose();
      compressedStream?.Dispose();
    }

    private class CircularBuffer
    {
      private readonly float[] buffer;
      private int head;
      private int tail;
      private int count;

      public int Capacity => buffer.Length;

      public CircularBuffer(int capacity)
      {
        buffer = new float[capacity];
        head = 0;
        tail = 0;
        count = 0;
      }

      public void Write(float value)
      {
        buffer[head] = value;
        head = (head + 1) % Capacity;
        if (count < Capacity)
          count++;
        else
          tail = (tail + 1) % Capacity;
      }

      public float Read()
      {
        if (count == 0) return 0f;

        float value = buffer[tail];
        tail = (tail + 1) % Capacity;
        count--;
        return value;
      }
    }
  }
}