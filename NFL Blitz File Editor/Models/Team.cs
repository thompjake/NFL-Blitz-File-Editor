using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace NFL_Blitz_2000_Roster_Manager.Models
{
    [Serializable]
    public class Team
    {
        #region vars and matching properties
        /// <summary>
        /// Name of the Blitz team
        /// </summary>
        public string TeamName { get;  set; }
        public string TeamNameOffset { get; set; }
        public List<BlitzGameFile> TeamFiles { get; set; }
        #endregion
    }
}
