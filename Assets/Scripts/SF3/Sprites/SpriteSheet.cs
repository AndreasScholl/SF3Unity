using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Shiningforce
{
    public class Header
    {
        public byte Promotion;
        public List<Texture2D> Textures = new List<Texture2D>();
        public ushort ID => toUShort(0);
        public ushort Width => toUShort(2);
        public ushort Height => toUShort(4);

        public byte NumberOfAngles => RawData[0x6];
        public byte Junk => RawData[0x7];

        public ushort WidthReads => RawData[0x8];
        public ushort HeightReads => RawData[0x9];
        public byte PromotionSometimes => RawData[0xA];
        public byte Junk2 => RawData[0xB];


        public uint StartAddress => toUInt(0x10);
        public uint TimingAddress => toUInt(0x14);

        protected List<byte> RawData = new List<byte>();

        private ushort toUShort(int idx) => (ushort)(RawData[idx] << 8 | RawData[idx + 1]);
        private uint toUInt(int idx) => (uint)((toUShort(idx) << 16) | toUShort(idx + 2));

        public Header(BinaryReader br)
        {
            RawData = br.ReadBytes(0x18).ToList();
        }
    }

    public struct TimingEventFrame
    {
        public ushort Index;
        public ushort NumberOfFrames;
        public TimingEventFrame(ushort index, ushort fps)
        {
            Index = index;
            NumberOfFrames = fps;
        }
    }
    public class TimingEventFrames
    {
        public int this[int animFrame]
        {
            get
            {
                animFrame %= TotalFrameCount;

                var frameCount = 0;
                foreach (var e in Events)
                {
                    if (animFrame - frameCount < e.NumberOfFrames)
                    {
                        return e.Index;
                    }
                    frameCount += e.NumberOfFrames;
                }

                return Events.Last().Index;
            }
        }
        public int TotalFrameCount => Events.Sum(s => s.NumberOfFrames);

        public List<TimingEventFrame> Events = new List<TimingEventFrame>();
        public void Add(TimingEventFrame timingEvent) => Events.Add(timingEvent);
    }

    public struct SpriteLayout
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public int NumOfFrames;
    }

    public enum Direction
    {
        DOWN_RIGHT_HARD,
        DOWN_RIGHT_EASY,
        UP_RIGHT_EASY,
        UP_RIGHT_HARD,
        DOWN,
        UP
    }

    public enum AnimationState
    {
        STATIONARY,
        IDLE,
        WALK,
        YES,
        NO,
        SPELLSTATIONARY,
        SPELL,
        KNEELINGSTATIONARY,
        KNEELING,
        GETTINGUP,
        STATE10,
        STATE11,
        STATE12,
        STATE13,
        STATE14,
        STATE15,
    }

    public class SpriteSheet
    {
        public Header Header;
        public uint TimingAddress => Header.TimingAddress;
        public int ID => Header.ID;
        public List<uint> Frames = new List<uint>();
        public Texture2D TextureAtlas;
        public bool Active => Header.NumberOfAngles == 6;
        public int Promotion => Header.Promotion;
        public int CurrentFrame = 0;

        private AnimationState _State = AnimationState.STATIONARY;

        public AnimationState State
        {
            get => _State;

            set
            {
                if (_State == value) return;
                if (TimingEvents.Count <= (int)value) { value = AnimationState.STATIONARY; }
                _State = value;
                CurrentFrame = 0;
            }
        }
        private List<Sprite> SpriteCollection = new List<Sprite>();
        public List<TimingEventFrames> TimingEvents = new List<TimingEventFrames>();

        //public SpriteSheet this[int i] => SpriteCollection.FirstOrDefault(s => s.ID == i && s.Active == false && s.Promotion == 0);
        //public SpriteSheet this[int i, bool active] => SpriteCollection.FirstOrDefault(s => s.ID == i && s.Active == active && s.Promotion == 0);
        //public SpriteSheet this[int i, int promotion] => SpriteCollection.FirstOrDefault(s => s.ID == i && s.Active == false && s.Promotion == promotion);
        public Sprite this[int frame, int rotation] => SpriteCollection[TimingEvents[(int)State][frame] + rotation];

        public SpriteSheet(BinaryReader br)
        {
            Header = new Header(br);
        }

        public uint ExtractFrames(BinaryReader br, long StartingPosition)
        {
            var read = 0xFFFFFFFF;
            br.Position(StartingPosition + Header.StartAddress);
            while (read > 0)
            {
                read = br.ReadUInt32().FlipEndian();
                if (read != 0) Frames.Add(read);
            }
            return read;
        }

        public void ExtractTiming(BinaryReader br, long StartingPosition)
        {
            var timingEventAddresses = new List<uint>();
            var lengthTimingEventAddresses = new List<uint>();

            br.Position(StartingPosition + Header.TimingAddress);
            var read = 0xFFFFFFFF;
            for (int i = 0; i < 16; i++)
            {
                read = br.ReadUInt32().FlipEndian();
                if (timingEventAddresses.Count > 0)
                {
                    lengthTimingEventAddresses.Add(read);
                }
                timingEventAddresses.Add(read);

            }
            lengthTimingEventAddresses.Reverse();
            lengthTimingEventAddresses = lengthTimingEventAddresses.SkipWhile(s => s == 0).ToList();
            lengthTimingEventAddresses.Reverse();
            lengthTimingEventAddresses.Add(Header.TimingAddress);

            for (var t = 0; t < timingEventAddresses.Count; t++)
            {
                var timingEvents = new TimingEventFrames();
                if (timingEventAddresses[t] == 0)
                {
                    TimingEvents.Add(timingEvents);
                    continue;
                }

                br.Position(StartingPosition + timingEventAddresses[t]);
                while (br.Position() < StartingPosition + lengthTimingEventAddresses[t])
                {
                    var index = (ushort)(br.ReadUInt16().FlipEndian());
                    var frames = br.ReadUInt16().FlipEndian();

                    var timingEvent = new TimingEventFrame(index, frames);
                    timingEvents.Add(timingEvent);
                    if ((sbyte)index < 0) break;
                }
                TimingEvents.Add(timingEvents);
            }
        }

        public void DecompressImages(BinaryReader br, long StartingPosition, ref long EndingPosition)
        {
            Texture2D texture = CreateSheet(br, StartingPosition, ref EndingPosition);
            TextureAtlas = texture;

            var imageCount = (int)Math.Ceiling((double)Header.Textures.Count / Header.NumberOfAngles) * Header.NumberOfAngles;

            for (int sX = 0; sX < imageCount; sX++)
            {
                var sprite = Sprite.Create(texture, new Rect(sX * Header.Width, 0, Header.Width, Header.Height),
                                                    new Vector2(0.5f, 0.5f), 100f);
                SpriteCollection.Add(sprite);
            }
        }

        public Texture2D CreateSheet(BinaryReader br, long StartingPosition, ref long EndingPosition)
        {
            Header.Textures = new List<Texture2D>();
            var frames = Frames;

            for (int f = 0; f < frames.Count; f++)
            {
                try
                {
                    var frame = frames[f];
                    br.Position(StartingPosition + frame);
                    var test = RawImageDecoder.DecompressImage(br).ToArray();
                    Header.Textures.Add(test.ToTexture2D(Header.Width, Header.Height));
                    EndingPosition = Math.Max(EndingPosition, br.Position());
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
            }
            var imageCount = (int)Math.Ceiling((double)Header.Textures.Count / Header.NumberOfAngles) * Header.NumberOfAngles;

            var sheet = new Texture2D(Header.Width * imageCount, Header.Height);
            sheet.filterMode = FilterMode.Point;
            var x = 0;
            for (var i = 0; i < Header.Textures.Count; i++)
            {
                for (var ty = 0; ty < Header.Height; ty++)
                {
                    for (var tx = 0; tx < Header.Width; tx++)
                    {
                        sheet.SetPixel(x + tx, (Header.Height - 1)  - ty,  Header.Textures[i].GetPixel(tx, ty));
                    }
                }
                x += Header.Width;
            }
            sheet.Apply();
            return sheet;
        }

        public void Export(string name) => File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), $"{name}.png"), TextureAtlas.EncodeToPNG());
    }
}
