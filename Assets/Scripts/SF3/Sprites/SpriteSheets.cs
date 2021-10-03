using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shiningforce
{
    public class SpriteSheets
    {
        public List<SpriteSheet> Sheets = new List<SpriteSheet>();

        public SpriteSheets(string path) : this(File.ReadAllBytes(path))
        {
        }

        public SpriteSheets(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            using (var br = new BinaryReader(stream))
            {
                while (br.Position() < br.Length())
                {
                    List<SpriteSheet> localSheets = new List<SpriteSheet>();
                    long StartingPosition = 0;
                    long EndingPosition = 0;
                    br.Position(br.Position() + (0x800 - (br.Position() & 0x7FF) & 0x7FF));

                    while (!(br.Position() >= (br.Length() - 1) || br.ReadUInt16() != 0xFFFF)) //Skip invalid sheets
                    {
                        br.Position(br.Position() + (0x800 - (br.Position() & 0x7FF) & 0x7FF));
                    }

                    if (br.Position() >= br.Length()) return;
                    br.Position(br.Position() & 0xFFFFF800);
                    StartingPosition = br.Position();

                    try
                    {
                        while (true)
                        {
                            var sprite = new SpriteSheet(br);
                            if (sprite.ID == 0xFFFF) break;
                            var prevPromotion = Sheets.Where(s => s.ID == sprite.ID && s.Active == sprite.Active).Select(s => s.Promotion).DefaultIfEmpty(-1).Max() + 1;
                            sprite.Header.Promotion = (byte) prevPromotion;
                            localSheets.Add(sprite);
                            System.Diagnostics.Debug.WriteLine($"POS: {br.Position() - StartingPosition:X4},ID: {sprite.ID:X4},Width: {sprite.Header.Width:X2},Height: {sprite.Header.Height:X2}");
                        }

                        localSheets.ForEach(sprite => sprite.ExtractFrames(br, StartingPosition));

                        EndingPosition = Math.Max(EndingPosition, br.Position());
                        localSheets.ForEach(sprite => sprite.ExtractTiming(br, StartingPosition));

                        EndingPosition = Math.Max(EndingPosition, br.Position());
                        localSheets.ForEach(sprite => sprite.DecompressImages(br, StartingPosition, ref EndingPosition));

                        br.Position(EndingPosition);
                        Sheets.AddRange(localSheets);
                    }
                    catch (Exception ex)
                    {
                        ex.ToString();
                        throw;
                    }
                }
            }
        }

        public int[] AvailableIDs => Sheets.Select(s => s.ID).Distinct().ToArray();

        public SpriteSheet this[int i] =>  Sheets.FirstOrDefault(s => s.ID == i && s.Active == false && s.Promotion == 0) ?? Sheets.FirstOrDefault(s => s.ID == i );
        public SpriteSheet this[int i, bool active] => Sheets.FirstOrDefault(s => s.ID == i && s.Active == active && s.Promotion == 0) ?? this[i];
        public SpriteSheet this[int i, int promotion] => Sheets.FirstOrDefault(s => s.ID == i && s.Active == false && s.Promotion == promotion) ?? this[i];
        public SpriteSheet this[int i, bool active, int promotion] => Sheets.FirstOrDefault(s => s.ID == i && s.Active == active && s.Promotion == promotion) ?? this[i, active] ?? this[i,  promotion];
    }
}
