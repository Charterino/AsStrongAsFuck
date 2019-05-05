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

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 1; i--)
            {
                int k = Next(i + 1);
                T tmp = list[k];
                list[k] = list[i];
                list[i] = tmp;
            }
        }
    }
}
