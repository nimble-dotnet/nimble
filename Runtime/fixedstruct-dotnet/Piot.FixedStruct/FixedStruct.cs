/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Runtime.InteropServices;

// TODO: Generate this file
namespace Piot.FixedStruct
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct FixedOctets16
    {
        [FieldOffset(0)] public byte a01;
        [FieldOffset(1)] public byte a02;
        [FieldOffset(2)] public byte a03;
        [FieldOffset(3)] public byte a04;
        [FieldOffset(4)] public byte a05;
        [FieldOffset(5)] public byte a06;
        [FieldOffset(6)] public byte a07;
        [FieldOffset(7)] public byte a08;
        [FieldOffset(8)] public byte a09;
        [FieldOffset(9)] public byte a10;
        [FieldOffset(10)] public byte a11;
        [FieldOffset(11)] public byte a12;
        [FieldOffset(12)] public byte a13;
        [FieldOffset(13)] public byte a14;
        [FieldOffset(14)] public byte a15;
        [FieldOffset(15)] public byte a16;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct FixedOctets32
    {
        [FieldOffset(0)] public FixedOctets16 a01;
        [FieldOffset(16)] public FixedOctets16 a02;

        /// <summary>
        /// This is very unsafe to use, only use if it if you are very comfortable with what you are doing
        /// </summary>
        /// <returns></returns>
        public unsafe byte* UnsafePointer()
        {
            fixed (byte* ptr = &a01.a01)
            {
                return ptr;
            }
        }

        public byte this[int index]
        {
            get
            {
                if (index is < 0 or >= 32)
                    throw new IndexOutOfRangeException();

                unsafe
                {
                    fixed (byte* ptr = &a01.a01)
                    {
                        return *(ptr + index);
                    }
                }
            }
            set
            {
                if (index is < 0 or >= 32)
                    throw new IndexOutOfRangeException();

                unsafe
                {
                    fixed (byte* ptr = &a01.a01)
                    {
                        *(ptr + index) = value;
                    }
                }
            }
        }

        public static bool operator ==(FixedOctets32 a, FixedOctets32 b)
        {
            for (var i = 0; i < 32; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        public static bool operator !=(FixedOctets32 a, FixedOctets32 b)
        {
            return !(a == b);
        }

        public void CopyTo(byte[] byteArray)
        {
            if (byteArray.Length < 32)
            {
                throw new Exception($"wrong target size, {byteArray.Length} vs 32");
            }

            unsafe
            {
                fixed (byte* ptr = &a01.a01)
                {
                    for (var i = 0; i < 32; i++)
                    {
                        byteArray[i] = *(ptr + i);
                    }
                }
            }
        }

        public byte[] ToArray()
        {
            var octetArray = new byte[32];
            
            CopyTo(octetArray);

            return octetArray;
        }


        public void CopyFrom(ReadOnlySpan<byte> span)
        {
            if (span.Length < 32)
            {
                throw new Exception($"wrong source size in span {span.Length} vs 32");
            }

            unsafe
            {
                fixed (byte* ptr = &a01.a01)
                {
                    for (var i = 0; i < 32; i++)
                    {
                        *(ptr + i) = span[i];
                    }
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 33)]
    public struct FixedOctets32WithLength
    {
        [FieldOffset(0)] public byte length;
        [FieldOffset(1)] public FixedOctets32 a01;

        public int Length => length;

        public void Clear()
        {
            length = 0;
        }

        public byte this[int index]
        {
            get
            {
                if (index < 0 || index >= length)
                    throw new IndexOutOfRangeException();

                unsafe
                {
                    fixed (byte* ptr = &a01.a01.a01)
                    {
                        return *(ptr + index);
                    }
                }
            }
            set
            {
                if (index < 0 || index >= length)
                    throw new IndexOutOfRangeException();

                unsafe
                {
                    fixed (byte* ptr = &a01.a01.a01)
                    {
                        *(ptr + index) = value;
                    }
                }
            }
        }

        public void CopyFrom(ReadOnlySpan<byte> byteArray)
        {
            unsafe
            {
                length = (byte)byteArray.Length;
                fixed (byte* ptr = &a01.a01.a01)
                {
                    for (var i = 0; i < byteArray.Length; i++)
                    {
                        *(ptr + i) = byteArray[i];
                    }
                }
            }
        }
        
        public byte[] ToArray()
        {
            var octetArray = new byte[length];
            
            CopyTo(octetArray);

            return octetArray;
        }


        public void CopyTo(byte[] byteArray)
        {
            if (byteArray.Length < length)
            {
                throw new Exception($"wrong target size, {byteArray.Length} vs {length}");
            }

            unsafe
            {
                fixed (byte* ptr = &a01.a01.a01)
                {
                    for (var i = 0; i < length; i++)
                    {
                        byteArray[i] = *(ptr + i);
                    }
                }
            }
        }

        public static bool operator ==(FixedOctets32WithLength a, FixedOctets32WithLength b)
        {
            if (a.length != b.length)
                return false;

            if (a.length == 0)
                return true;

            for (var i = 0; i < a.length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        public static bool operator !=(FixedOctets32WithLength a, FixedOctets32WithLength b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FixedOctets32WithLength withLength))
                return false;

            return this == withLength;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}