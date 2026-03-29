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
using Microsoft.Win32;

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
        string baseInputFolder = "";
        string outputFolder = "";
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

        private string[] GetImageFilesFromDirectory(string directoryPath)
        {
            List<string> imageFiles = new List<string>();
            string[] extensions = { ".jpg", ".jpeg", ".png", ".tiff", ".tif", ".wmf", ".emf", ".bmp", ".gif", ".ico", ".dds" };
            
            try
            {
                foreach (string ext in extensions)
                {
                    string[] files = Directory.GetFiles(directoryPath, "*" + ext, SearchOption.AllDirectories);
                    imageFiles.AddRange(files);
                }
            }
            catch (Exception)
            {
            }
            
            return imageFiles.ToArray();
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
                // Check if it's a directory
                if (Directory.Exists(newFile))
                {
                    string[] imageFiles = GetImageFilesFromDirectory(newFile);
                    foreach (var imageFile in imageFiles)
                    {
                        if (!filesPath_T.Contains(imageFile))
                        {
                            filesPath_T = AddToArray(imageFile, filesPath_T);
                        }
                    }
                }
                else if (!filesPath_T.Contains(newFile))
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
            UpdateListBox_T();
            My_PsText_T.Text = "";
        }

        #endregion

        #region Folder Selection
        private void My_B_SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select folder with images to convert";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string[] imageFiles = GetImageFilesFromDirectory(dialog.SelectedPath);
                    foreach (var imageFile in imageFiles)
                    {
                        if (!filesPath_T.Contains(imageFile))
                        {
                            filesPath_T = AddToArray(imageFile, filesPath_T);
                        }
                    }
                    UpdateListBox_T();
                    baseInputFolder = dialog.SelectedPath;
                    My_PsText_T.Text = $"Loaded {imageFiles.Length} images from folder";
                }
            }
        }

        private bool SelectOutputFolder(out string folderPath)
        {
            folderPath = "";
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select output folder for converted images";
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    folderPath = dialog.SelectedPath;
                    return true;
                }
            }
            return false;
        }

        private string GetOutputPath(string originalFilePath, string outputBaseFolder, string subFolderName = "")
        {
            if (string.IsNullOrEmpty(baseInputFolder))
            {
                baseInputFolder = Path.GetDirectoryName(originalFilePath);
            }

            string relativePath = Path.GetRelativePath(baseInputFolder, Path.GetDirectoryName(originalFilePath));
            string targetFolder = string.IsNullOrEmpty(subFolderName)
                ? Path.Combine(outputBaseFolder, relativePath)
                : Path.Combine(outputBaseFolder, subFolderName, relativePath);

            Directory.CreateDirectory(targetFolder);

            string fileName = Path.GetFileName(originalFilePath);
            return Path.Combine(targetFolder, fileName);
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
                if (!SelectOutputFolder(out outputFolder))
                {
                    return;
                }

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

                        string newImageName = System.IO.Path.GetFileNameWithoutExtension(file);
                        string outputFilePath = GetOutputPath(file, outputFolder, "Format Conversion");
                        outputFilePath = Path.ChangeExtension(outputFilePath, ext);

                        try
                        {
                            if (ext == ".jpg")
                            {
                                newImage.Save(outputFilePath, DrawingImageFormat.Jpeg);
                            }
                            else if (ext == ".png")
                            {
                                newImage.Save(outputFilePath, DrawingImageFormat.Png);
                            }
                            else if (ext == ".dds")
                            {
                                SaveDdsFromBitmap(outputFilePath, newImage);
                            }
                            else if (ext == ".tiff")
                            {
                                newImage.Save(outputFilePath, DrawingImageFormat.Tiff);
                            }
                            else if (ext == ".wmf")
                            {
                                newImage.Save(outputFilePath, DrawingImageFormat.Wmf);
                            }
                            else if (ext == ".emf")
                            {
                                newImage.Save(outputFilePath, DrawingImageFormat.Emf);
                            }
                            else if (ext == ".bmp")
                            {
                                newImage.Save(outputFilePath, DrawingImageFormat.Bmp);
                            }
                            else if (ext == ".gif")
                            {
                                newImage.Save(outputFilePath, DrawingImageFormat.Gif);
                            }
                            else if (ext == ".ico")
                            {
                                using (FileStream FS = File.OpenWrite(outputFilePath))
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
                MessageBox.Show($"Conversion complete! Output folder: {outputFolder}");
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
                    if (!SelectOutputFolder(out outputFolder))
                    {
                        return;
                    }

                    foreach (var file in filesPath_T)
                    {
                        System.Drawing.Bitmap bitmap = LoadBitmap(file);
                        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(bitmap, Int16.Parse(My_T_ParseX.Text), Int16.Parse(My_T_ParseY.Text));
                        SaveImageWithStructure(file, bmp, "Resolution Conversion");
                        bmp.Dispose();
                        bitmap.Dispose();
                    }
                    MessageBox.Show($"Resolution conversion complete! Output folder: {outputFolder}");
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

        private void SaveImageWithStructure(string file, System.Drawing.Bitmap bitMap, string subFolderName)
        {
            string ext = System.IO.Path.GetExtension(file);
            string outputFilePath = GetOutputPath(file, outputFolder, subFolderName);

            if (ext == ".dds")
            {
                SaveDdsFromBitmap(outputFilePath, bitMap);
                return;
            }
            if (ext == ".jpg")
            {
                bitMap.Save(outputFilePath, DrawingImageFormat.Jpeg);
            }
            else if (ext == ".png")
            {
                bitMap.Save(outputFilePath, DrawingImageFormat.Png);
            }
            else if (ext == ".tiff")
            {
                bitMap.Save(outputFilePath, DrawingImageFormat.Tiff);
            }
            else if (ext == ".wmf")
            {
                bitMap.Save(outputFilePath, DrawingImageFormat.Wmf);
            }
            else if (ext == ".emf")
            {
                bitMap.Save(outputFilePath, DrawingImageFormat.Emf);
            }
            else if (ext == ".bmp")
            {
                bitMap.Save(outputFilePath, DrawingImageFormat.Bmp);
            }
            else if (ext == ".gif")
            {
                bitMap.Save(outputFilePath, DrawingImageFormat.Gif);
            }
            else if (ext == ".ico")
            {
                bitMap.Save(outputFilePath, DrawingImageFormat.Icon);
            }
        }

        private void My_B_Gray_Click(object sender, RoutedEventArgs e)
        {
            if (filesPath_T.Length > 0)
            {
                if (!SelectOutputFolder(out outputFolder))
                {
                    return;
                }

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
                    SaveImageWithStructure(file, bitmap, "Grayscale Conversion");
                    bitmap.Dispose();
                }
                MessageBox.Show($"Grayscale conversion complete! Output folder: {outputFolder}");
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
                if (!SelectOutputFolder(out outputFolder))
                {
                    return;
                }

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
                    SaveImageWithStructure(file, bitmap, "Batch Blur");
                    bitmap.Dispose();
                }
                MessageBox.Show($"Blur conversion complete! Output folder: {outputFolder}");
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
                if (!SelectOutputFolder(out outputFolder))
                {
                    return;
                }

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
                    SaveImageWithStructure(file, bitmap, "Batch Invert");
                    bitmap.Dispose();
                }
                MessageBox.Show($"Invert conversion complete! Output folder: {outputFolder}");
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
