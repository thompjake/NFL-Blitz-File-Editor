using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFL_Blitz_2000_Roster_Manager.Models
{
    [Serializable]
    public class Player
    {
        private int number;
        public int Number
        {
            get { return number; }
            set { number = value; }
        }

        public string NumberOffset { get; set; }

       private byte skinColor;
       public byte SkinColor
       {
           get { return skinColor; }
           set { skinColor = value; }
       }

       public string SkinColorOffset { get; set; }

       private string lastName;
       public string LastName
       {
           get { return lastName; }
           set { lastName = value; }
       }

       public string LastNameOffset { get; set; }

       private string firstName;
       public string FirstName
       {
           get { return firstName; }
           set { firstName = value; }
       }

       public string FirstNameOffset { get; set; }


       public byte Weight { get; set; }
       public long WeightOffset { get; set; }


       public byte Luck { get; set; }
       public long LuckOffset { get; set; }

       public byte Scale { get; set; }
       public string ScaleOffset { get; set; }
    }
}
