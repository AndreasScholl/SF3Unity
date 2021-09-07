using System.IO;
using UnityEngine;

public class MemoryManager
{
    public byte[] Data;
    public const int DataSize = 0x0100000;
    public byte[] BackupData;

    public int Base;

    public MemoryManager(int baseAddress)
    {
        Base = baseAddress;

        Data = new byte[DataSize];
        BackupData = new byte[DataSize];
    }

    public int LoadFile(string filePath, int address)
    {
        byte[] data;
        
        data = File.ReadAllBytes(filePath);

        data.CopyTo(Data, address - Base);

        return data.Length;
    }

    public bool IsAddressValid(int address)
    {
        if (address < Base)
        {
            return false;
        }
        else if (address >= Base + DataSize)
        {
            return false;
        }

        return true;
    }

    public int GetInt32(int address)
    {
        int value;

        address -= Base;
        value = (Data[address] << 24) + (Data[address + 1] << 16) + (Data[address + 2] << 8) + Data[address + 3];

        return value;
    }

    public int GetInt16(int address)
    {
        int value;

        address -= Base;
        value = (Data[address] << 8) + Data[address + 1];

        return value;
    }

    public byte GetByte(int address)
    {
        return Data[address - Base];
    }

    public float GetFloat(int address)
    {
        int intValue = GetInt32(address);

        //float value = 0f;

        //short integral = (short)(intValue >> 16);
        //int fraction = intValue & 0xffff;

        //value = integral + ((float)fraction / (float)0x10000);
        float value = intValue / (float)0x10000;

        return value;
    }

    public float GetFloat16(int address)
    {
        int intValue = GetInt16(address);

        sbyte integral = (sbyte)(intValue >> 8);
        int fraction = intValue & 0xff;

        float value = integral + ((float)fraction / (float)0x100);

        return value;
    }

    public float GetAngle16(int address)
    {
        int intValue = GetInt16(address);

        byte integral = (byte)(intValue >> 8);
        int fraction = intValue & 0xff;

        float value = integral + ((float)fraction / (float)0x100);

        return value;
    }

    public int GetPolygonIndex(int address, out bool negate)
    {
        ushort value = (ushort)GetInt16(address);

        if ((value & 0x8000) != 0)
        {
            value ^= 0xffff;
            negate = true;
        }
        else
        {
            negate = false;
        }

        int index = value >> 3;

        return index;
    }

    public int GetPolygonIndex8(int address, int numPoints, out bool negate)
    {
        byte value = (byte)Data[address - Base];

        if ((value & 0x80) != 0)
        {
            value ^= 0xff;
            negate = true;
        }
        else
        {
            negate = false;
        }

        int index = value;

        if (index >= numPoints)
        {
            index -= numPoints;
        }

        return index;
    }

    public Vector3 GetVector3(int address)
    {
        float x, y, z;
        x = GetFloat(address);
        y = GetFloat(address + 4);
        z = GetFloat(address + 8);

        return new Vector3(x, y, z);
    }

    public Vector3 GetVector3_16(int address)
    {
        float x, y, z;
        x = GetFloat16(address);
        y = GetFloat16(address + 2);
        z = GetFloat16(address + 4);

        return new Vector3(x, y, z);
    }

    public void Backup()
    {
        Data.CopyTo(BackupData, 0);
    }

    public void Restore()
    {
        BackupData.CopyTo(Data, 0);
    }
}

