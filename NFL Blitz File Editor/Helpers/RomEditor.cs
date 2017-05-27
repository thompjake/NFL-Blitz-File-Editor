using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFL_Blitz_2000_Roster_Manager.Models;
using System.IO;
using System.Globalization;
using System.Collections.ObjectModel;

namespace NFL_Blitz_2000_Roster_Manager.Helpers
{
    public static class RomEditor
    {
        public static Teams ReadRom(string filePath, BlitzGame gameSystem)
        {
            Teams teams = new Teams();
            BlitzGame gameSystemClone = Clone.DeepClone<BlitzGame>(gameSystem);
            for (int x = 0; x < gameSystem.GameTeamCount; x++)
            {
                using (var fs = new FileStream(filePath,
   FileMode.Open,
   FileAccess.ReadWrite))
                {
                    //Get Team Name
                    byte thisByte = 01;
                    List<Byte> listOfBytes = new List<byte>();
                    fs.Position = gameSystemClone.TeamNameOffsetStart + x * gameSystemClone.TeamOffsetIncrement;
                    while (true)
                    {
                        thisByte = byte.Parse(fs.ReadByte().ToString());
                        if (thisByte == 00)
                        {
                            break;
                        }
                        listOfBytes.Add(thisByte);
                    }
                    Team team = new Team();
                    team.TeamName = System.Text.Encoding.ASCII.GetString(listOfBytes.ToArray());
                    teams.Add(team);
                }
            }
            return teams;
        }


        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            string returnValue = hex.ToString();
            return returnValue;
        }

        public static void ReadTeamFiles(string filePath, BlitzGame gameSystem,ref Teams blitzTeams, List<BlitzGameFile> files)
        {

            using (var fs = new FileStream(filePath,
   FileMode.Open,
   FileAccess.ReadWrite))
            {
                for (int y = 0; y < gameSystem.GameTeamCount;y++)
                {
                    int fileIndex = 0;
                    blitzTeams[y].TeamFiles = new List<BlitzGameFile>();
                    // Load Uniform
                    for (int x = 0; x < gameSystem.TeamUniformLength;x++)
                    {
                        fs.Position = gameSystem.TeamUniformOffset + (gameSystem.TeamOffsetIncrement * y) + fileIndex;
                        long fileTableLocation;
                        long.TryParse(ByteArrayToString(new byte[] { (byte)fs.ReadByte(), (byte)fs.ReadByte() }), System.Globalization.NumberStyles.HexNumber, null, out fileTableLocation);
                        fileTableLocation -= 1;
                        blitzTeams[y].TeamFiles.Add(Clone.DeepClone(files[(int)fileTableLocation]));
                        blitzTeams[y].TeamFiles[blitzTeams[y].TeamFiles.Count - 1].fileDescription = gameSystem.UniformFileNameList[x];
                        blitzTeams[y].TeamFiles[blitzTeams[y].TeamFiles.Count - 1].teamReferenceOffset = fs.Position - 2;
                        fileIndex += 2;
                    }
                    // Load Menu items
                    fileIndex = 0;
                    for (int x = 0; x < gameSystem.TeamMenuLength; x++)
                    {
                        fs.Position = gameSystem.TeamMenuOffset + (gameSystem.TeamOffsetIncrement * y) + fileIndex;
                        long fileTableLocation;
                        long.TryParse(ByteArrayToString(new byte[] { (byte)fs.ReadByte(), (byte)fs.ReadByte() }), System.Globalization.NumberStyles.HexNumber, null, out fileTableLocation);
                        fileTableLocation -= 1;
                        blitzTeams[y].TeamFiles.Add(Clone.DeepClone(files[(int)fileTableLocation]));
                        blitzTeams[y].TeamFiles[blitzTeams[y].TeamFiles.Count - 1].fileDescription = gameSystem.MenuSelectFileNameList[x];
                        blitzTeams[y].TeamFiles[blitzTeams[y].TeamFiles.Count - 1].teamReferenceOffset = fs.Position - 2;
                        fileIndex += 2;
                    }

                    // Load in game team items (endzone, ect...)
                    fileIndex = 0;
                    for (int x = 0; x <  gameSystem.TeamInGameLength; x++)
                    {
                        fs.Position = gameSystem.TeamInGameOffset + (gameSystem.TeamOffsetIncrement * y) + fileIndex;
                        long fileTableLocation;
                        long.TryParse(ByteArrayToString(new byte[] { (byte)fs.ReadByte(), (byte)fs.ReadByte() }), System.Globalization.NumberStyles.HexNumber, null, out fileTableLocation);
                        fileTableLocation -= 1;
                        blitzTeams[y].TeamFiles.Add(Clone.DeepClone(files[(int)fileTableLocation]));
                        blitzTeams[y].TeamFiles[blitzTeams[y].TeamFiles.Count - 1].fileDescription = gameSystem.InGameFileNameList[x];
                        blitzTeams[y].TeamFiles[blitzTeams[y].TeamFiles.Count - 1].teamReferenceOffset = fs.Position - 2;
                        fileIndex += 2;
                    }
                }
            }
        }



        public static void WriteStringToFile(string toBeWritten, string filePath, long offset, bool writeToBlank = true)
        {
            using (var fs = new FileStream(filePath,
FileMode.Open,
FileAccess.ReadWrite))
            {
                fs.Position = offset;
                foreach (char letter in toBeWritten)
                {
                    fs.WriteByte(Convert.ToByte(letter));
                }
                //if write to blank is set to true we will write blanks until we hit a blank
                while (writeToBlank)
                {
                    long currentPostiton = fs.Position;
                    byte thisByte = byte.Parse(fs.ReadByte().ToString());
                    if (thisByte == 00)
                    {
                        break;
                    }
                    fs.Position = currentPostiton;
                    fs.WriteByte(00);
                }
            }
        }


        private static void WriteIntToFile(int toBeWritten, string filePath, long offset)
        {

            using (var fs = new FileStream(filePath,
FileMode.Open,
FileAccess.ReadWrite))
            {
                fs.Position = offset;
                var numberByte = Byte.Parse(toBeWritten.ToString(), NumberStyles.HexNumber);
                fs.WriteByte((byte)numberByte);
            }
        }

        public static void ReadFileTable(int toBeWritten, string filePath, long offset)
        {

            using (var fs = new FileStream(filePath,
FileMode.Open,
FileAccess.ReadWrite))
            {
                fs.Position = offset;
                var numberByte = Byte.Parse(toBeWritten.ToString(), NumberStyles.HexNumber);
                fs.WriteByte((byte)numberByte);
            }
        }


        public static bool ByteArrayToFile(string fileName, byte[] byteArray,int offset)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(fileName);
                int index = offset;
                foreach (byte replacementByte in byteArray)
                {
                    fileBytes[index] = replacementByte;
                    index++;
                }
                File.WriteAllBytes(fileName,fileBytes);
                return true;
            }
            catch (Exception _Exception)
            {
                // Error
                Console.WriteLine("Exception caught in process: {0}",
                                  _Exception.ToString());
            }

            // error occured, return false
            return false;
        }

    }
}
