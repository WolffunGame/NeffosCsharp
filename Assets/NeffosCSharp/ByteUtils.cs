using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeffosCSharp
{
    public static class ByteUtils
    {
        public static byte[] Join(byte[] separateBytes, params byte[][] values)
        {
            int resultLength = 0;
            foreach(var value in values)
            {
                resultLength += value.Length;
            }

            var result = new byte[resultLength + separateBytes.Length * (values.Length - 1)];

            int currentOffset = 0;

            for(int i=0; i< values.Length; i++)
            {
                var value = values[i];

                if(i != 0)
                {
                    Buffer.BlockCopy(separateBytes, 0, result, currentOffset, separateBytes.Length);
                    currentOffset += separateBytes.Length;
                }

                Buffer.BlockCopy(value, 0, result, currentOffset, value.Length);
                currentOffset += value.Length;
            }

            return result;
        }

        public static byte[][] Split(byte seperateByte, byte[] value)
        {
            List<byte[]> result = new List<byte[]>();

            List<byte> currentChunk = new List<byte>();

            foreach(var element in value)
            {
                if(element != seperateByte)
                {
                    currentChunk.Add(element);
                }
                else
                {
                    result.Add(currentChunk.ToArray());
                    currentChunk.Clear();
                }
            }

            result.Add(currentChunk.ToArray());

            return result.ToArray();
        }
    }
}
