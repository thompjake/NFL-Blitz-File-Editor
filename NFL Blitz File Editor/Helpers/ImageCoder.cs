using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using SharpOcarina;
/* 
 * NTexture.cs / Semi-intelligent N64 texture converter
 * Analyzes image to determine best possible N64 texture format (well, sort of)
 * Written in 2011 by xdaniel
 */

namespace N64ImageViewer
{
    class ImageCoder
    {

        /// <summary>
        /// Texture width in pixels
        /// </summary>
        public int Width;
        /// <summary>
        /// Texture height in pixels
        /// </summary>
        public int Height;
        /// <summary>
        /// N64-side texture type
        /// </summary>
        public byte Type;
        /// <summary>
        /// N64-side texture format
        /// </summary>
        public byte Format;
        /// <summary>
        /// N64-side texture size
        /// </summary>
        public byte Size;
        /// <summary>
        /// Converted N64 texture data
        /// </summary>
        public byte[] Data;
        /// <summary>
        /// Converted N64 palette data, if applicable
        /// </summary>
        public byte[] Palette;
        /// <summary>
        /// Is texture image grayscale? (set by CheckImageProperties)
        /// </summary>
        public bool IsGrayscale;
        /// <summary>
        /// Does texture image have alpha channel? (set by CheckImageProperties)
        /// </summary>
        public bool HasAlpha;

        public N64ImageType n64ImageType;

        /// <summary>
        /// Find and return all unique colors of a bitmap
        /// </summary>
        /// <param name="Image">Bitmap to get colors from</param>
        /// <returns>List of unique colors</returns>
        private List<Color> GetUniqueColors(Bitmap Image)
        {
            List<Color> Colors = new List<Color>();

            for (int X = 0; X < Image.Width; ++X)
                for (int Y = 0; Y < Image.Height; ++Y)
                    Colors.Add(Image.GetPixel(X, Y));

            Colors = Colors.Distinct().ToList();
            return Colors;
        }

        /// <summary>
        /// Checks given bitmap for alpha channel and if color or grayscale
        /// </summary>
        /// <param name="Image">Bitmap to check</param>
        private void CheckImageProperties(Bitmap Image)
        {
            IsGrayscale = true;
            HasAlpha = false;

            Color Pixel;

            for (int X = 0; X < Image.Width; ++X)
                for (int Y = 0; Y < Image.Height; ++Y)
                {
                    Pixel = Image.GetPixel(X, Y);
                    if (Pixel.R != Pixel.G || Pixel.R != Pixel.B || Pixel.G != Pixel.B)
                        IsGrayscale = false;
                    if (Pixel.A != 0xFF)
                        HasAlpha = true;
                }

            Width = Image.Width;
            Height = Image.Height;
        }

        /// <summary>
        /// Checks if the texture's size is valid
        /// </summary>
        /// <returns>True or False, depending on size validity</returns>
        private bool IsSizeValid()
        {
            int[] ValidValues = new int[] { 8, 16, 32, 64, 128, 256, 512 };

            if (Array.Find(ValidValues, element => element == Width) == 0) return false;
            if (Array.Find(ValidValues, element => element == Height) == 0) return false;

            return true;
        }

        /// <summary>
        /// Converts 8-bit RGBA values into 16-bit RGBA5551 value
        /// </summary>
        /// <param name="R">8-bit Red channel</param>
        /// <param name="G">8-bit Green channel</param>
        /// <param name="B">8-bit Blue channel</param>
        /// <param name="A">8-bit Alpha channel</param>
        /// <returns>16-bit RGBA5551 value</returns>
        private ushort ToRGBA5551(byte R, byte G, byte B, byte A)
        {
            return (ushort)((((R) << 8) & 0xF800) | (((G) << 3) & 0x7C0) | (((B) >> 2) & 0x3E) | (((A) >> 7) & 0x1));
        }

        /// <summary>
        /// Generates N64 color palette from a list of colors
        /// </summary>
        /// <param name="Colors">List of color to convert</param>
        /// <param name="ColorCount">Amount of colors</param>
        /// <returns>Byte array containing N64 palette</returns>
        private byte[] GeneratePalette(List<Color> Colors, int ColorCount)
        {
            byte[] Palette = new byte[ColorCount * 2];

            List<ushort> PalEntries = new List<ushort>();
            foreach (Color Col in Colors)
                PalEntries.Add(ToRGBA5551(Col.R, Col.G, Col.B, Col.A));

            PalEntries = PalEntries.Distinct().ToList();
            for (int i = 0, j = 0; i < PalEntries.Count; i++, j += 2)
            {
                Palette[j] = (byte)(PalEntries[i] >> 8);
                Palette[j + 1] = (byte)(PalEntries[i] & 0xFF);
            }

            return Palette;
        }

