# GroovyCodecs
A set of C# native implementations of various audio codecs:
- An MP3 encoder / decoder, based on LAME and Jump3r.
- A G.729 encoder / decoder based on libjitsi/Atlassian code.
- A G.711 encoder / decoder 

Released under the LGPL v3.0.

Compiles to a .NET Standard 2.0 DLL and will run on .NET Framework >= 4.6 and .NET Core >= 2.0.

Fully managed code, no external OS dependencies, cross platform.

Not the most polished or optimised code; if you want efficiency you should be using LAME via NAudio or something like that.

Some of the Jump3r java code was converted to C# using the Free Edition of the Java to C# Converter 
produced by Tangible Software Solutions - https://www.tangiblesoftwaresolutions.com

V0.1.0 (alpha)
- Functional MP3 encoding
- A minimal console test app that will convert a wav file to an mp3 file

NOTE: this is still alpha code, and the GroovyMp3.dll interface is likely to change

V0.1.1 (alpha)
- Added G.711 and G.729 codecs

## Example MP3 Decoding

Let's say you have an array of byte (`bytes`) representing a MP3 file and wants to get the PCM data as an array of float (`samples`), you can do it like this:

```csharp
Stream stream = new MemoryStream(bytes);
var decoder = new Mp3Decoder(stream);

float[] samples = new float[decoder.Length * decoder.Channels];
float[] buffer = new float[4096];

int n = 0;
int samplesReturned = 1;
while (samplesReturned > 0)
{
    samplesReturned = decoder.decode(buffer, true);

    for (int i = 0; i < samplesReturned; i++)
    {
        samples[n++] = buffer[i];
    }
}

stream.Dispose();
decoder.close();
```

From my own test, it seems to be able to get more accurate data tham [MP3Sharp](https://github.com/ZaneDubya/MP3Sharp) or [NLayer](https://github.com/naudio/NLayer)