using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace NFL_Blitz_2000_Roster_Manager.Helpers
{
   public static class BitsHelper
    {
       public static string ByteArrayToString(byte[] ba)
       {
           StringBuilder hex = new StringBuilder(ba.Length * 2);
           foreach (byte b in ba)
               hex.AppendFormat("{0:x2}", b);
           return hex.ToString();
       }

       public static int GetNumberFromBytes(byte[] ba)
       {
           return int.Parse(ByteArrayToString(ba), System.Globalization.NumberStyles.HexNumber);
       }

       public static List<byte> ReadBytesFromFileStream(FileStream fs,long offset, int count)
       {
           List<Byte> byteList = new List<byte>();
           for (long y = offset; y < count + offset; y++)
           {
               fs.Position = y;
               byte thisByte = byte.Parse(fs.ReadByte().ToString());
               byteList.Add(thisByte);
           }
           return byteList;
       }

       public static void WritBytesToFileStream(byte[] toBeWritten, FileStream fs, long offset)
       {
               fs.Position = offset;
               //if write to blank is set to true we will write blanks until we hit a blank
               foreach (byte byteToWrite in toBeWritten)
               {
                   fs.WriteByte(byteToWrite);
               }
       }

       public static byte[] intToByteArray(int value)
       {
           return new byte[] {
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        };
       }


       public static void WriteStringToFileStream(string toBeWritten,FileStream fs, long offset)
       {
           
               fs.Position = offset;
               foreach (char letter in toBeWritten)
               {
                   fs.WriteByte(Convert.ToByte(letter));
               }
           }


    }
}
