using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using OggVorbisEncoder;
using Encoding = System.Text.Encoding;

namespace GenshinFilePlayer
{
    public class PckData(string fullPath)
    {
        public string? Path;
        public long Offset;
        public long Size;
        public (long, long?) ID;
        public Pck.ExtensionKind Extension = Pck.ExtensionKind.Wem;
        public string? LanguageData;
        public string PckName = "";
        public List<string> Folders = [];
        public string? Name;
        public string? SongHash;
        //public Mutex? Mutex;
        public string FullPath = fullPath;

        public string? GetPossibleName()
        {
            return SongHash switch
            {
                "" => "This useless thing is to remove the VS warning message, ignore until done.",
                _ => null, // TODO
            };
        }

        public byte[] GetRaw()
        {
            //Mutex!.WaitOne();
            using var reader = new BinaryReader(File.OpenRead(FullPath));
            var currentOffset = reader!.BaseStream.Position;
            reader.BaseStream.Seek(Offset, SeekOrigin.Begin);
            var data = reader.ReadBytes((int)Size);
            reader.BaseStream.Seek(currentOffset, SeekOrigin.Begin);
            reader.Close();
            reader.Dispose();
            //Mutex!.ReleaseMutex();
            return data;
        }

        public byte[] GetWav()
        {
            var targetFilename = $"{System.IO.Path.GetTempPath()}/{System.IO.Path.GetRandomFileName()}.{Extension.ToString().ToLower()}";
            {
                using var f = File.OpenWrite(targetFilename);
                f.Write(GetRaw());
                f.Flush();
                f.Close();
            }
            using var proc = new Process()
            {
                StartInfo = new()
                {
                    FileName = $"{Directory.GetParent(Environment.ProcessPath!)!.FullName}/vgmstream/vgmstream-cli.exe",
                    Arguments = $"-i -p {targetFilename}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                }
            };
            proc.Start();

            using var final = new MemoryStream();
            while (!proc.HasExited)
                proc.StandardOutput.BaseStream.CopyTo(final);
            proc.WaitForExit();
            proc.Dispose();

            File.Delete(targetFilename);

            return final.ToArray();
        }

        private const int WriteBufferSize = 4096;

        public byte[] GetOgg()
        {
            using var data = new BinaryReader(new MemoryStream(GetWav()));
            var format = PcmSample.EightBit;
            var sampleRate = 0;
            var channels = 0;
            var samples = Array.Empty<byte>();

            for (int i = 0; i < data.BaseStream.Length - 4; i++)
            {
                data.BaseStream.Seek(i, SeekOrigin.Begin);
                switch (Encoding.ASCII.GetString(data.ReadBytes(4)))
                {
                    case "fmt ":
                        data.BaseStream.Seek(4, SeekOrigin.Current);
                        if (data.ReadUInt16() == 1)
                            format = PcmSample.SixteenBit;
                        channels = data.ReadUInt16();
                        sampleRate = data.ReadInt32();
                        break;
                    case "data":
                        samples = data.ReadBytes(data.ReadInt32());
                        break;
                }
                if (samples.Length > 0)
                    break;
            }

            return ConvertRawPCMFile(sampleRate, channels, samples, format, sampleRate, channels);
        }

        private static byte[] ConvertRawPCMFile(int outputSampleRate, int outputChannels, byte[] pcmSamples, PcmSample pcmSampleSize, int pcmSampleRate, int pcmChannels)
        {
            int numPcmSamples = (pcmSampleSize == 0 || pcmChannels == 0) ? 0 : (pcmSamples.Length / (int)pcmSampleSize / pcmChannels);
            float pcmDuration = (pcmSampleSize == 0 || pcmChannels == 0) ? 0 : (numPcmSamples / (float)pcmSampleRate);
            int numOutputSamples = (int)(pcmDuration * outputSampleRate) / WriteBufferSize * WriteBufferSize;
            float[][] outSamples = new float[outputChannels][];

            for (int ch = 0; ch < outputChannels; ch++)
                outSamples[ch] = new float[numOutputSamples];

            for (int sampleNumber = 0; sampleNumber < numOutputSamples; sampleNumber++)
            {
                for (int ch = 0; ch < outputChannels; ch++)
                {
                    int sampleIndex = sampleNumber * pcmChannels * (int)pcmSampleSize;
                    if (ch < pcmChannels)
                        sampleIndex += ch * (int)pcmSampleSize;
                    outSamples[ch][sampleNumber] = pcmSampleSize switch // Raw sample
                    {
                        PcmSample.EightBit => pcmSamples[sampleIndex] / 128f,
                        PcmSample.SixteenBit => ((short)(pcmSamples[sampleIndex + 1] << 8 | pcmSamples[sampleIndex])) / 32768f,
                        _ => throw new NotImplementedException(),
                    };
                }
            }

            return GenerateFile(outSamples, outputSampleRate, outputChannels);
        }

        private static byte[] GenerateFile(float[][] floatSamples, int sampleRate, int channels)
        {
            using MemoryStream outputData = new();

            // Stores all the static vorbis bitstream settings
            var info = VorbisInfo.InitVariableBitRate(channels, sampleRate, 0.5f);

            // set up our packet->stream encoder
            var serial = new Random().Next();
            var oggStream = new OggStream(serial);

            // =========================================================
            // HEADER
            // =========================================================
            // Vorbis streams begin with three headers; the initial header (with
            // most of the codec setup parameters) which is mandated by the Ogg
            // bitstream spec.  The second header holds any comment fields.  The
            // third header holds the bitstream codebook.

            var comments = new Comments();
            comments.AddTag("ARTIST", "GenshinFilePlayer");

            var infoPacket = HeaderPacketBuilder.BuildInfoPacket(info);
            var commentsPacket = HeaderPacketBuilder.BuildCommentsPacket(comments);
            var booksPacket = HeaderPacketBuilder.BuildBooksPacket(info);

            oggStream.PacketIn(infoPacket);
            oggStream.PacketIn(commentsPacket);
            oggStream.PacketIn(booksPacket);

            // Flush to force audio data onto its own page per the spec
            FlushPages(oggStream, outputData, true);

            // =========================================================
            // BODY (Audio Data)
            // =========================================================
            var processingState = ProcessingState.Create(info);
            for (int readIndex = 0; readIndex <= floatSamples[0].Length; readIndex += WriteBufferSize)
            {
                if (readIndex == floatSamples[0].Length)
                    processingState.WriteEndOfStream();
                else
                    processingState.WriteData(floatSamples, WriteBufferSize, readIndex);
                while (!oggStream.Finished && processingState.PacketOut(out OggPacket packet))
                {
                    oggStream.PacketIn(packet);
                    FlushPages(oggStream, outputData, false);
                }
            }
            FlushPages(oggStream, outputData, true);

            return outputData.ToArray();
        }

        private static void FlushPages(OggStream oggStream, Stream output, bool force)
        {
            while (oggStream.PageOut(out OggPage page, force))
            {
                output.Write(page.Header, 0, page.Header.Length);
                output.Write(page.Body, 0, page.Body.Length);
            }
        }

        enum PcmSample
        {
            EightBit = 1,
            SixteenBit = 2,
        }
    }

