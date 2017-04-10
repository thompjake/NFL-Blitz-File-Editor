   using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFL_Blitz_2000_Roster_Manager.Models
{
    [Serializable]
    public class BlitzGameFile
    {
        public string fileName { get; set; }
        public long fileTableEntryStart { get; set; }
        public long fileOffset { get; set; }
        public long compressedSize { get; set; }
        public long decompressedSize { get; set; }
        public string fileDescription { get; set; }
        public long teamReferenceOffset { get; set; }
    }
}
