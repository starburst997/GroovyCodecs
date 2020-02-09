using System.IO;

namespace GroovyCodecs.Mp3
{
    public interface IMp3Decoder
    {
        void close();

        int decode(float[] sampleBuffer, bool playOriginal);
    }
}