    public class Pck(string fullPath)
    {
        public enum ExtensionKind
        {
            Xma,
            Ogg,
            Wav,
            Bnk,
            Ext,
            Wem,
        }

        public bool IsParsed;

        public uint BankVersion;
        public uint HeaderSize;
        public uint Flag;

        public uint LanguageSize;
        public uint BankSize;
        public uint SoundSize;
        public uint ExternalSize;

        public Dictionary<uint, string> LanguageList = [];
        public Dictionary<string, PckData> FileSystem = [];

        public bool Cache;
        public string Name = "";
        public string FullPath = fullPath;
        public string LocalPath = "";
        public bool IsLoaded;
        public long PckSize;

        //public Mutex Mutex = new();
        static readonly HashSet<string> HashCollisions = [];
        static readonly Mutex GlobalMutex = new();

        public static void ClearHashes()
        {
            HashCollisions.Clear();
        }

        public void Read(bool useCache = false, string? name = null)
        {
            Cache = useCache;
            IsParsed = true;

            if (name != null)
                Name = name;

            FileSystem.Clear();
            LanguageList.Clear();

            using var reader = new BinaryReader(File.OpenRead(FullPath));

            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            var headerIdentifier = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (headerIdentifier != "AKPK")
                throw new Exception($"Expected .pck file header (AKPK) but found {headerIdentifier}");

            PckSize = reader.BaseStream.Length;
            HeaderSize = reader.ReadUInt32();
            Flag = reader.ReadUInt32();

            if (BinaryPrimitives.ReverseEndianness(Flag) < Flag)
                throw new Exception("Expected little-endian file, but found big-endian!");
            LanguageSize = reader.ReadUInt32();
            BankSize = reader.ReadUInt32();
            SoundSize = reader.ReadUInt32();
            if (LanguageSize + BankSize + SoundSize + 0x10 < HeaderSize)
                ExternalSize = reader.ReadUInt32();
            ParseLanguages(reader);

            // Extract banks
            ParseTable(reader, ExtensionKind.Bnk, BankSize, false, false);

            if (BankVersion == 0)
            {
                if (ExternalSize == 0)
                    Log.Write("Can't detect bank version, assuming 62.");
                BankVersion = 62;
            }

            // Extract sounds
            ParseTable(reader, ExtensionKind.Wem, SoundSize, true, false);

            // Extract externals
            ParseTable(reader, ExtensionKind.Wem, ExternalSize, true, true);

            // Last sound may be padding
            reader.Close();
            reader.Dispose();
        }

