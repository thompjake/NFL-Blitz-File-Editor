﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Globalization;
using NFL_Blitz_2000_Roster_Manager.Models;
using Microsoft.Win32;
using System.Drawing;
using NFL_Blitz_2000_Roster_Manager.Helpers;
using System.ComponentModel;
using N64ImageViewer;
using System.Drawing.Imaging;

namespace NFL_Blitz_2000_Roster_Manager
{
    /// <summary>
    /// Interaction logic for GraphicEditor.xaml
    /// </summary>
    public partial class GraphicEditor : Window, INotifyPropertyChanged
    {

        private string romLocation;
        private BlitzGame gameInfo;
        private Teams blitzTeams;
        private BlitzGraphic selectedGraphic;

        public BlitzGraphic SelectedGraphic
        {
            get { return selectedGraphic; }
            set { selectedGraphic = value; }
        }

        public Teams BlitzTeams
        {
            get { return blitzTeams; }
            set { blitzTeams = value; NotifiyPropertyChanged("BlitzTeams"); }
        }

        public GraphicEditor()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private List<BlitzGameFile> filesSortedByOffset;
        private List<BlitzGameFile> gameFiles;

        private List<BlitzGameFile> ParseBlitzFileList(string romLocation, BlitzGame gameInfo)
        {
            List<BlitzGameFile> midwayDecFileList = new List<BlitzGameFile>();
            using (var fs = new FileStream(romLocation,
FileMode.Open,
FileAccess.ReadWrite))
            {
                int fileTableEntrySize = gameInfo.maxFileNameLenght + gameInfo.filePositionLength + gameInfo.decompressedLenght + gameInfo.compressedLenght;
                int fileCount = BitsHelper.GetNumberFromBytes(BitsHelper.ReadBytesFromFileStream(fs, gameInfo.FileSystemOffset + 12, 4).ToArray());
                long currentOffset = BitsHelper.GetNumberFromBytes(BitsHelper.ReadBytesFromFileStream(fs, gameInfo.FileSystemOffset + 8, 4).ToArray()) + gameInfo.FileSystemOffset;
                for (int x = 0; x < fileCount; x++)
                {
                    int readLocation = 0;

                    // read file table entry
                    List<Byte> fileTableEntry = BitsHelper.ReadBytesFromFileStream(fs, currentOffset, fileTableEntrySize);

                    //Get File Name
                    string fileName = System.Text.Encoding.ASCII.GetString(fileTableEntry.ToArray(), readLocation, gameInfo.maxFileNameLenght).Replace("\0", "");

                    /// Get decompressed file size
                    readLocation += gameInfo.maxFileNameLenght;
                    byte[] decompressedSizeBytes = fileTableEntry.GetRange(readLocation, gameInfo.decompressedLenght).ToArray();
                    Array.Reverse(decompressedSizeBytes);
                    int decompressedSize = BitConverter.ToInt32(decompressedSizeBytes, 0);

                    //File Position 
                    readLocation += gameInfo.decompressedLenght;
                    byte[] filePositionBytes = fileTableEntry.GetRange(readLocation, gameInfo.filePositionLength).ToArray();
                    Array.Reverse(filePositionBytes);
                    long filePosition = BitConverter.ToInt32(filePositionBytes, 0) + gameInfo.FileSystemOffset;

                    /// Get decompressed file size
                    readLocation += gameInfo.filePositionLength;
                    byte[] compressedSizeBytes = fileTableEntry.GetRange(readLocation, gameInfo.compressedLenght).ToArray();
                    Array.Reverse(compressedSizeBytes);
                    int compressedSize = BitConverter.ToInt32(compressedSizeBytes, 0);

                    midwayDecFileList.Add(new BlitzGameFile()
                    {
                        fileName = fileName,
                        decompressedSize = decompressedSize,
                        fileOffset = filePosition,
                        compressedSize = compressedSize,
                        fileTableEntryStart = currentOffset
                    }
                    );
                    currentOffset += fileTableEntrySize;
                }
            }
            return midwayDecFileList;
        }

        private void btnLoadRom_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            Nullable<bool> result = openDialog.ShowDialog();
            if (result == true)
            {
                LoadRom(openDialog.FileName);
            }
        }


