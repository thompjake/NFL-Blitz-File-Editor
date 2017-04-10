using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFL_Blitz_2000_Roster_Manager.Models
{
    [Serializable]
    public class BlitzGame
    {
        public virtual long TeamNameOffsetStart { get; set; }
        public virtual int TeamNameOffsetIncrement { get; set; }

        public virtual string GameName { get; set; }
        public virtual int GameTeamCount { get; set; }

        public virtual long FileSystemOffset { get; set; }
        public virtual int maxFileNameLenght { get; set; }
        public virtual int filePositionLength { get; set; }
        public virtual int decompressedLenght { get; set; }
        public virtual int compressedLenght { get; set; }

        public virtual long TeamUniformOffset { get; set; }
        public virtual int TeamUniformLength { get; set; }
        public virtual int TeamUniformIncrement { get; set; }
        public string[] FileNameList { get; set; }

        public static BlitzGame GetBlitz2000Zoinkity()
        {
            BlitzGame blitzGame2000 = new BlitzGame();
            blitzGame2000.TeamNameOffsetStart = 1176412;
            blitzGame2000.TeamNameOffsetIncrement = 544;

            blitzGame2000.GameTeamCount = 32;
            blitzGame2000.FileSystemOffset = 1379392;
            blitzGame2000.maxFileNameLenght = 20;
            blitzGame2000.filePositionLength = 4;
            blitzGame2000.decompressedLenght = 4;
            blitzGame2000.compressedLenght = 4;

            blitzGame2000.TeamUniformOffset = 1176316;
            blitzGame2000.TeamUniformLength = 40;
            blitzGame2000.TeamUniformIncrement = 544;

            blitzGame2000.FileNameList = new string[20]{
            "Helmet Side Home",
            "Helmet Side Away",
            "Helmet stripe front Home",
            "Helmet stripe front Away",
            "Helmet stripe back Home",
            "Helmet stripe back Away",
            "Mask Home",
            "Mask Away",
            "shoulder Home",
            "shoulder Away",
            "Jersey Home",
            "Jersey Away",
            "Pant's Strip Home",
            "Pant's Strip Away",
            "Pants Home",
            "Pants Away",
            "Socks Home",
            "Sock Away",
            "Jersey Numbers Home",
            "Jersey Numbers Away"
        };
            return blitzGame2000;
        }

    }
}