        void ParseLanguages(BinaryReader reader)
        {
            uint startOffset = (uint)reader.BaseStream.Position;
            uint languageAmount = reader.ReadUInt32();
            for (int i = 0; i < languageAmount; i++)
            {
                uint languageOffset = reader.ReadUInt32() + startOffset;
                uint languageID = reader.ReadUInt32();
                uint currentOffset = (uint)reader.BaseStream.Position;
                reader.BaseStream.Seek(languageOffset, SeekOrigin.Begin);
                string languageName;
                ushort testUnicode = reader.ReadUInt16();
                reader.BaseStream.Seek(-2, SeekOrigin.Current);
                if (((testUnicode & 0xff00) >> 8) == 0x00 || (testUnicode & 0x00ff) == 0x00) // Read Unicode
                {
                    var charList = new List<char>();
                    while (true)
                    {
                        ushort unicodeByte = reader.ReadUInt16();
                        if (unicodeByte == 0x00)
                            break;
                        charList.Add((char)unicodeByte);
                    }
                    languageName = new string([.. charList]);
                }
                else // Read UTF-8
                {
                    var charList = new List<byte>();
                    while (true)
                    {
                        byte _char = reader.ReadByte();
                        if (_char == 0x00)
                            break;
                        charList.Add(_char);
                    }
                    languageName = Encoding.UTF8.GetString([.. charList]);
                }
                LanguageList[languageID] = languageName;
                reader.BaseStream.Seek(currentOffset, SeekOrigin.Begin);
            }
            reader.BaseStream.Seek(startOffset + LanguageSize, SeekOrigin.Begin);
        }

        void DetectBankVersion(BinaryReader reader, long offset)
        {
            long currentOffset = reader.BaseStream.Position;
            // Skip BKHD<chunkSize:uint>
            reader.BaseStream.Seek(offset + 8, SeekOrigin.Begin);
            BankVersion = reader.ReadUInt32();
            if (BankVersion > 0x1000)
            {
                Log.Write("Wrong bank version, assuming 62.");
                BankVersion = 62;
            }
            reader.BaseStream.Seek(currentOffset, SeekOrigin.Begin);
        }

