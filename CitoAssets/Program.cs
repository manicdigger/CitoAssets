using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace CitoAssets
{
    class Program
    {
        static void Main(string[] args)
        {
            int partLength = 1024 * 16;

            string inputPath = args[0];
            string[] files = Directory.GetFiles(inputPath, "*.*", SearchOption.AllDirectories);
            files = files.Where(s => !s.EndsWith("Thumbs.db")).ToArray();
            StringBuilder o = new StringBuilder();
            o.AppendLine("public class Assets");
            o.AppendLine("{");
            o.AppendLine("internal int count;");
            o.AppendLine("internal byte[][] data;");
            o.AppendLine("internal int[] length;");
            o.AppendLine("internal string[] name;");
            o.AppendLine("internal string[] path;");
            o.AppendLine("public Assets()");
            o.AppendLine("{");
            o.AppendLine("AssetsBase64 base64 = new AssetsBase64();");
            o.AppendLine(string.Format("count = {0};", files.Length));
            o.AppendLine(string.Format("byte[] part = new byte[{0}];", partLength));
            o.AppendLine(string.Format("data = new byte[{0}][];", files.Length));
            o.AppendLine(string.Format("length = new int[{0}];", files.Length));
            o.AppendLine(string.Format("name = new string[{0}];", files.Length));
            o.AppendLine(string.Format("path = new string[{0}];", files.Length));
            
            int i = 0;
            foreach (string s in files)
            {
                string path = s.Replace(inputPath, "");
                byte[] data = File.ReadAllBytes(s);
                o.AppendLine(string.Format("name[{0}] = \"{1}\";", i, new FileInfo(s).Name));
                o.AppendLine(string.Format("length[{0}] = {1};", i, data.Length));
                o.AppendLine(string.Format("path[{0}] = \"{1}\";", i, path.Replace("\\", "\\\\")));
                o.AppendLine(string.Format("data[{0}] = new byte[{1}];", i, data.Length));
                int k = 0;
                foreach (byte[] dataPart in Parts(data, partLength))
                {
                    string dataString = Convert.ToBase64String(dataPart);
                    o.AppendLine(string.Format("base64.Decode(data{0}_{1}, {2}, data[{0}], {3});", i, k, dataString.Length, k * partLength));
                    k++;
                }
                i++;
            }
            o.AppendLine("}");
            i = 0;
            foreach (string s in files)
            {
                string path = s.Replace(inputPath, "");
                byte[] data = File.ReadAllBytes(s);
                int k = 0;
                foreach (byte[] dataPart in Parts(data, partLength))
                {
                    o.Append("const ");
                    o.Append(string.Format("string data{0}_{1} = \"", i, k));

                    string dataString = Convert.ToBase64String(dataPart);
                    o.Append(dataString);
                    o.AppendLine("\";");
                    k++;
                }

                i++;
            }
            o.AppendLine("}");

            o.AppendLine(@"
public class AssetsBase64
{
    public AssetsBase64()
    {
        decoding_table = new byte[256];

        for (int i = 0; i < 64; i++)
        {
            decoding_table[encoding_table[i]] = IntToByte(i);
        }
    }
    // http://stackoverflow.com/a/6782480
#if CITO
        const
#else
    static
#endif
 int[] encoding_table = {65, 66, 67, 68, 69, 70, 71, 72,
                                73, 74, 75, 76, 77, 78, 79, 80,
                                81, 82, 83, 84, 85, 86, 87, 88,
                                89, 90, 97, 98, 99, 100, 101, 102,
                                103, 104, 105, 106, 107, 108, 109, 110,
                                111, 112, 113, 114, 115, 116, 117, 118,
                                119, 120, 121, 122, 48, 49, 50, 51,
                                52, 53, 54, 55, 56, 57, 43, 47};

    byte[] decoding_table;

    public int DecodeLength(string data, int inputLength)
    {
        if (inputLength % 4 != 0) return 0;

        int output_length = inputLength / 4 * 3;
        if (data[inputLength - 1] == '=') output_length--;
        if (data[inputLength - 2] == '=') output_length--;

        return output_length;
    }
    
    public void Decode(string data, int inputLength, byte[] output, int outputIndex)
    {
        int output_length = inputLength / 4 * 3;
        if (data[inputLength - 1] == '=') { output_length--; }
        if (data[inputLength - 2] == '=') { output_length--; }

        int i = 0;
        int j = 0;
        for (; i < inputLength; )
        {

            int sextet_a = data[i] == '=' ? 0 & i++ : decoding_table[data[i++]];
            int sextet_b = data[i] == '=' ? 0 & i++ : decoding_table[data[i++]];
            int sextet_c = data[i] == '=' ? 0 & i++ : decoding_table[data[i++]];
            int sextet_d = data[i] == '=' ? 0 & i++ : decoding_table[data[i++]];

            int triple = (sextet_a << 3 * 6)
            + (sextet_b << 2 * 6)
            + (sextet_c << 1 * 6)
            + (sextet_d << 0 * 6);

            if (j < output_length) output[outputIndex + (j++)] = IntToByte((triple >> 2 * 8) & 0xFF);
            if (j < output_length) output[outputIndex + (j++)] = IntToByte((triple >> 1 * 8) & 0xFF);
            if (j < output_length) output[outputIndex + (j++)] = IntToByte((triple >> 0 * 8) & 0xFF);
        }
    }

    public static byte IntToByte(int a)
    {
#if CITO
        return a.LowByte;
#else
        return (byte)a;
#endif
    }
}

");

            File.WriteAllText(args[1], o.ToString());
        }


        public static IEnumerable<byte[]> Parts(byte[] blob, int partsize)
        {
            int i = 0;
            for (; ; )
            {
                if (i >= blob.Length) { break; }
                int currentPartLength = blob.Length - i;
                if (currentPartLength > partsize) { currentPartLength = partsize; }
                byte[] part = new byte[currentPartLength];
                for (int ii = 0; ii < currentPartLength; ii++)
                {
                    part[ii] = blob[i + ii];
                }
                yield return part;
                i += currentPartLength;
            }
        }
    }
}
