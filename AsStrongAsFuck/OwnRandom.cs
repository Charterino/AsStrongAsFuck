using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AsStrongAsFuck
{
    public class OwnRandom : RandomNumberGenerator
    {
        private static RandomNumberGenerator r;

        public OwnRandom()
        {
            r = RandomNumberGenerator.Create();
        }

        public override void GetBytes(byte[] buffer)
        {
            r.GetBytes(buffer);
        }

        public double NextDouble()
        {
            byte[] b = new byte[4];
            r.GetBytes(b);
            return (double)BitConverter.ToUInt32(b, 0) / UInt32.MaxValue;
        }

        public int Next(int minValue, int maxValue)
        {
            return (int)Math.Floor(NextDouble() * (maxValue - minValue)) + minValue;
        }

        public int Next()
        {
            return Next(0, Int32.MaxValue);
        }

        public int Next(int maxValue)
        {
            return Next(0, maxValue);
        }

        public byte NextByte()
        {
            return (Byte)Next(Byte.MaxValue);
        }

        public uint NextUInt32()
        {
            return (uint)Next() * 2;
        }
    }
}