        void ParseTable(BinaryReader reader, ExtensionKind extension, uint sectionSize, bool isSounds, bool isExternals)
        {
            if (sectionSize != 0)
            {
                uint files = reader.ReadUInt32();
                if (files != 0)
                    ParseTableInternal(reader, extension, files, sectionSize, isSounds, isExternals);
            }
        }

        void ParseTableInternal(BinaryReader reader, ExtensionKind extension, uint files, uint sectionSize, bool isSounds, bool isExternals)
        {
            uint entrySize = (sectionSize - 0x04) / files;
            bool altMode = entrySize == 0x18;
            for (int i = 0; i < files; i++)
            {
                (uint, uint?) ID = altMode && isExternals ? (reader.ReadUInt32(), reader.ReadUInt32()) : (reader.ReadUInt32(), null);
                uint blockSize = reader.ReadUInt32();
                ulong size = altMode && !isExternals ? reader.ReadUInt64() : reader.ReadUInt32();
                uint offset = reader.ReadUInt32();
                uint languageID = reader.ReadUInt32();
                if (blockSize != 0)
                    offset *= blockSize;
                if (!isSounds && BankVersion == 0)
                    DetectBankVersion(reader, offset);
                if (isSounds && BankVersion < 62)
                {
                    long currentOffset = reader.BaseStream.Position;
                    // Maybe it should find the "fmt " chunk first
                    reader.BaseStream.Seek(offset + 0x14, SeekOrigin.Begin); // Codec offset
                    extension = reader.ReadUInt16() switch // Codec
                    {
                        0x0401 or 0x0166 => ExtensionKind.Xma,
                        0xffff => ExtensionKind.Ogg,
                        _ => ExtensionKind.Wav,
                    };
                    reader.BaseStream.Seek(currentOffset, SeekOrigin.Begin);
                }
                string path = $"{Name}/{LanguageList[languageID]}";
                path = altMode && isExternals ? $"externals/{path}/{ID.Item1}+{ID.Item2}.{extension.ToString().ToLower()}" : $"{path}/{ID.Item1}.{extension.ToString().ToLower()}";
                {
                    var folders = new List<string>();

                    if (altMode && isExternals)
                        folders.Add("externals");
                    //folders.Add(Name);
                    folders.Add(LanguageList[languageID]);
                    var currentOffset = reader.BaseStream.Position;
                    reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    var songHash = Convert.ToHexStringLower(SHA512.HashData((reader.ReadBytes((int)size))));
                    reader.BaseStream.Seek(currentOffset, SeekOrigin.Begin);
                    GlobalMutex.WaitOne();
                    try
                    {
                        if (HashCollisions.Contains(songHash))
                        {
                            Log.Write($"{songHash} was already in a collision, changing...");
                            var indexer = 0;
                            while (HashCollisions.Contains($"{songHash}+{indexer}"))
                                indexer++;
                            songHash += $"+{indexer}";
                        }
                        HashCollisions.Add(songHash);
                    }
                    finally
                    {
                        GlobalMutex.ReleaseMutex();
                    }

                    PckData data = new(FullPath)
                    {
                        ID = ID,
                        Path = path,
                        Extension = extension,
                        Offset = offset,
                        Size = (long)size,
                        LanguageData = LanguageList[languageID],
                        PckName = Name,
                        Folders = folders,
                        Name = altMode && isExternals ? $"{ID.Item1}+{ID.Item2}.{extension.ToString().ToLower()}" : $"{ID.Item1}.{extension.ToString().ToLower()}",
                        SongHash = songHash,
                        //Mutex = Mutex,
                    };
                    FileSystem.Add(path, data);
                }
            }
        }
    }
}
