using System.Text;
using UnityEngine;

#pragma warning disable CS0660
#pragma warning disable CS0661

interface IBitFlag
{
    public void SetBit(int bit);
    public void ResetBit(int bit);
    public bool CheckBit(int bit);
    public bool[] GetBitArray();
}

[System.Serializable]
public struct BitFlag8 : IBitFlag
{
    [SerializeField] byte flagsBytes;

    public BitFlag8(bool[] bitArray)
    {
        flagsBytes = 0;

        if (bitArray.Length < 8)
        {
            Debug.LogError($"Tried to initialize an 8 bit BitFlag with a wider bool array");
            return;
        }

        for (int i = 0; i < 8; i++)
            if (bitArray[i]) SetBit(i);
    }

    public BitFlag8(byte byteFlag) => flagsBytes = byteFlag;

    public static BitFlag8 operator +(BitFlag8 flag, int bit)
    {
        flag.SetBit(bit);
        return flag;
    }

    public static BitFlag8 operator -(BitFlag8 flag, int bit)
    {
        flag.ResetBit(bit);
        return flag;
    }

    public static bool operator ==(BitFlag8 flag, int bit) => flag.CheckBit(bit);

    public static bool operator !=(BitFlag8 flag, int bit) => !flag.CheckBit(bit);

    public void SetBit(int bit)
    {
        bit = Mathf.Clamp(bit, 0, 7);
        flagsBytes |= (byte)(1 << bit);
    }

    public void ResetBit(int bit)
    {
        bit = Mathf.Clamp(bit, 0, 7);
        flagsBytes &= (byte) ~(1 << bit);
    }

    public bool CheckBit(int bit)
    {
        bit = Mathf.Clamp(bit, 0, 7);
        return (flagsBytes & (byte)(1 << bit)) != 0;
    }

    public void SetAllBits() { for (int i = 0; i < 8; i++) SetBit(i); }
    public void ResetAllBits() { for (int i = 0; i < 8; i++) ResetBit(i); }

    public bool[] GetBitArray()
    {
        bool[] boolArray = new bool[8];

        for (int i = 0; i < 8; i++) boolArray[i] = CheckBit(i);
        return boolArray;
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder("(");

        for (int i = 0; i < 7; i++)
            stringBuilder.AppendFormat("{0}, ", CheckBit(i) ? 1 : 0);

        stringBuilder.AppendFormat("{0})", CheckBit(7) ? 1 : 0);
        return stringBuilder.ToString();
    }

    public byte GetByte() => flagsBytes;
}

[System.Serializable]
public struct BitFlag16 : IBitFlag
{
    [SerializeField] ushort flagsBytes;

    public BitFlag16(bool[] bitArray)
    {
        flagsBytes = 0;

        if (bitArray.Length < 16)
        {
            Debug.LogError($"Tried to initialize an 16 bit BitFlag with a wider bool array");
            return;
        }

        for (int i = 0; i < 16; i++)
            if (bitArray[i]) SetBit(i);
    }

    public static BitFlag16 operator +(BitFlag16 flag, int bit)
    {
        flag.SetBit(bit);
        return flag;
    }

    public static BitFlag16 operator -(BitFlag16 flag, int bit)
    {
        flag.ResetBit(bit);
        return flag;
    }

    public static bool operator ==(BitFlag16 flag, int bit) => flag.CheckBit(bit);

    public static bool operator !=(BitFlag16 flag, int bit) => !flag.CheckBit(bit);

    public void SetBit(int bit)
    {
        bit = Mathf.Clamp(bit, 0, 15);
        flagsBytes |= (ushort)(1 << bit);
    }

    public void ResetBit(int bit)
    {
        bit = Mathf.Clamp(bit, 0, 15);
        flagsBytes &= (ushort)(1 << bit);
    }

    public bool CheckBit(int bit)
    {
        bit = Mathf.Clamp(bit, 0, 15);
        return (flagsBytes & (ushort)(1 << bit)) != 0;
    }

    public bool[] GetBitArray()
    {
        bool[] boolArray = new bool[16];

        for (int i = 0; i < 16; i++) boolArray[i] = CheckBit(i);
        return boolArray;
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder("{ ");

        for (byte j = 0; j < 2; j++)
        {
            stringBuilder.Append("(");

            int size = 7 * (j + 1);
            for (int i = 7 * j; i < size; i++)
                stringBuilder.AppendFormat("{0}, ", CheckBit(i) ? 1 : 0);

            stringBuilder.AppendFormat("{0}) ", CheckBit(size) ? 1 : 0);
        }

        stringBuilder.Append("}");
        return stringBuilder.ToString();
    }
}

