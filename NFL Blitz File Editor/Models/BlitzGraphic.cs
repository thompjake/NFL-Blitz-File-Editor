using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using N64ImageViewer;

namespace NFL_Blitz_2000_Roster_Manager.Models
{
    public class BlitzGraphic
    {
        public Bitmap BlitzImage { get; set; }
        public string ImageType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int IRX { get; set; }
        public int IRY { get; set; }
    }
}
