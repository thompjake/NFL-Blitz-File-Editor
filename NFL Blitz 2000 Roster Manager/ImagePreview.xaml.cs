using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

namespace NFL_Blitz_2000_Roster_Manager
{
    /// <summary>
    /// Interaction logic for ImagePreview.xaml
    /// </summary>
    public partial class ImagePreview : Window
    {
        public ImagePreview(BitmapImage image)
        {
            InitializeComponent();
            previewImage.Width = image.Width;
            previewImage.Height = image.Height;      
            previewImage.Source = image;
        }
    }
}
