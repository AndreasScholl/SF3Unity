using System.Collections.Generic;
using UnityEngine;

public class ByteArray
{
    static public int GetInt32(byte[] array, int offset)
    {
        int value;

        value = (array[offset] << 24) + (array[offset + 1] << 16) + (array[offset + 2] << 8) + array[offset + 3];

        return value;
    }

    static public int GetInt16(byte[] array, int offset)
    {
        int value;

        value = (array[offset] << 8) + array[offset + 1];

        return value;
    }

    static public short GetShort(byte[] array, int offset)
    {
        short value;

        value = (short)((array[offset] << 8) + array[offset + 1]);

        return value;
    }

    static public byte GetByte(byte[] array, int offset)
    {
        return array[offset];
    }

    static public float GetFloat(byte[] array, int offset)
    {
        int intValue = GetInt32(array, offset);

        //float value = intValue / 65536.0f;

        short integral = (short)(intValue >> 16);
        int fraction = intValue & 0xffff;
        float value = integral + ((float)fraction / (float)0x10000);

        return value;
    }

    static public float GetFloat16(byte[] array, int offset)
    {
        int intValue = GetInt16(array, offset);

        //float value = (short)intValue / 256.0f;

        // 16-bit fixed
        sbyte integral = (sbyte)(intValue >> 8);
        int fraction = intValue & 0xff;
        float value = integral + ((float)fraction / (float)0x100);

        return value;
    }

    static public float GetFloatCompressed16(byte[] array, int offset)
    {
        short shortValue = GetShort(array, offset);

        int intValue = (int)shortValue;
        intValue = intValue << 2;

        float value = intValue / 65536.0f;

        return value;
    }

    static public Vector3 GetVector3(byte[] array, int offset)
    {
        float x, y, z;
        x = ByteArray.GetFloat(array, offset);
        y = ByteArray.GetFloat(array, offset + 4);
        z = ByteArray.GetFloat(array, offset + 8);

        return new Vector3(x, y, z);
    }

    static public Vector3 GetVector3_16(byte[] array, int offset)
    {
        float x, y, z;
        x = ByteArray.GetFloat16(array, offset);
        y = ByteArray.GetFloat16(array, offset + 4);
        z = ByteArray.GetFloat16(array, offset + 8);

        return new Vector3(x, y, z);
    }

    static public Quaternion GetQuaternion(byte[] array, int offset)
    {
        float x, y, z, w;
        x = ByteArray.GetFloat(array, offset);
        y = ByteArray.GetFloat(array, offset + 4);
        z = ByteArray.GetFloat(array, offset + 8);
        w = ByteArray.GetFloat(array, offset + 12);

        return new Quaternion(x, y, z, w);
    }

    static public List<int> ReadInt16List(byte[] array, int size, int offset)
    {
        List<int> list = new List<int>();

        for (int i = 0; i < size; i++)
        {
            int value = ByteArray.GetInt16(array, offset);
            list.Add(value);
            offset += 2;
        }

        return list;
    }

    static public List<float> ReadFixedList(byte[] array, int size, int offset)
    {
        List<float> list = new List<float>();

        for (int i = 0; i < size; i++)
        {
            float value = ByteArray.GetFloat(array, offset);
            list.Add(value);
            offset += 4;
        }

        return list;
    }

    static public List<float> ReadFixed16List(byte[] array, int size, int offset)
    {
        List<float> list = new List<float>();

        for (int i = 0; i < size; i++)
        {
            //float value = ByteArray.GetFloat16(array, offset);
            float value = ByteArray.GetFloatCompressed16(array, offset);
            list.Add(value);
            offset += 2;
        }

        return list;
    }

}
