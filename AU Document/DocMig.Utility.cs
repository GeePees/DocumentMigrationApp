using System;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace AU_Document
{
    class Utility
    {
        // reverse any string value
        public string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        // convert filestream to base 64 string
        public string ToBase64String(string fileName)
        {
            using (FileStream reader = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[reader.Length];
                reader.Read(buffer, 0, (int)reader.Length);
                reader.Close();
                return Convert.ToBase64String(buffer);
            }
        }

        // convert file to binary
        public byte[] ImageToBinary(string imagePath)
        {
            FileStream fS = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            byte[] b = new byte[fS.Length];
            fS.Read(b, 0, (int)fS.Length);
            fS.Close();
            return b;
        }

        // compress the document binary before storing in ARMNet
        public byte[] Compress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        // reset progress bar
        public void ResetProgressBar(ProgressBar progressBar1)
        {
            // Set Minimum to 1 to represent the first file being copied.
            progressBar1.Minimum = 1;
            // Set the initial value of the ProgressBar.
            progressBar1.Value = 1;
            // Set the Step property to a value of 1 to represent each file being copied.
            progressBar1.Step = 1;
        }
    }
}
