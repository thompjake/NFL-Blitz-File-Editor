using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFL_Blitz_2000_Roster_Manager.Helpers;

namespace N64ImageViewer
{
    public static class Blitz2000Header
    {
        private static readonly int headerSize = 32;
        private static readonly int widthHeaderIndex = 8;
        private static readonly int heightHeaderIndex = 12;
        private static readonly int imageTypeIndex = 7;
        private static readonly int typeIndex = 0;
        private static readonly int versionIndex = 2;
        private static readonly int flagIndex = 3;
        private static readonly byte[] type = new byte[]{0x77,0x6E};
        private static readonly int version = 2;

        public static byte[] CreateNFLBlitz2000Header(int width, int height, bool containsAlpha,byte imageType)
        {
            List<byte> header = new List<byte>(new byte[headerSize]);
            header[typeIndex] = type[0];
            header[typeIndex + 1] = type[1];
            header[versionIndex] = (byte)version;
            if(containsAlpha)
                header[flagIndex] = 0x04;
            header[imageTypeIndex] = imageType;
            header[widthHeaderIndex] =(byte)width;
            for (int x = widthHeaderIndex; x < widthHeaderIndex + 4;x++)
                header[x] = BitsHelper.intToByteArray(width)[x - widthHeaderIndex];
            for (int x = heightHeaderIndex; x < heightHeaderIndex + 4; x++)
                header[x] = BitsHelper.intToByteArray(height)[x - heightHeaderIndex];
            return header.ToArray();
        }

    }

}
