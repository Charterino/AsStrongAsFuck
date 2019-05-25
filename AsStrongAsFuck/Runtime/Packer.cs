using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AsStrongAsFuck.Runtime
{
    public class Packer
    {
        public static void Initialize(int len)
        {
            array = new byte[len];

            const int BUFFER_SIZE = 256;
            byte[] tempArray = new byte[BUFFER_SIZE];
            List<byte[]> tempList = new List<byte[]>();
            int count = 0, length = 0;
            MemoryStream ms = new MemoryStream(array);
            DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress);
            while ((count = ds.Read(tempArray, 0, BUFFER_SIZE)) > 0)
            {
                if (count == BUFFER_SIZE)
                {
                    tempList.Add(tempArray);
                    tempArray = new byte[BUFFER_SIZE];
                }
                else
                {
                    byte[] temp = new byte[count];
                    Array.Copy(tempArray, 0, temp, 0, count);
                    tempList.Add(temp);
                }
                length += count;
            }
            byte[] retVal = new byte[length];
            count = 0;
            foreach (byte[] temp in tempList)
            {
                Array.Copy(temp, 0, retVal, count, temp.Length);
                count += temp.Length;
            }
            array = retVal;
        }

        public static void Set()
        {
            array[0] = 0;
        }

        public static void Load()
        {
            Assembly.Load(array);
        }

        public static byte[] array = new byte[] { };
    }
}
