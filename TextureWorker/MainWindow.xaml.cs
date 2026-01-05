using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Pfim;
using PfimImageFormat = Pfim.ImageFormat;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;
using DirectXTexNet;

namespace TextureWorker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables
        string[] filesPath = new string[] { };
        string[] filesPath_T = new string[] { };
        string[] newFilesPath = new string[] { };
        int starRenameIndex = 0;
        #endregion

        #region Initialization
        public MainWindow()
        {
            InitializeComponent();
        }
        #endregion

        #region Utility Methods
        private string[] AddToArray(string newFile, string[] filesPath)
        {
            List<String> list = new List<String>(filesPath);
            list.Add(newFile);
            filesPath = list.ToArray();
            return filesPath;
        }

        private void UpdateListBox_T()
        {
            My_ListBox_T.Items.Clear();
            foreach (var file in filesPath_T)
            {
                System.Windows.Controls.Image img = new System.Windows.Controls.Image();
                if (IsDds(file))
                {
                    img.Source = CreateBitmapSourceFromDds(file);
                }
                else
                {
                    BitmapImage bi3 = new BitmapImage();
                    bi3.BeginInit();
                    bi3.UriSource = new Uri(file, UriKind.Absolute);
                    bi3.EndInit();
                    img.Source = bi3;
                }
                img.Width = 50;
                img.Height = 50;
                My_ListBox_T.Items.Add(img);

            }
        }

        private void IniMyData_T()
        {
            filesPath_T = new string[] { };
            UpdateListBox_T();
            MessageBox.Show("Done");
            My_T_ParseX.Text = "";
            My_T_ParseY.Text = "";
            My_PsText_T.Text = "Drag and drop images here";
        }
        #endregion

        #region Image Drag-and-Drop

        private void My_ListBox_T_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effects = DragDropEffects.All;
            }
        }

        private void My_ListBox_T_Drop(object sender, DragEventArgs e)
        {
            newFilesPath = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var newFile in newFilesPath)
            {
                if (!filesPath_T.Contains(newFile))
                {
                    string ext = System.IO.Path.GetExtension(newFile);
                    if (ext == ".jpg" || ext == ".png" || ext == ".tiff" || ext == ".wmf" || ext == ".emf" || ext == ".bmp" || ext == ".gif" || ext == ".ico" || ext == ".dds")
                    {
                        filesPath_T = AddToArray(newFile, filesPath_T);
                        UpdateListBox_T();
                        My_PsText_T.Text = "";
                    }
                }
            }
        }

        #endregion

        #region Image Conversion Features

        private void My_B_ConverImage_Click(object sender, RoutedEventArgs e)
        {
            if (My_ComBox.Text == "")
            {
                MessageBox.Show("Please select a format to convert to");
            }
            else
            {
                ConvertImage(My_ComBox.Text);
            }
        }

        private void ConvertImage(string ext)
        {
            if (filesPath_T.Length > 0)
            {
                foreach (var file in filesPath_T)
                {
                    System.Drawing.Bitmap newImage = LoadBitmap(file);
                    try
                    {
                        if (My_T_UseInputRes != null && My_T_UseInputRes.IsChecked == false)
                        {
                            if (!TryGetOutputSize(out int outWidth, out int outHeight))
                            {
                                MessageBox.Show("Please enter resolution");
                                return;
                            }
                            if (newImage.Width != outWidth || newImage.Height != outHeight)
                            {
                                System.Drawing.Bitmap resized = new System.Drawing.Bitmap(newImage, outWidth, outHeight);
                                newImage.Dispose();
                                newImage = resized;
                            }
                        }
                        string newImageFolder = System.IO.Path.GetDirectoryName(file);
                        string newImageName = System.IO.Path.GetFileNameWithoutExtension(file);
                        DirectoryInfo di = Directory.CreateDirectory(newImageFolder + @"/Format Conversion");
                        string saveImagePath = newImageFolder + @"/Format Conversion/" + newImageName + ext;
                        try
                        {
                            if (ext == ".jpg")
                            {
                                newImage.Save(saveImagePath, DrawingImageFormat.Jpeg);
                            }
                            else if (ext == ".png")
                            {
                                newImage.Save(saveImagePath, DrawingImageFormat.Png);
                            }
                            else if (ext == ".dds")
                            {
                                SaveDdsFromBitmap(saveImagePath, newImage);
                            }
                            else if (ext == ".tiff")
                            {
                                newImage.Save(saveImagePath, DrawingImageFormat.Tiff);
                            }
                            else if (ext == ".wmf")
                            {
                                newImage.Save(saveImagePath, DrawingImageFormat.Wmf);
                            }
                            else if (ext == ".emf")
                            {
                                newImage.Save(saveImagePath, DrawingImageFormat.Emf);
                            }
                            else if (ext == ".bmp")
                            {
                                newImage.Save(saveImagePath, DrawingImageFormat.Bmp);
                            }
                            else if (ext == ".gif")
                            {
                                newImage.Save(saveImagePath, DrawingImageFormat.Gif);
                            }
                            else if (ext == ".ico")
                            {
                                using (FileStream FS = File.OpenWrite(saveImagePath))
                                {
                                    System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(newImage, 256, 256);
                                    System.Drawing.Icon.FromHandle(bmp.GetHicon()).Save(FS);
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    finally
                    {
                        newImage.Dispose();
                    }
                }
                IniMyData_T();
            }
            else
            {
                MessageBox.Show("Please drag and drop files first");
            }
        }

        private void My_B_ReReslutation_Click(object sender, RoutedEventArgs e)
        {
            if (filesPath_T.Length > 0)
            {
                if (My_T_ParseX.Text != "" && My_T_ParseY.Text != "")
                {
                    foreach (var file in filesPath_T)
                    {
                        System.Drawing.Bitmap bitmap = LoadBitmap(file);
                        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(bitmap, Int16.Parse(My_T_ParseX.Text), Int16.Parse(My_T_ParseY.Text));
                        SaveImage("Resolution Conversion", file, bmp);
                    }
                    IniMyData_T();
                }
                else
                {
                    MessageBox.Show("Please enter resolution");
                }
            }
            else
            {
                MessageBox.Show("Please drag and drop files first");
            }
        }

        private void SaveImage(string createFolder, string file, System.Drawing.Bitmap bitMap)
        {
            string ext = System.IO.Path.GetExtension(file);
            string newImageFolder = System.IO.Path.GetDirectoryName(file);
            string newImageName = System.IO.Path.GetFileNameWithoutExtension(file);
            DirectoryInfo di = Directory.CreateDirectory(newImageFolder + @"/" + createFolder);
            string saveImagePath = newImageFolder + @"/" + createFolder + @"/" + newImageName + ext;
            if (ext == ".dds")
            {
                SaveDdsFromBitmap(saveImagePath, bitMap);
                return;
            }
            if (ext == ".jpg")
            {
                bitMap.Save(saveImagePath, DrawingImageFormat.Jpeg);
            }
            else if (ext == ".png")
            {
                bitMap.Save(saveImagePath, DrawingImageFormat.Png);
            }
            else if (ext == ".tiff")
            {
                bitMap.Save(saveImagePath, DrawingImageFormat.Tiff);
            }
            else if (ext == ".wmf")
            {
                bitMap.Save(saveImagePath, DrawingImageFormat.Wmf);
            }
            else if (ext == ".emf")
            {
                bitMap.Save(saveImagePath, DrawingImageFormat.Emf);
            }
            else if (ext == ".bmp")
            {
                bitMap.Save(saveImagePath, DrawingImageFormat.Bmp);
            }
            else if (ext == ".gif")
            {
                bitMap.Save(saveImagePath, DrawingImageFormat.Gif);
            }
            else if (ext == ".ico")
            {
                bitMap.Save(saveImagePath, DrawingImageFormat.Icon);
            }
        }

        private void My_B_Gray_Click(object sender, RoutedEventArgs e)
        {
            if (filesPath_T.Length > 0)
            {
                foreach (var file in filesPath_T)
                {
                    System.Drawing.Bitmap bitmap = LoadBitmap(file);
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        for (int y = 0; y < bitmap.Height; y++)
                        {
                            Color originalColor = bitmap.GetPixel(x, y);
                            int grayScale = ((int)originalColor.R * 299 + (int)originalColor.G * 587 + (int)originalColor.B * 114 + 500) / 1000;
                            Color newColor = Color.FromArgb(bitmap.GetPixel(x, y).A, grayScale, grayScale, grayScale);
                            bitmap.SetPixel(x, y, newColor);
                        }
                    }
                    SaveImage("Grayscale Conversion", file, bitmap);
                }
                IniMyData_T();
            }
            else
            {
                MessageBox.Show("Please drag and drop files first");
            }
        }

        private void My_B_Blur_Click(object sender, RoutedEventArgs e)
        {
            if (filesPath_T.Length > 0)
            {
                int blurIntensity = (int)My_T_BlurSlider.Value;
                foreach (var file in filesPath_T)
                {
                    System.Drawing.Bitmap bitmap = LoadBitmap(file);
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        for (int y = 0; y < bitmap.Height; y++)
                        {
                            for (int i = 0; i != blurIntensity; i++)
                            {
                                try
                                {
                                    Color prevX = bitmap.GetPixel(x - blurIntensity, y);
                                    Color nextX = bitmap.GetPixel(x + blurIntensity, y);
                                    Color prevY = bitmap.GetPixel(x, y - blurIntensity);
                                    Color nextY = bitmap.GetPixel(x, y + blurIntensity);

                                    int avgR = (prevX.R + nextX.R + prevY.R + nextY.R) / 4;
                                    int avgG = (prevX.G + nextX.G + prevY.G + nextY.G) / 4;
                                    int avgB = (prevX.B + nextX.B + prevY.B + nextY.B) / 4;
                                    int avgA = (prevX.A + nextX.A + prevY.A + nextY.A) / 4;

                                    bitmap.SetPixel(x, y, Color.FromArgb(avgA, avgR, avgG, avgB));
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }
                    SaveImage("Batch Blur", file, bitmap);
                }
                IniMyData_T();
            }
            else
            {
                MessageBox.Show("Please drag and drop files first");
            }
        }

        private void My_B_Invert_Click(object sender, RoutedEventArgs e)
        {
            if (filesPath_T.Length > 0)
            {
                foreach (var file in filesPath_T)
                {
                    System.Drawing.Bitmap bitmap = LoadBitmap(file);
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        for (int y = 0; y < bitmap.Height; y++)
                        {
                            Color pixel = bitmap.GetPixel(x, y);
                            int red = pixel.R;
                            int green = pixel.G;
                            int blue = pixel.B;
                            int alpha = pixel.A;
                            if (My_T_CB_R.IsChecked == true)
                            {
                                red = 255 - red;
                            }
                            if (My_T_CB_G.IsChecked == true)
                            {
                                green = 255 - green;
                            }
                            if (My_T_CB_B.IsChecked == true)
                            {
                                blue = 255 - blue;
                            }
                            if (My_T_CB_A.IsChecked == true)
                            {
                                blue = 255 - blue;
                            }
                            bitmap.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));
                        }
                    }
                    SaveImage("Batch Invert", file, bitmap);
                }
                IniMyData_T();
            }
            else
            {
                MessageBox.Show("Please drag and drop files first");
            }
        }
        #endregion

        #region DDS Helpers
        private bool IsDds(string file)
        {
            return System.IO.Path.GetExtension(file).Equals(".dds", StringComparison.OrdinalIgnoreCase);
        }

        private System.Drawing.Bitmap LoadBitmap(string file)
        {
            if (!IsDds(file))
            {
                return new System.Drawing.Bitmap(file);
            }

            IImage image = Pfim.Pfim.FromFile(file);
            if (image.Compressed)
            {
                image.Decompress();
            }

            PixelFormat pixelFormat;
            if (image.Format == PfimImageFormat.Rgba32)
            {
                pixelFormat = PixelFormat.Format32bppArgb;
            }
            else if (image.Format == PfimImageFormat.Rgb24)
            {
                pixelFormat = PixelFormat.Format24bppRgb;
            }
            else
            {
                throw new NotSupportedException("Unsupported DDS format: " + image.Format);
            }

            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(image.Width, image.Height, pixelFormat);
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            try
            {
                int srcStride = image.Stride;
                int dstStride = data.Stride;
                int rowBytes = Math.Min(Math.Abs(srcStride), Math.Abs(dstStride));
                IntPtr dst = data.Scan0;
                int srcIndex = 0;
                for (int y = 0; y < image.Height; y++)
                {
                    Marshal.Copy(image.Data, srcIndex, dst, rowBytes);
                    dst = IntPtr.Add(dst, dstStride);
                    srcIndex += srcStride;
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
            return bitmap;
        }

        private BitmapSource CreateBitmapSourceFromDds(string file)
        {
            using (System.Drawing.Bitmap bitmap = LoadBitmap(file))
            {
                IntPtr hBitmap = bitmap.GetHbitmap();
                try
                {
                    return Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private void SaveDdsFromBitmap(string saveImagePath, System.Drawing.Bitmap bitMap)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
            try
            {
                bitMap.Save(tempFile, DrawingImageFormat.Png);
                TexHelper.LoadInstance();
                TexHelper helper = TexHelper.Instance;
                using (ScratchImage scratch = helper.LoadFromWICFile(tempFile, WIC_FLAGS.NONE))
                using (ScratchImage converted = scratch.Convert(DXGI_FORMAT.R8G8B8A8_UNORM, TEX_FILTER_FLAGS.DEFAULT, 0.5f))
                {
                    DXGI_FORMAT targetFormat = GetSelectedDdsFormat();
                    if (targetFormat == DXGI_FORMAT.R8G8B8A8_UNORM)
                    {
                        converted.SaveToDDSFile(DDS_FLAGS.NONE, saveImagePath);
                    }
                    else
                    {
                        using (ScratchImage compressed = converted.Compress(targetFormat, TEX_COMPRESS_FLAGS.DEFAULT, 0.5f))
                        {
                            compressed.SaveToDDSFile(DDS_FLAGS.NONE, saveImagePath);
                        }
                    }
                }
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        private DXGI_FORMAT GetSelectedDdsFormat()
        {
            string selection = (My_DdsFormat != null ? My_DdsFormat.Text : "RGBA32") ?? "RGBA32";
            if (selection == "BC1")
            {
                return DXGI_FORMAT.BC1_UNORM;
            }
            if (selection == "BC3")
            {
                return DXGI_FORMAT.BC3_UNORM;
            }
            if (selection == "BC7")
            {
                return DXGI_FORMAT.BC7_UNORM;
            }
            return DXGI_FORMAT.R8G8B8A8_UNORM;
        }

        private bool TryGetOutputSize(out int width, out int height)
        {
            width = 0;
            height = 0;
            if (string.IsNullOrWhiteSpace(My_T_ParseX.Text) || string.IsNullOrWhiteSpace(My_T_ParseY.Text))
            {
                return false;
            }
            if (!int.TryParse(My_T_ParseX.Text, out width) || !int.TryParse(My_T_ParseY.Text, out height))
            {
                return false;
            }
            if (width <= 0 || height <= 0)
            {
                return false;
            }
            return true;
        }
        #endregion
    }
}
