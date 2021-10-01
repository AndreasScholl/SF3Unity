using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ToneExtractor
{
    class Mixer
    {
        public byte[] EFSDLEFPAN = new byte[18];
    }

    class VL
    {
        public byte Slope0;
        public byte VelocityPoint0;
        public byte Level0;
        public byte Slope1;
        public byte VelocityPoint1;
        public byte Level1;
        public byte Slope2;
        public byte VelocityPoint2;
        public byte Level2;
        public byte Slope3;
    }

    class PEG
    {
        public byte PEGDLY;
        public byte OL;
        public byte AR;
        public byte AL;
        public byte DR;
        public byte DL;
        public byte SR;
        public byte SL;
        public byte RR;
        public byte RL;
    }

    class PLFO
    {
        public byte PLFODLY;
        public byte FRQR;
        public byte HT;
        public byte FDCT;
    }

    class Layer
    {
        public byte StartMidiNote;
        public byte EndMidiNote;
        public ushort[] SCSPRegister = new ushort[12];
        public byte BaseNote;
        public byte FineTune;
        public byte LayerNumberInsideFMConnectionVoice;
        public byte VLConversionNumber;
        public byte PEGNumber;
        public byte PLFONumber;

        public byte KeyOnExecute => (byte)((SCSPRegister[0] >> 12) & 0b1);
        public byte KeyOnBit => (byte)((SCSPRegister[0] >> 11) & 0b1);
        public byte SourceBitControl => (byte)((SCSPRegister[0] >> 9) & 0b11);
        public byte SourceSoundControl => (byte)((SCSPRegister[0] >> 7) & 0b11);
        public byte LoopControl => (byte)((SCSPRegister[0] >> 5) & 0b11);
        public byte PCM8Bit => (byte)((SCSPRegister[0] >> 4) & 0b1);
        public uint StartAddress => (uint)(((SCSPRegister[0] & 0b1111) << 16) | SCSPRegister[1]);
        public uint LocalStartAddress = 0;
        public uint LocalLoopStartAddress = 0;
        public ushort LoopStartAddress => SCSPRegister[2];
        public ushort LoopEndAddress => SCSPRegister[3];

        public int LoopSize => LoopEndAddress - LoopStartAddress;
        public byte Decay2Rate => (byte)((SCSPRegister[4] >> 11) & 0b11111);
        public byte Decay1Rate => (byte)((SCSPRegister[4] >> 6) & 0b11111);
        public byte EGHoldMode => (byte)((SCSPRegister[4] >> 5) & 0b1);
        public byte AttackRate => (byte)((SCSPRegister[4]) & 0b11111);
        public byte LoopStartLink => (byte)((SCSPRegister[5] >> 14) & 0b1);
        public byte KeyRateScaling => (byte)((SCSPRegister[5] >> 10) & 0b1111);
        public byte DecayLevel => (byte)((SCSPRegister[5] >> 5) & 0b11111);
        public byte ReleaseRate => (byte)((SCSPRegister[5]) & 0b11111);
        public byte StackWriteInhibit => (byte)((SCSPRegister[6] >> 9) & 0b1);
        public byte SoundDirect => (byte)((SCSPRegister[6] >> 8) & 0b1);
        public byte TotalLevel => (byte)((SCSPRegister[6] & 0xFF));
        public byte ModulationLevel => (byte)((SCSPRegister[7] >> 12) & 0b1111);
        public byte ModulationInputX => (byte)((SCSPRegister[7] >> 6) & 0b111111);
        public byte ModulationInpuyY => (byte)((SCSPRegister[7]) & 0b111111);
        public byte Octave => (byte)((SCSPRegister[8] >> 11) & 0b1111);
        public byte FrequencyNumberSwitch => (byte)(SCSPRegister[8] & 0xb1111111111);
        public byte LFOReset => (byte)((SCSPRegister[9] >> 15) & 0b1);
        public byte LFOFrequency => (byte)((SCSPRegister[9] >> 10) & 0b11111);
        public byte PitchLFOWaveSelect => (byte)((SCSPRegister[9] >> 8) & 0b11);
        public byte PitchLFODisplacement => (byte)((SCSPRegister[9] >> 5) & 0b111);
        public byte AmplitudeLFOWaveSelect => (byte)((SCSPRegister[9] >> 3) & 0b11);
        public byte AmplitudeLFODisplacement => (byte)((SCSPRegister[9]) & 0b111);
        public byte InputSelect => (byte)((SCSPRegister[10] >> 3) & 0b1111);
        public byte InputMixLevel => (byte)((SCSPRegister[10]) & 0b111);
        public byte DirectDryLevel => (byte)((SCSPRegister[11] >> 13) & 0b111);
        public byte DirectDryPan => (byte)((SCSPRegister[11] >> 8) & 0b11111);
        public byte EffectWetLevel => (byte)((SCSPRegister[11] >> 5) & 0b111);
        public byte EffectWetPan => (byte)((SCSPRegister[11]) & 0b11111);

        public float[] Wave;

        public int GetWaveSize()
        {
            int size = LoopEndAddress;

            return size;
        }

        public void StoreWaveData(byte[] data, int baseOffset = 0)
        {
            int waveSize = GetWaveSize();
            Wave = new float[waveSize];

            if (PCM8Bit == 1)
            {
                // 8-bit pcm
                for (int count = 0; count < waveSize; count++)
                {
                    sbyte pcmValue = ((sbyte)data[StartAddress + baseOffset + count]);
                    float value = pcmValue / 128f;
                    Wave[count] = value;
                }
            }
            else
            {
                // 16-bit pcm
                for (int count = 0; count < waveSize; count++)
                {
                    short pcmValue = (short)((data[StartAddress + baseOffset + (count * 2)] << 8) | data[StartAddress + baseOffset + (count * 2) + 1]);
                    float value = pcmValue / 32768f;
                    Wave[count] = value;
                }
            }
        }

        public void DebugLog()
        {
            Debug.Log("  Layer: " + StartMidiNote + " , " + EndMidiNote);

            string scspDebug = "";
            for (int count = 0; count < 12; count++)
            {
                scspDebug += SCSPRegister[count].ToString("X4");
                scspDebug += " ";
            }
            Debug.Log("    SCSP: " + scspDebug);

            Debug.Log("          " + BaseNote + " , " + FineTune + " , " + LayerNumberInsideFMConnectionVoice + " , "
                                   + VLConversionNumber + " , " + PEGNumber + " , " + PLFONumber);

            Debug.Log("     LOOP: " + LoopControl);
            Debug.Log("     8Bit: " + PCM8Bit);
            Debug.Log("    Start: " + StartAddress.ToString("X6"));
            Debug.Log("LoopStart: " + LoopStartAddress.ToString("X6"));
            Debug.Log(" LoopSize: " + LoopEndAddress.ToString("X6"));
        }
    }

    class Voice
    {
        public byte BendRangeWidth;
        public byte PortamentoTime;
        public byte NumberOfLayers;
        public byte VolumeBias;

        public List<Layer> Layers = new List<Layer>();

        public void DebugLog()
        {
            Debug.Log("--------------------------------");
            Debug.Log("Voice: " + BendRangeWidth + " , " + PortamentoTime + " , " + NumberOfLayers + " , " + VolumeBias);

            foreach (Layer layer in Layers)
            {
                layer.DebugLog();
            }
            Debug.Log("--------------------------------");
        }
    }

    private List<Voice> _voiceList = new List<Voice>();

    public ToneExtractor(string filePath, string soundPostfix)
    {
        byte[] data = File.ReadAllBytes(filePath);

        ExtractVoices(data, 0, data.Length, soundPostfix);
    }

    public ToneExtractor(byte[] data, int offset, int dataSize, string soundPostfix)
    {
        ExtractVoices(data, offset, dataSize, soundPostfix);
    }

    public void ExtractVoices(byte[] data, int offset, int dataSize, string soundPostfix)
    {
        int baseOffset = offset;

        var mixerOffset = GetInt16(data, offset) + baseOffset;
        offset += 2;

        var vlOffset = GetInt16(data, offset) + baseOffset;
        offset += 2;

        var pegOffset = GetInt16(data, offset) + baseOffset;
        offset += 2;

        var plfoOffset = GetInt16(data, offset) + baseOffset;
        offset += 2;

        // debug output
        //Debug.Log("mixerOffset " + mixerOffset.ToString("X4"));
        //Debug.Log("vlOffset " + vlOffset.ToString("X4"));
        //Debug.Log("pegOffset " + pegOffset.ToString("X4"));
        //Debug.Log("plfoOffset " + plfoOffset.ToString("X4"));

        var numberOfVoices = (mixerOffset - offset) / 2;

        // debug output
        //Debug.Log("voices " + numberOfVoices);

        for (int v = 0; v < numberOfVoices; v++)
        {
            var voiceOffset = (ushort)GetInt16(data, offset) + baseOffset;
            offset += 2;

            // debug output
            //Debug.Log("voiceOffset " + voiceOffset.ToString("X4"));

            Voice voice = new Voice();
            voice.BendRangeWidth = data[voiceOffset];
            voice.PortamentoTime = data[voiceOffset + 1];
            voice.NumberOfLayers = (byte)(data[voiceOffset + 2] + 1);
            voice.VolumeBias = data[voiceOffset + 3];

            voiceOffset += 4;

            for (int layerCount = 0; layerCount < voice.NumberOfLayers; layerCount++)
            {
                Layer layer = new Layer();

                layer.StartMidiNote = data[voiceOffset];
                layer.EndMidiNote = data[voiceOffset + 1];
                for (int scspRegCount = 0; scspRegCount < 12; scspRegCount++)
                {
                    layer.SCSPRegister[scspRegCount] = (ushort)GetInt16(data, voiceOffset + 2 + (scspRegCount * 2));
                }
                layer.BaseNote = data[voiceOffset + 26];
                layer.FineTune = data[voiceOffset + 27];
                layer.LayerNumberInsideFMConnectionVoice = data[voiceOffset + 28];
                layer.VLConversionNumber = data[voiceOffset + 29];
                layer.PEGNumber = data[voiceOffset + 30];
                layer.PLFONumber = data[voiceOffset + 31];

                voice.Layers.Add(layer);

                // wave
                //
                layer.StoreWaveData(data, baseOffset);

                // debug cliptest
                AudioPlayer.GetInstance().AddPCMAudio(layer.Wave, _voiceList.Count.ToString() + "_" + layerCount + soundPostfix);

                voiceOffset += 32;
            }

            // debug output
           //Debug.Log(_voiceList.Count);
            //voice.DebugLog();

            _voiceList.Add(voice);
        }
    }

    int GetInt16(byte[] array, int offset)
    {
        int value;

        value = (array[offset] << 8) + array[offset + 1];

        return value;
    }

}
