namespace BrainClock.PlayerComms
{
    public interface IAudioDataReceiver
    {
        void ReceiveAudioData(long referenceId, byte[] data, int length, float volume, int flags);
    }
}