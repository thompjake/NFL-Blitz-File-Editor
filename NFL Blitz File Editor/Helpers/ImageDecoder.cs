using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using NFL_Blitz_2000_Roster_Manager.Models;

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

        public BlitzGraphic ReadFile(byte[] values)
        {
            int imageWidth;
            int imageHeight;
            int irx;
            int iry;
            //if (values[10] != 00)
            //{
            imageWidth = int.Parse(ByteArrayToString(new byte[2] { values[10], values[11] }), System.Globalization.NumberStyles.HexNumber);
            imageHeight = int.Parse(ByteArrayToString(new byte[2] { values[14], values[15] }), System.Globalization.NumberStyles.HexNumber);
            irx = int.Parse(ByteArrayToString(new byte[2] { values[19], values[20] }), System.Globalization.NumberStyles.HexNumber);
            iry = int.Parse(ByteArrayToString(new byte[2] { values[23], values[24] }), System.Globalization.NumberStyles.HexNumber);
            //}
            //else
            //{
            //    imageWidth = values[11];
            //    imageHeight = values[15];
            //}
            switch (values[7])
            {
                case 00:
                    return new BlitzGraphic() { ImageType = "ci8", BlitzImage = CI8(values, imageWidth, imageHeight), Width = imageWidth, Height = imageHeight, IRX = irx, IRY = iry };
                case 08:
                    return new BlitzGraphic() { ImageType = "c16", BlitzImage = C16(values, imageWidth, imageHeight), Width = imageWidth, Height = imageHeight, IRX = irx, IRY = iry };
                case 16:
                    return new BlitzGraphic() { ImageType = "cI4", BlitzImage = CI4(values, imageWidth, imageHeight), Width = imageWidth, Height = imageHeight, IRX = irx, IRY = iry };
                case 17:
                    return new BlitzGraphic() { ImageType = "i4", BlitzImage = I4(values, imageWidth, imageHeight), Width = imageWidth, Height = imageHeight, IRX = irx, IRY = iry };
                case 19:
                    return new BlitzGraphic() { ImageType = "c16PlusI4", BlitzImage = C16I4(values, imageWidth, imageHeight), Width = imageWidth, Height = imageHeight, IRX = irx, IRY = iry };
            }
            return new BlitzGraphic() { ImageType = "?", Width = imageWidth, Height = imageHeight, BlitzImage = null };
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


        public Bitmap CI4(byte[] values, int imageWidth, int imageHeight)
        {
            // round width up to nearest even number
            Bitmap b = new Bitmap((imageWidth % 2 == 0) ? imageWidth : (imageWidth + 1), imageHeight);
            int imageX = 0;
            int imageY = 0;


            // Get 4 bit indices
            List<int> imagesIndices = new List<int>();
            foreach (byte value in values.Skip(32))
            {
                imagesIndices.Add(value >> 4);
                imagesIndices.Add(value & 0x0F);
            }

            Color[] palette = new Color[16];

            //Get CI4 palette from file
            byte[] ci4Palette = new byte[32];
            Array.Copy(values, values.Length - 32, ci4Palette, 0, 32);

            // Get Palette (RGBA 5551 - C16)
            byte R, G, B, A;
            for (int i = 0; i < 16; i++)
            {
                ushort color = (ushort)((ci4Palette[i * 2] << 8) | (ci4Palette[i * 2 + 1]));
                B = (byte)((color & 0x3E) << 2);
                G = (byte)((color & 0x7C0) >> 3);
                R = (byte)((color & 0xF800) >> 8);
                A = (byte)(0xFF * ((color) & 1));
                palette[i] = Color.FromArgb(A, R, G, B);
            }


            //Draw Image
            foreach (int value in imagesIndices)
            {
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
                if (imageX + 1 != b.Width)
                {
                    imageX++;
                }
                else
                {
                    imageY++;
                    imageX = 0;
                    if (imageY == b.Height)
                    {
                        break;
                    }
                }

            }
            return b;
        }

        /// <summary>
        /// The I4 format is used for storing 4 bit intensity values (each pixel is composed of just one value).
        /// Conversion to RGBA is achieved by multiplying the 4 bit value by 0x11 and then setting each color component to this value. 
        /// The alpha component is set to 0xff.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="imageWidth"></param>
        /// <param name="imageHeight"></param>
        /// <returns></returns>
        public Bitmap I4(byte[] values, int imageWidth, int imageHeight)
        {
            // round width up to nearest even number
            Bitmap b = new Bitmap((imageWidth % 2 == 0) ? imageWidth : (imageWidth + 1), imageHeight);
            int imageX = 0;
            int imageY = 0;


            // Get 4 bit indices
            List<int> imagesIndices = new List<int>();
            foreach (byte value in values.Skip(32))
            {
                imagesIndices.Add(value >> 4);
                imagesIndices.Add((value & 0x0F));
            }
            //Draw Image
            foreach (int value in imagesIndices)
            {
                if (!value.ToString().Equals("0"))
                {
                    try
                    {
                        b.SetPixel(imageX, imageY, Color.FromArgb(255, value * 0x11, value * 0x11, value * 0x11));
                    }
                    catch (Exception ex)
                    {
                    }
                }
                else
                {
                    b.SetPixel(imageX, imageY, Color.Black);
                }
                if (imageX + 1 != b.Width)
                {
                    imageX++;
                }
                else
                {
                    imageY++;
                    imageX = 0;
                    if (imageY == b.Height)
                    {
                        break;
                    }
                }

            }
            return b;
        }



        public Bitmap C16(byte[] values, int imageWidth, int imageHeight)
        {
            Bitmap b = new Bitmap(imageWidth, imageHeight);
            int imageX = 0;
            int imageY = 0;
            int startOfPallete = 31;
            int imageSize = startOfPallete + imageHeight * imageWidth * 2;
            for (int z = startOfPallete; z + 2 < imageSize; z += 2)
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

        public Bitmap C16I4(byte[] values, int imageWidth, int imageHeight)
        {
            Bitmap b = new Bitmap(imageWidth, imageHeight * 2);
            Bitmap c16Image = C16(values, imageWidth, imageHeight);
            Bitmap i4Image = I4(values.Skip(imageWidth * imageHeight * 2).ToArray(), imageWidth, imageHeight);
            using (Graphics grfx = Graphics.FromImage(b))
            {
                grfx.DrawImage(c16Image, 0, 0);
                grfx.DrawImage(i4Image, 0, c16Image.Height);
            }
            return b;
        }


    }
}