[System.Serializable]
public struct BitFlag32 : IBitFlag
{
    [SerializeField] uint flagsBytes;

    public BitFlag32(bool[] bitArray)
    {
        flagsBytes = 0;

        if (bitArray.Length < 32)
        {
            Debug.LogError($"Tried to initialize an 32 bit BitFlag with a wider bool array");
            return;
        }

        for (int i = 0; i < 32; i++)
            if (bitArray[i]) SetBit(i);
    }

    public static BitFlag32 operator +(BitFlag32 flag, int bit)
    {
        flag.SetBit(bit);
        return flag;
    }

    public static BitFlag32 operator -(BitFlag32 flag, int bit)
    {
        flag.ResetBit(bit);
        return flag;
    }

    public static bool operator ==(BitFlag32 flag, int bit) => flag.CheckBit(bit);

    public static bool operator !=(BitFlag32 flag, int bit) => !flag.CheckBit(bit);

    public void SetBit(int bit)
    {
        bit = Mathf.Clamp(bit, 0, 31);
        flagsBytes |= (uint)(1 << bit);
    }

    public void ResetBit(int bit)
    {
        bit = Mathf.Clamp(bit, 0, 31);
        flagsBytes &= (uint)(1 << bit);
    }

    public bool CheckBit(int bit)
    {
        bit = Mathf.Clamp(bit, 0, 31);
        return (flagsBytes & (uint)(1 << bit)) != 0;
    }

    public bool[] GetBitArray()
    {
        bool[] boolArray = new bool[32];

        for (int i = 0; i < 32; i++) boolArray[i] = CheckBit(i);
        return boolArray;
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder("{ ");

        for (byte j = 0; j < 4; j++)
        {
            stringBuilder.Append("(");

            int size = 7 * (j + 1);
            for (int i = 7 * j; i < size; i++)
                stringBuilder.AppendFormat("{0}, ", CheckBit(i) ? 1 : 0);

            stringBuilder.AppendFormat("{0}) ", CheckBit(size) ? 1 : 0);
        }

        stringBuilder.Append("}");
        return stringBuilder.ToString();
    }
}

[System.Serializable]
public struct BitFlag64 : IBitFlag
{
    [SerializeField] ulong flagsBytes;

    public BitFlag64(bool[] bitArray)
    {
        flagsBytes = 0;

        if (bitArray.Length < 64)
        {
            Debug.LogError($"Tried to initialize an 64 bit BitFlag with a wider bool array");
            return;
        }

        for (int i = 0; i < 64; i++)
            if (bitArray[i]) SetBit(i);
    }

    public static BitFlag64 operator +(BitFlag64 flag, int bit)
    {
        flag.SetBit(bit);
        return flag;
    }

    public static BitFlag64 operator -(BitFlag64 flag, int bit)
    {
        flag.ResetBit(bit);
        return flag;
    }

    public static bool operator ==(BitFlag64 flag, int bit) => flag.CheckBit(bit);

    public static bool operator !=(BitFlag64 flag, int bit) => !flag.CheckBit(bit);

    public void SetBit(int bit)
    {
        bit = Mathf.Clamp(bit, 0, 63);
        flagsBytes |= (ulong)1 << bit;
    }

    public void ResetBit(int bit)
    {
        bit = Mathf.Clamp(bit, 0, 63);
        flagsBytes &= (ulong)1 << bit;
    }

    public bool CheckBit(int bit)
    {
        bit = Mathf.Clamp(bit, 0, 63);
        return (flagsBytes & (ulong)(1 << bit)) != 0;
    }

    public bool[] GetBitArray()
    {
        bool[] boolArray = new bool[64];

        for (int i = 0; i < 64; i++) boolArray[i] = CheckBit(i);
        return boolArray;
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder("{ ");

        for (byte j = 0; j < 8; j++)
        {
            stringBuilder.Append("(");

            int size = 7 * (j + 1);
            for (int i = 7 * j; i < size; i++)
                stringBuilder.AppendFormat("{0}, ", CheckBit(i) ? 1 : 0);

            stringBuilder.AppendFormat("{0}) ", CheckBit(size) ? 1 : 0);
        }

        stringBuilder.Append("}");
        return stringBuilder.ToString();
    }
}
