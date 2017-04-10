using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;


namespace NFL_Blitz_2000_Roster_Manager.Helpers
{
    public class ImageDecoder
    {

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public Bitmap ReadFile(byte[] values)
        {
            int imageWidth;
            int imageHeight;
            if (values[10] != 00)
            {
                imageWidth = int.Parse(ByteArrayToString(new byte[2] { values[10], values[11] }), System.Globalization.NumberStyles.HexNumber);
                imageHeight = int.Parse(ByteArrayToString(new byte[2] { values[14], values[15] }), System.Globalization.NumberStyles.HexNumber);
            }
            else
            {
                imageWidth = values[11];
                imageHeight = values[15];
            }
            switch (values[7])
            {
                case 00:
                    return CI8(values, imageWidth, imageHeight);
                case 08:
                    return C16(values, imageWidth, imageHeight);
               // case 16:
                  //  return CI4(values, imageWidth, imageHeight);
                case 19: //  c16+i4; c16 image padded to 8 bytes, followed by i4 alpha channel padded to even width.
                    // in the future change this out to pull the i4 also.
                    return C16(values, imageWidth, imageHeight);
            }
            return null;
        }



        public Bitmap CI8(byte[] values, int imageWidth, int imageHeight)
        {
            Bitmap b = new Bitmap(imageWidth, imageHeight);
            int imageX = 0;
            int imageY = 0;
            Color[] palette = new Color[256];

            //Get CI8 palette from file
            byte[] ci8Palette = new byte[512];
            Array.Copy(values, values.Length - 512, ci8Palette, 0, 512);

            // Get Palette (RGBA 5551 - C16)
            byte R, G, B, A;
            for (int i = 0; i < 256; i++)
            {
                ushort color = (ushort)((ci8Palette[i * 2] << 8) | (ci8Palette[i * 2 + 1]));
                B = (byte)((color & 0x3E) << 2);
                G = (byte)((color & 0x7C0) >> 3);
                R = (byte)((color & 0xF800) >> 8);
                A = (byte)(0xFF * ((color) & 1));
                palette[i] = Color.FromArgb(A, R, G, B);
            }

            //Draw Image
            int bigInt = 0;
            foreach (byte value in values.Skip(32))
            {
                if (value > bigInt)
                    bigInt = value;
                if (!value.ToString().Equals("0"))
                {
                    try
                    {
                        b.SetPixel(imageX, imageY, palette[value]);
                    }
                    catch (Exception ex)
                    {
                    }
                }
                else
                {
                    b.SetPixel(imageX, imageY, Color.Transparent);
                }
                if (imageX + 1 != imageWidth)
                {
                    imageX++;
                }
                else
                {
                    imageY++;
                    imageX = 0;
                    if (imageY == imageHeight)
                    {
                        break;
                    }
                }

            }
            return b;
        }


        ////todo fix
        //public Bitmap CI4(byte[] values, int imageWidth, int imageHeight)
        //{
        //    Bitmap b = new Bitmap(imageWidth, imageHeight);
        //    int imageX = 0;
        //    int imageY = 0;

        //    // Create palette
        //    int startOfPalette = (imageHeight * imageWidth) / 2 + 32;
        //    List<Color> palette = new List<Color>();
        //    for (int z = startOfPalette; z + 2 <= values.Length; z += 2)
        //    {
        //     //   int pixel = BitsHelper.GetTwoBytes(values, z);
        //        int red = (pixel & 0x7C00) >> 7;
        //        int green = (pixel & 0x03E0) >> 2;
        //        int blue = (pixel & 0x001F) << 3;
        //       // red |= red >> 5;
        //      //  green |= green >> 5;
        //       // blue |= blue >> 5;
        //        int alpha = (pixel & 0x0001) == 1 ? 0xFF : 0x00;
        //        palette.Add(Color.FromArgb(alpha,red,green,blue));
        //    }
        //    List<byte> colorIndex = new List<byte>();
        //    foreach (byte twoIndexs in values.ToList().GetRange(32, (imageHeight * imageWidth) / 2))
        //    {
        //        colorIndex.Add((byte)((twoIndexs & 0xF0) >> 4));
        //        colorIndex.Add((byte)(twoIndexs & 0x0F));
        //    }

        //    foreach (byte value in colorIndex)
        //    {

        //        if (!value.ToString().Equals("0"))
        //        {
        //            try
        //            {
        //                b.SetPixel(imageX, imageY, palette[value]);
        //            }
        //            catch (Exception ex)
        //            {
        //            }
        //        }
        //        else
        //        {
        //            b.SetPixel(imageX, imageY, Color.Transparent);
        //        }
        //        if (imageX + 1 != imageWidth)
        //        {
        //            imageX++;
        //        }
        //        else
        //        {
        //            imageY++;
        //            imageX = 0;
        //            if (imageY == imageHeight)
        //            {
        //                break;
        //            }
        //        }

        //    }
        //    return b;
        //}





        public Bitmap C16(byte[] values, int imageWidth, int imageHeight)
        {
            Bitmap b = new Bitmap(imageWidth, imageHeight);
            int imageX = 0;
            int imageY = 0;
            int startOfPallete = 31;
            for (int z = startOfPallete; z + 2 < values.Length; z += 2)
            {
                int val = BitConverter.ToInt16(values, z);
                int red = (val & 0xF800) >> 8;
                int green = (val & 0x07C0) >> 3;
                int blue = (val & 0x003E) << 2;
                int alpha = (val & 0x0001) == 1 ? 0xFF : 0x00;
                red = (val & 0xF800) >> 8;
                green = (val & 0x07C0) >> 3;
                blue = (val & 0x003E) << 2;
                // Pretty up output
                red |= red >> 5;
                green |= green >> 5;
                blue |= blue >> 5;
                b.SetPixel(imageX, imageY, Color.FromArgb(alpha, red, green, blue));
                if (imageX + 1 != imageWidth)
                {
                    imageX++;
                }
                else
                {
                    imageY++;
                    imageX = 0;
                    if (imageY == imageHeight)
                    {
                        break;
                    }
                }
            }
            return b;
        }


    }
}
