using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using GroovyCodecs.Mp3.Common;
using GroovyCodecs.Mp3.Mp3;
using GroovyCodecs.Mp3.Mpg;
using GroovyCodecs.Types;

namespace GroovyCodecs.Mp3
{

    public class Mp3Decoder : IMp3Decoder
    {

        private readonly BitStream bs;

        private readonly short[][] buffer = Arrays.ReturnRectangularArray<short>(2, 1152);

        private readonly Mpg.Common common;

        private readonly GainAnalysis ga;

        private readonly GetAudio gaud;

        // private DataOutput outf;
        private readonly LameGlobalFlags gfp;

        private readonly ID3Tag id3;

        private readonly Interface intf;

        private readonly Lame lame;

        private readonly MPGLib mpg;

        private readonly Presets p;

        private readonly Parse parse;

        private readonly Quantize qu;

        private readonly QuantizePVT qupvt;

        private readonly Reservoir rv;

        private readonly Takehiro tak;

        private readonly VBRTag vbr;

        private readonly Mp3Version ver;

        public int WavSize;

        public int Length;

        public int Channels;
        
        public int SkipStart;
        public int SkipEnd;
        
        public Mp3Decoder(Stream mp3Stream)
        {
            // encoder modules
            lame = new Lame();
            gaud = new GetAudio();
            ga = new GainAnalysis();
            bs = new BitStream();
            p = new Presets();
            qupvt = new QuantizePVT();
            qu = new Quantize();
            vbr = new VBRTag();
            ver = new Mp3Version();
            id3 = new ID3Tag();
            rv = new Reservoir();
            tak = new Takehiro();
            parse = new Parse();

            mpg = new MPGLib();
            intf = new Interface();
            common = new Mpg.Common();

            lame.setModules(ga, bs, p, qupvt, qu, vbr, ver, id3, mpg);
            bs.setModules(ga, mpg, ver, vbr);
            id3.setModules(bs, ver);
            p.Modules = lame;
            qu.setModules(bs, rv, qupvt, tak);
            qupvt.setModules(tak, rv, lame.enc.psy);
            rv.Modules = bs;
            tak.Modules = qupvt;
            vbr.setModules(lame, bs, ver);
            gaud.setModules(parse, mpg);
            parse.setModules(ver, id3, p);

            // decoder modules
            mpg.setModules(intf, common);
            intf.setModules(vbr, common);

            gfp = lame.lame_init();

            /*
             * turn off automatic writing of ID3 tag data into mp3 stream we have to
             * call it before 'lame_init_params', because that function would spit
             * out ID3v2 tag data.
             */
            gfp.write_id3tag_automatic = false;

            /*
             * Now that all the options are set, lame needs to analyze them and set
             * some more internal options and check for problems
             */
            lame.lame_init_params(gfp);

            parse.input_format = GetAudio.sound_file_format.sf_mp3;

            var enc = new Enc();

            gaud.init_infile(gfp, mp3Stream, enc);

            SkipStart = 0;
            SkipEnd = 0;

            if (enc.enc_delay > -1 || enc.enc_padding > -1)
            {
                if (enc.enc_delay > -1)
                    SkipStart = enc.enc_delay + 528 + 1;

                if (enc.enc_padding > -1)
                    SkipEnd = enc.enc_padding - (528 + 1);
            }
            else
            {
                SkipStart = gfp.encoder_delay + 528 + 1;
            }

            WavSize = -(SkipStart + SkipEnd);
            parse.mp3input_data.totalframes = parse.mp3input_data.nsamp / parse.mp3input_data.framesize;

            Length = parse.mp3input_data.nsamp;
            Channels = gfp.num_channels;
            
            Debug.Assert(gfp.num_channels >= 1 && gfp.num_channels <= 2);
        }

        public virtual int decode(float[] sampleBuffer, bool playOriginal)
        {
            var iread = gaud.get_audio16(gfp, buffer); // TODO: Could I get float directly instead of using 16bit?
            if (iread >= 0)
            {
                parse.mp3input_data.framenum += iread / parse.mp3input_data.framesize;
                WavSize += iread;

                if (gfp.num_channels == 2)
                {
                    for (var i = 0; i < iread; i++)
                    {
                        sampleBuffer[i*2] = buffer[0][i] / 32768f;
                        sampleBuffer[i*2 + 1] = buffer[1][i] / 32768f;
                    }
                }
                else
                {
                    for (var i = 0; i < iread; i++)
                    {
                        sampleBuffer[i] = buffer[0][i] / 32768f;
                    }
                }

                return iread * gfp.num_channels;
            }

            return -1;
        }

        public virtual void close()
        {
            lame.lame_close(gfp);
        }
    }
}