        /// <summary>
        /// Find 16-bit RGBA5551 value in N64 color palette
        /// </summary>
        /// <param name="Palette">Byte array with N64 palette</param>
        /// <param name="RGBA5551">16-bit RGBA5551 value to search</param>
        /// <returns>Offset of color in N64 palette</returns>
        private int GetPaletteIndex(byte[] Palette, ushort RGBA5551)
        {
            for (int i = 0; i < Palette.Length; i += 2)
            {
                if (RGBA5551 == (ushort)((Palette[i] << 8) | (Palette[i + 1]))) return (i / 2);
            }

            return -1;
        }

        /// <summary>
        /// Converts given ObjFile.imageToConvert into N64 texture
        /// </summary>
        /// <param name="imageToConvert">imageToConvert to convert</param>
        public void Convert(Bitmap imageToConvert, bool checkC16I4 = true)
        {
            // C16-I4 Check, Pretty hacky...
            if (checkC16I4 && imageToConvert.Height % 2 == 0)
            {
                //even height, split it in half
                Bitmap c16, i4;
                c16 = i4 = new Bitmap(imageToConvert.Width, imageToConvert.Height / 2);
                Rectangle rect = new Rectangle(0, 0, imageToConvert.Width, imageToConvert.Height / 2);
                c16 = imageToConvert.Clone(rect, imageToConvert.PixelFormat);
                rect = new Rectangle(0, imageToConvert.Height / 2, imageToConvert.Width, imageToConvert.Height / 2);
                i4 = imageToConvert.Clone(rect, imageToConvert.PixelFormat);

                ImageCoder c16ImageCoder = new ImageCoder();
                c16ImageCoder = new N64ImageViewer.ImageCoder();
                c16ImageCoder.Convert(c16, false);
                ImageCoder i4ImageCoder = new ImageCoder();
                i4ImageCoder.Convert(i4, false);
                if (c16ImageCoder.n64ImageType.Equals(N64ImageType.c16) && i4ImageCoder.n64ImageType.Equals(N64ImageType.i4))
                {
                    n64ImageType = N64ImageType.c16PlusI4;
                    List<byte> tempData = new List<byte>();
                    tempData.AddRange(c16ImageCoder.Data);
                    tempData.AddRange(i4ImageCoder.Data);
                    Data = tempData.ToArray();
                    Width = imageToConvert.Width;
                    Height = imageToConvert.Height / 2; // Height is used for both the C16 and I4 so we need to split it in half, since each image makes up half
                    HasAlpha = c16ImageCoder.HasAlpha;
                    return;
                }
            }
            //End C16-I4 check

            try
            {
                N64ImageType imageType;
                BitmapData RawBmp = null;
                byte[] Raw = null;

                IsGrayscale = false;
                HasAlpha = false;

                CheckImageProperties(imageToConvert);

                try
                {
                    RawBmp = imageToConvert.LockBits(
                        new Rectangle(0, 0, (int)imageToConvert.Width, (int)imageToConvert.Height),
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format32bppArgb
                    );

                    int Size = RawBmp.Height * RawBmp.Stride;
                    Raw = new byte[Size];

                    System.Runtime.InteropServices.Marshal.Copy(RawBmp.Scan0, Raw, 0, Size);
                }
                finally
                {
                    if (RawBmp != null)
                        imageToConvert.UnlockBits(RawBmp);
                }

                //throw new Exception("Too many grayshades in texture OR invalid size");
                List<Color> UniqueColors = GetUniqueColors(imageToConvert);

                if (IsGrayscale == true)
                {
                    if (HasAlpha == true)
                    {
                        /* Convert to IA */
                        if (UniqueColors.Count <= 16)
                        {
#if DEBUG
                            Console.WriteLine("IA 8-bit <- " + "Image to convert" + ", " + imageToConvert.Width.ToString() + "*" + imageToConvert.Height.ToString() + ", " + UniqueColors.Count.ToString() + " grayshades");
#endif
                            /* Set type, IA 8-bit */
                            Format = GBI.G_IM_FMT_IA;
                            Size = GBI.G_IM_SIZ_8b;
                            n64ImageType = N64ImageType.ia8;
                            /* Generate texture buffer */
                            Data = new byte[imageToConvert.Width * imageToConvert.Height];
                            Palette = null;

                            /* Loop through pixels, convert to IA 8-bit, write to texture buffer */
                            for (int i = 0, j = 0; i < Raw.Length; i += 4, j++)
                            {
                                Data[j] = (byte)(((Raw[i] / 16) << 4) | ((Raw[i + 3] / 16) & 0xF));
                            }
                        }
                        else if (UniqueColors.Count <= 256 && imageToConvert.Width * imageToConvert.Height <= 2048)
                        {
#if DEBUG
                            Console.WriteLine("IA 16-bit <- " + "Image to convert" + ", " + imageToConvert.Width.ToString() + "*" + imageToConvert.Height.ToString() + ", " + UniqueColors.Count.ToString() + " grayshades");
#endif
                            /* Set type, IA 16-bit */
                            Format = GBI.G_IM_FMT_IA;
                            Size = GBI.G_IM_SIZ_16b;
                            n64ImageType = N64ImageType.notSupported; //NFL Blitz does not make use of IA16 Images
                            /* Generate texture buffer */
                            Data = new byte[imageToConvert.Width * imageToConvert.Height * 2];
                            Palette = null;

                            /* Loop through pixels, convert to IA 16-bit, write to texture buffer */
                            for (int i = 0, j = 0; i < Raw.Length; i += 4, j += 2)
                            {
                                Data[j] = Raw[i + 2];
                                Data[j + 1] = Raw[i + 3];
                            }
                        }
                        else
                        {
                            /* Uh-oh, too many grayshades OR invalid size! */
                            throw new Exception("Too many grayshades in texture OR invalid size");
                        }
                    }
                    else
                    {
                        /* Convert to I */
                        if (UniqueColors.Count <= 16)
                        {
#if DEBUG
                            Console.WriteLine("I 4-bit <- " + "Image to convert" + ", " + imageToConvert.Width.ToString() + "*" + imageToConvert.Height.ToString() + ", " + UniqueColors.Count.ToString() + " grayshades");
#endif
                            /* Set type, I 4-bit */
                            Format = GBI.G_IM_FMT_I;
                            Size = GBI.G_IM_SIZ_4b;
                            n64ImageType = N64ImageType.i4;
                            /* Generate texture buffer */
                            Data = new byte[(imageToConvert.Width * imageToConvert.Height) / 2];
                            Palette = null;

                            /* Loop through pixels, convert to I 4-bit, write to texture buffer */
                            for (int i = 0, j = 0; i < Raw.Length; i += 8, j++)
                            {
                                Data[j] = (byte)(((Raw[i] / 16) << 4) | ((Raw[i + 4] / 16) & 0xF));
                            }
                        }
                        else if (UniqueColors.Count <= 256 && imageToConvert.Width * imageToConvert.Height <= 4096)
                        {
#if DEBUG
                            Console.WriteLine("I 8-bit <- " + "Image to convert" + ", " + imageToConvert.Width.ToString() + "*" + imageToConvert.Height.ToString() + ", " + UniqueColors.Count.ToString() + " grayshades");
#endif
                            /* Set type, I 8-bit */
                            Format = GBI.G_IM_FMT_I;
                            Size = GBI.G_IM_SIZ_8b;
                            n64ImageType = N64ImageType.notSupported; //NFL Blitz does not make use of I8 Images
                            /* Generate texture buffer */
                            Data = new byte[imageToConvert.Width * imageToConvert.Height];
                            Palette = null;

                            /* Loop through pixels, convert to I 8-bit, write to texture buffer */
                            for (int i = 0, j = 0; i < Raw.Length; i += 4, j++)
                            {
                                Data[j] = Raw[i];
                            }
                        }
                        else
                        {
                            /* Uh-oh, too many grayshades OR invalid size! */
                            throw new Exception("Too many grayshades in texture OR invalid size");
                        }
                    }
                }
                else
                {
                    /* Convert to CI */
                    if (UniqueColors.Count <= 16)
                    {
#if DEBUG
                        Console.WriteLine("CI 4-bit <- " + "Image to convert" + ", " + imageToConvert.Width.ToString() + "*" + imageToConvert.Height.ToString() + ", " + UniqueColors.Count.ToString() + " unique colors");
#endif
                        /* Set type, CI 4-bit */
                        Format = GBI.G_IM_FMT_CI;
                        Size = GBI.G_IM_SIZ_4b;
                        n64ImageType = N64ImageType.ci4;
                        /* Generate texture buffer */
                        Data = new byte[(imageToConvert.Width * imageToConvert.Height) / 2];

                        /* Generate 16-color RGBA5551 palette */
                        Palette = GeneratePalette(UniqueColors, 16);

                        /* Loop through pixels, get palette indexes, write to texture buffer */
                        for (int i = 0, j = 0; i < Raw.Length; i += 8, j++)
                        {
                            ushort RGBA5551_1 = ToRGBA5551(Raw[i + 2], Raw[i + 1], Raw[i], Raw[i + 3]);
                            ushort RGBA5551_2 = ToRGBA5551(Raw[i + 6], Raw[i + 5], Raw[i + 4], Raw[i + 7]);
                            byte Value = (byte)(
                                ((GetPaletteIndex(Palette, RGBA5551_1)) << 4) |
                                ((GetPaletteIndex(Palette, RGBA5551_2) & 0xF)));
                            Data[j] = Value;
                        }
                    }
                    else if (UniqueColors.Count <= 256)
                    {
#if DEBUG
                        Console.WriteLine("CI 8-bit <- " + "Image to convert" + ", " + imageToConvert.Width.ToString() + "*" + imageToConvert.Height.ToString() + ", " + UniqueColors.Count.ToString() + " unique colors");
#endif
                        /* Set type, CI 8-bit */
                        Format = GBI.G_IM_FMT_CI;
                        Size = GBI.G_IM_SIZ_8b;
                        n64ImageType = N64ImageType.ci8;
                        /* Generate texture buffer
                         * NFL BLitz requires CI8 to be padded to 8 bytes */
                        int dataSize = (imageToConvert.Width * imageToConvert.Height);
                        while (dataSize % 8 != 0)
                        {
                            dataSize++;
                        }
                        Data = new byte[dataSize];

                        /* Generate 256-color RGBA5551 palette */ //setting it to 256 works for editor chaneg back to UniqueColors.Count
                        Palette = GeneratePalette(UniqueColors, 256);

                        /* Loop through pixels, get palette indexes, write to texture buffer */
                        for (int i = 0, j = 0; i < Raw.Length; i += 4, j++)
                        {
                            ushort RGBA5551 = ToRGBA5551(Raw[i + 2], Raw[i + 1], Raw[i], Raw[i + 3]);
                            Data[j] = (byte)GetPaletteIndex(Palette, RGBA5551);
                        }
                    }
                    else
                    {
                        /* Convert to RGBA */
#if DEBUG
                        Console.WriteLine("RGBA 16-bit <- " + "Image to convert" + ", " + imageToConvert.Width.ToString() + "*" + imageToConvert.Height.ToString());
#endif
                        /* Set type, RGBA 16-bit */
                        Format = GBI.G_IM_FMT_RGBA;
                        Size = GBI.G_IM_SIZ_16b;
                        n64ImageType = N64ImageType.c16;
                        /* Generate texture buffer */
                        Data = new byte[imageToConvert.Width * imageToConvert.Height * 2];
                        Palette = null;

                        /* Loop through pixels, convert to RGBA5551, write to texture buffer */
                        for (int i = 0, j = 0; i < Raw.Length; i += 4, j += 2)
                        {
                            ushort RGBA5551 = ToRGBA5551(Raw[i + 2], Raw[i + 1], Raw[i], Raw[i + 3]);
                            Data[j] = (byte)(RGBA5551 >> 8);
                            Data[j + 1] = (byte)(RGBA5551 & 0xFF);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //   System.Windows.Forms.MessageBox.Show("imageToConvert '" + imageToConvert.DisplayName + "': " + ex.Message, "Exception", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                //  SetInvalidTexture(imageToConvert);
            }

            /* Pack texture type */
            Type = (byte)((Format << 5) | (Size << 3));
        }

    }
}