        private void LoadRom(string romLocation)
        {
            gameInfo = BlitzGame.GetBlitz2000Zoinkity();
            tbTeamCount.Text = gameInfo.GameTeamCount.ToString();
            this.romLocation = romLocation;
            BlitzTeams = RomEditor.ReadRom(romLocation, gameInfo);
            gameFiles = ParseBlitzFileList(romLocation, gameInfo);
            filesSortedByOffset = Clone.DeepClone(gameFiles).OrderBy(x => x.fileOffset).ToList();
            lbGameFiles.ItemsSource = gameFiles;
            RomEditor.ReadTeamFiles(romLocation, gameInfo, ref blitzTeams, gameFiles);
            lbGameFiles.DisplayMemberPath = "fileName";
            NotifiyPropertyChanged("BlitzTeams");
        }

        private void btnLoadReplacementFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            Nullable<bool> result = openDialog.ShowDialog();
            if (result == true)
            {
                byte[] replacementFile;
                if (openDialog.FileName.ToLower().Contains(".png") || openDialog.FileName.ToLower().Contains(".bmp"))
                {
                    ImageCoder imageCoder = new ImageCoder();
                    imageCoder.Convert(new Bitmap(openDialog.FileName));
                    List<byte> convertedImage = new List<byte>();
                    // testing out using part of existing header TODO
                    BlitzGameFile selectedGame = (BlitzGameFile)lbGameFiles.SelectedItem;
                    BlitzGraphic currentGraphic = GetBlitzGraphic(selectedGame);
                    convertedImage.AddRange(Blitz2000Header.CreateNFLBlitz2000Header(imageCoder.Width, imageCoder.Height, imageCoder.HasAlpha, (byte)imageCoder.n64ImageType, currentGraphic.IRX, currentGraphic.IRY));
                    convertedImage.AddRange(imageCoder.Data);
                    if (imageCoder.Palette != null)
                        convertedImage.AddRange(imageCoder.Palette);
                    replacementFile = convertedImage.ToArray();
                    openDialog.FileName = openDialog.FileName.Split('.')[0] + ".wms";
                    File.WriteAllBytes(openDialog.FileName, convertedImage.ToArray());
                }
                replacementFile = File.ReadAllBytes(openDialog.FileName);
                InsertReplacementFile(replacementFile);
            }
        }

        private void InsertReplacementFile(byte[] replacementFile)
        {
            File.WriteAllBytes("temp.wms", replacementFile);
            byte[] compressedFileBytes = MiniLZO.MiniLZO.Compress(replacementFile);
            int compressedSize = compressedFileBytes.Length;
            if ((compressedSize & 1) != 0)
            {
                List<byte> tempList = compressedFileBytes.ToList();
                tempList.Add(0x00);
                compressedFileBytes = tempList.ToArray();
            }

            int differenceInSize;
            BlitzGameFile selectedGame = (BlitzGameFile)lbGameFiles.SelectedItem;
            int indexOfFileInSortedList = filesSortedByOffset.FindIndex(x => x.fileOffset == selectedGame.fileOffset);
            long currentTableOffset;
            int fileTableEntrySize = gameInfo.maxFileNameLenght + gameInfo.filePositionLength + gameInfo.decompressedLenght + gameInfo.compressedLenght;
            int fileCount;
            using (var fs = new FileStream(romLocation, FileMode.Open, FileAccess.ReadWrite))
            {
                //Update tableOffsetLocation
                currentTableOffset = BitsHelper.GetNumberFromBytes(BitsHelper.ReadBytesFromFileStream(fs, gameInfo.FileSystemOffset + 8, 4).ToArray());// +gameInfo.FileSystemOffset;
                differenceInSize = (int)(compressedFileBytes.Length - selectedGame.compressedSize);
                //helps prevent writing to off addresses TODO: rewrite this logic
                if ((differenceInSize & 1) != 0)
                {
                    differenceInSize++;
                }
                    long newTableOffset = currentTableOffset + differenceInSize;
                byte[] newTableOffsetBytes = BitConverter.GetBytes((Int32)newTableOffset);
                Array.Reverse(newTableOffsetBytes);
                BitsHelper.WritBytesToFileStream(newTableOffsetBytes, fs, gameInfo.FileSystemOffset + 8);
                fileCount = BitsHelper.GetNumberFromBytes(BitsHelper.ReadBytesFromFileStream(fs, gameInfo.FileSystemOffset + 12, 4).ToArray());
            }

            //move the files that are after this entry
            byte[] fullRom = File.ReadAllBytes(romLocation);
            byte[] fileTable = fullRom.ToList().GetRange((int)(currentTableOffset + gameInfo.FileSystemOffset), fileTableEntrySize * fileCount).ToArray();
            int filesAfterStart = (int)filesSortedByOffset[indexOfFileInSortedList + 1].fileOffset;
            byte[] filesAfterNewFile = fullRom.ToList().GetRange(filesAfterStart, (int)(currentTableOffset + gameInfo.FileSystemOffset) - filesAfterStart - 1).ToArray();

            RomEditor.ByteArrayToFile(romLocation, filesAfterNewFile, filesAfterStart + differenceInSize);
            //Write new file
            RomEditor.ByteArrayToFile(romLocation, compressedFileBytes, (int)selectedGame.fileOffset);
            filesSortedByOffset[indexOfFileInSortedList].compressedSize = compressedSize;
            filesSortedByOffset[indexOfFileInSortedList].decompressedSize = replacementFile.Length;
            // fix/write to file table
            RomEditor.ByteArrayToFile(romLocation, fileTable, (int)(currentTableOffset + gameInfo.FileSystemOffset) + differenceInSize);
            AdjustFileTable(indexOfFileInSortedList + 1, differenceInSize);
            WritewFileTableToRom(differenceInSize);
            LoadRom(romLocation);
        }


        private void AdjustFileTable(int positionInTable, int offsetDifference)
        {
            for (int x = positionInTable; x < filesSortedByOffset.Count; x++)
            {
                filesSortedByOffset[x].fileOffset += offsetDifference;
            }
        }

        /// <summary>
        /// TODO: Make sure to write to even offset, odds do not work on real system
        /// </summary>
        /// <param name="offsetDifference"></param>
        private void WritewFileTableToRom(int offsetDifference)
        {
            using (var fs = new FileStream(romLocation,
FileMode.Open,
FileAccess.ReadWrite))
            {
                foreach (BlitzGameFile newEntry in filesSortedByOffset)
                {

                    int writeLocation = (int)newEntry.fileTableEntryStart + offsetDifference;
                    for (int z = 0; z >= newEntry.fileName.Length; z++)
                        newEntry.fileName += "\0";
                    BitsHelper.WriteStringToFileStream(newEntry.fileName, fs, writeLocation);

                    /// Set decompressed file size
                    writeLocation += gameInfo.maxFileNameLenght;
                    byte[] decompressedSizeBytes = BitConverter.GetBytes((Int32)newEntry.decompressedSize);
                    Array.Reverse(decompressedSizeBytes);
                    BitsHelper.WritBytesToFileStream(decompressedSizeBytes, fs, writeLocation);

                    //Set File Offset
                    writeLocation += gameInfo.decompressedLenght;
                    byte[] filePositionBytes = BitConverter.GetBytes((Int32)(newEntry.fileOffset - gameInfo.FileSystemOffset));
                    Array.Reverse(filePositionBytes);
                    BitsHelper.WritBytesToFileStream(filePositionBytes, fs, writeLocation);

                    /// Get decompressed file size
                    writeLocation += gameInfo.filePositionLength;
                    byte[] compressedSizeBytes = BitConverter.GetBytes((Int32)newEntry.compressedSize);
                    Array.Reverse(compressedSizeBytes);
                    BitsHelper.WritBytesToFileStream(compressedSizeBytes, fs, writeLocation);
                }
            }
        }


        private void btnSetTeamFileFromSelectedAllFile_Click(object sender, RoutedEventArgs e)
        {
            if (lbGameFiles.SelectedIndex > -1)
            {
                if (teamView.SelectedItem != null && teamView.SelectedItem.GetType() == typeof(BlitzGameFile))
                {
                    byte[] ba = BitConverter.GetBytes((Int16)(lbGameFiles.SelectedIndex + 1)).Reverse().ToArray();
                    RomEditor.ByteArrayToFile(romLocation, ba, (int)((BlitzGameFile)teamView.SelectedItem).teamReferenceOffset);
                }
            }
            else
            {
                MessageBox.Show("No File Selected");
            }
        }

        private void btnExportSelectedFile_Click(object sender, RoutedEventArgs e)
        {
            if (lbGameFiles.SelectedItem != null)
            {
                SaveFileDialog openDialog = new SaveFileDialog();
                if (((BlitzGameFile)lbGameFiles.SelectedItem).fileName.Split('.')[1].Equals("wms"))
                {
                    openDialog.Filter = "PNG (*.png)|*.png|Blitz N64 Graphic (*.wms)|*.wms";
                    openDialog.DefaultExt = "png";
                    openDialog.FileName = ((BlitzGameFile)lbGameFiles.SelectedItem).fileName.Split('.')[0];
                }
                Nullable<bool> result = openDialog.ShowDialog();
                if (result == true)
                {
                    byte[] fullRom = File.ReadAllBytes(romLocation);
                    byte[] fileBytes = fullRom.ToList().GetRange((int)((BlitzGameFile)lbGameFiles.SelectedItem).fileOffset, (int)((BlitzGameFile)lbGameFiles.SelectedItem).compressedSize).ToArray();
                    byte[] decompressedFileBytes = new byte[(int)((BlitzGameFile)lbGameFiles.SelectedItem).decompressedSize];
                    MiniLZO.MiniLZO.Decompress(fileBytes, decompressedFileBytes);
                    if (openDialog.FileName.Split('.')[1].Equals("png"))
                    {
                        ImageDecoder decoder = new Helpers.ImageDecoder();
                        Bitmap image = decoder.ReadFile(decompressedFileBytes).BlitzImage;
                        if (image != null)
                        {
                            image.Save(openDialog.FileName);
                        }
                        else
                        {
                            MessageBox.Show("We currently do not support exporting this image format");
                        }
                    }
                    else
                    {
                        File.WriteAllBytes(openDialog.FileName, decompressedFileBytes);
                    }
                }
            }
        }

        private void btnInsertNewFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            Nullable<bool> result = openDialog.ShowDialog();
            if (result == true)
            {
                byte[] replacementFile;
                if (openDialog.FileName.ToLower().Contains(".png") || openDialog.FileName.ToLower().Contains(".bmp"))
                {
                    ImageCoder imageCoder = new ImageCoder();
                    imageCoder.Convert(new Bitmap(openDialog.FileName));
                    List<byte> convertedImage = new List<byte>();
                    convertedImage.AddRange(Blitz2000Header.CreateNFLBlitz2000Header(imageCoder.Width, imageCoder.Height, imageCoder.HasAlpha, (byte)imageCoder.n64ImageType));
                    convertedImage.AddRange(imageCoder.Data);
                    if (imageCoder.Palette != null)
                        convertedImage.AddRange(imageCoder.Palette);
                    replacementFile = convertedImage.ToArray();
                    openDialog.FileName = openDialog.FileName.Split('.')[0] + ".wms";
                    File.WriteAllBytes(openDialog.FileName, convertedImage.ToArray());
                }
                replacementFile = File.ReadAllBytes(openDialog.FileName);
                //byte[] compressedFileBytes = MiniLZO.MiniLZO.Compress(replacementFile);
                byte[] compressedFileBytes = MiniLZO.MiniLZO.CompressWithPrecomp2(openDialog.FileName);
                int compressedSize = compressedFileBytes.Length;
                if ((compressedSize & 1) != 0)
                {
                    List<byte> tempList = compressedFileBytes.ToList();
                    tempList.Add(00);
                    compressedFileBytes = tempList.ToArray();
                }
                long currentTableOffset;
                int fileTableEntrySize = gameInfo.maxFileNameLenght + gameInfo.filePositionLength + gameInfo.decompressedLenght + gameInfo.compressedLenght;
                int fileCount;
                using (var fs = new FileStream(romLocation, FileMode.Open, FileAccess.ReadWrite))
                {
                    //Update tableOffsetLocation
                    currentTableOffset = BitsHelper.GetNumberFromBytes(BitsHelper.ReadBytesFromFileStream(fs, gameInfo.FileSystemOffset + 8, 4).ToArray());
                    long newTableOffset = currentTableOffset + compressedFileBytes.Length;
                    byte[] newTableOffsetBytes = BitConverter.GetBytes((Int32)newTableOffset);
                    Array.Reverse(newTableOffsetBytes);
                    BitsHelper.WritBytesToFileStream(newTableOffsetBytes, fs, gameInfo.FileSystemOffset + 8);
                    fileCount = BitsHelper.GetNumberFromBytes(BitsHelper.ReadBytesFromFileStream(fs, gameInfo.FileSystemOffset + 12, 4).ToArray());
                    byte[] newfileCountBytes = BitConverter.GetBytes((Int32)(fileCount + 1));
                    Array.Reverse(newfileCountBytes);
                    BitsHelper.WritBytesToFileStream(newfileCountBytes, fs, gameInfo.FileSystemOffset + 12);
                }

                //move the files that are after this entry
                byte[] fullRom = File.ReadAllBytes(romLocation);
                byte[] fileTable = fullRom.ToList().GetRange((int)(currentTableOffset + gameInfo.FileSystemOffset), fileTableEntrySize * fileCount).ToArray();
                //Write new file
                BlitzGameFile newGameFile = new BlitzGameFile()
                {
                    fileName = "~" + openDialog.FileName.Split('\\').Last(),
                    compressedSize = compressedSize,
                    decompressedSize = replacementFile.Length,
                    fileOffset = currentTableOffset,
                    fileTableEntryStart = currentTableOffset + gameInfo.FileSystemOffset + compressedFileBytes.Length + (fileTableEntrySize * (fileCount))
                };

                RomEditor.ByteArrayToFile(romLocation, compressedFileBytes, (int)newGameFile.fileOffset);
                // fix/write to file table
                RomEditor.ByteArrayToFile(romLocation, fileTable, (int)(currentTableOffset + gameInfo.FileSystemOffset) + compressedFileBytes.Length);
                WriteNewFileTableToRom(newGameFile);
                LoadRom(romLocation);
            }
        }


        private void WriteNewFileTableToRom(BlitzGameFile newGameFile)
        {
            using (var fs = new FileStream(romLocation, FileMode.Open, FileAccess.ReadWrite))
            {

                int writeLocation = (int)newGameFile.fileTableEntryStart;
                int addBlanks = gameInfo.maxFileNameLenght - newGameFile.fileName.Length;
                for (int z = 0; z <= addBlanks; z++)
                    newGameFile.fileName += "\0";
                BitsHelper.WriteStringToFileStream(newGameFile.fileName, fs, writeLocation);

                /// Set decompressed file size
                writeLocation += gameInfo.maxFileNameLenght;
                byte[] decompressedSizeBytes = BitConverter.GetBytes((Int32)newGameFile.decompressedSize);
                Array.Reverse(decompressedSizeBytes);
                BitsHelper.WritBytesToFileStream(decompressedSizeBytes, fs, writeLocation);

                //Set File Offset
                writeLocation += gameInfo.decompressedLenght;
                byte[] filePositionBytes = BitConverter.GetBytes((Int32)(newGameFile.fileOffset - gameInfo.FileSystemOffset));
                Array.Reverse(filePositionBytes);
                BitsHelper.WritBytesToFileStream(filePositionBytes, fs, writeLocation);

                /// Get decompressed file size
                writeLocation += gameInfo.filePositionLength;
                byte[] compressedSizeBytes = BitConverter.GetBytes((Int32)newGameFile.compressedSize);
                Array.Reverse(compressedSizeBytes);
                BitsHelper.WritBytesToFileStream(compressedSizeBytes, fs, writeLocation);
            }
        }


        void NotifiyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void previewImage_Click(object sender, RoutedEventArgs e)
        {
            if (previewImage.Source != null)
            {
                ImagePreview imagePreviewWindow = new ImagePreview((BitmapImage)previewImage.Source);
                imagePreviewWindow.Owner = this;
                this.IsEnabled = false;
                imagePreviewWindow.ShowDialog();
                this.IsEnabled = true;
            }
        }

        /// <summary>
        /// When the file selection changes this shows the files details
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbGameFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BlitzGameFile file = (BlitzGameFile)lbGameFiles.SelectedItem;
            if (lbGameFiles.SelectedItem != null && file.fileName.Split('.')[1].Equals("wms"))
            {

                SetImagePreview(GetBlitzGraphic(file));
            }
            else
            {
                imagePreviewPanel.Visibility = Visibility.Hidden;
            }
        }

        private BlitzGraphic GetBlitzGraphic(BlitzGameFile file)
        {
            ImageDecoder decoder = new ImageDecoder();
            byte[] fullRom = File.ReadAllBytes(romLocation);
            byte[] fileBytes = fullRom.ToList().GetRange((int)file.fileOffset, (int)file.compressedSize).ToArray();
            byte[] decompressedFileBytes = new byte[(int)file.decompressedSize];
            MiniLZO.MiniLZO.Decompress(fileBytes, decompressedFileBytes);
            return decoder.ReadFile(decompressedFileBytes);
        }
        private byte[] GetFileBytes(BlitzGameFile file)
        {
            ImageDecoder decoder = new ImageDecoder();
            byte[] fullRom = File.ReadAllBytes(romLocation);
            byte[] fileBytes = fullRom.ToList().GetRange((int)file.fileOffset, (int)file.compressedSize).ToArray();
            byte[] decompressedFileBytes = new byte[(int)file.decompressedSize];
            MiniLZO.MiniLZO.Decompress(fileBytes, decompressedFileBytes);
            return decompressedFileBytes;
        }

        private void SetImagePreview(BlitzGraphic graphic)
        {
            SelectedGraphic = graphic;
            Bitmap image = graphic.BlitzImage;
            imagePreviewPanel.Visibility = Visibility.Visible;
            if (image != null)
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    image.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    previewImage.Source = bitmapImage;
                }
            }
            else
            {
                previewImage.Source = null;
            }
            cbImageType.SelectedValue = (N64ImageType)Enum.Parse(typeof(N64ImageType),graphic.ImageType);
            tbIRX.Text = graphic.IRX.ToString();
            tbIRY.Text = graphic.IRY.ToString();
        }

        private void teamView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (teamView.SelectedItem.GetType().Equals(typeof(BlitzGameFile)))
            {
                BlitzGameFile file = (BlitzGameFile)teamView.SelectedItem;
                if (teamView.SelectedItem != null && file.fileName.Split('.')[1].Equals("wms"))
                {
                    SetImagePreview(GetBlitzGraphic(file));
                }
                else
                {
                    imagePreviewPanel.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                imagePreviewPanel.Visibility = Visibility.Hidden;
            }
        }

        private void btnUpdateImageValues_Click(object sender, RoutedEventArgs e)
        {
            BlitzGameFile file = (BlitzGameFile)lbGameFiles.SelectedItem;
            byte[] fileBytes = GetFileBytes(file).Skip(32).ToArray();
            List<byte> newHeaderFileBytes = new List<byte>();
            newHeaderFileBytes.AddRange(
                Blitz2000Header.CreateNFLBlitz2000Header
                (selectedGraphic.Width,
                selectedGraphic.Height,
                true,
                (byte)((N64ImageType)Enum.Parse(typeof(N64ImageType), selectedGraphic.ImageType)),
                int.Parse(tbIRX.Text),
                int.Parse(tbIRY.Text)
                ));
            newHeaderFileBytes.AddRange(fileBytes);
            InsertReplacementFile(newHeaderFileBytes.ToArray());
        }

        private void btnReloadTeamFiles_Click(object sender, RoutedEventArgs e)
        {
            if (gameFiles != null)
            {
                int teamCount;
                if (int.TryParse(tbTeamCount.Text, out teamCount))
                {
                    gameInfo.GameTeamCount = teamCount;
                }
                Teams tempTeams = RomEditor.ReadRom(romLocation, gameInfo);
                RomEditor.ReadTeamFiles(romLocation, gameInfo, ref tempTeams, gameFiles);
                BlitzTeams = tempTeams;
            }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            cbImageType.ItemsSource = Enum.GetValues(typeof(N64ImageType)).Cast<N64ImageType>();
        }

        /// <summary>
        /// When a user chnages the selected n64 graphics type, we convert it to that type. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbImageType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbGameFiles.SelectedValue == null)
                return;
            BlitzGraphic currentGraphic = GetBlitzGraphic((BlitzGameFile)lbGameFiles.SelectedItem);
            if (cbImageType.IsDropDownOpen && (N64ImageType)Enum.Parse(typeof(N64ImageType), currentGraphic.ImageType) != (N64ImageType)cbImageType.SelectedValue)
            {
                ImageCoder imageCoder = new ImageCoder();
                imageCoder.ConvertTo(currentGraphic.BlitzImage, (N64ImageType)cbImageType.SelectedValue);
                List<byte> convertedImage = new List<byte>();
                convertedImage.AddRange(Blitz2000Header.CreateNFLBlitz2000Header(imageCoder.Width, imageCoder.Height, imageCoder.HasAlpha, (byte)imageCoder.n64ImageType, currentGraphic.IRX, currentGraphic.IRY));
                convertedImage.AddRange(imageCoder.Data);
                InsertReplacementFile(convertedImage.ToArray());
            }
        }
    }
}
