/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ImageHelper.Core.cs
    File description: ImageHelper.Core related implementation

    Created by: Senparc - 20211124

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.0.1.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Drawing;
using System.IO;
//using Jillzhang.GifUtility;

namespace Senparc.ImageUtility
{
    /// <summary>
    /// Provides bitmap thumbnail and generated-file-name helpers for the image utility package.
    /// </summary>
    public partial class ImageHelper
    {
        const string ThumbnailPrefix = "thumbnail";

        /// <summary>
        /// You must create a delegate and pass a reference to the delegate as the callback parameter, but the delegate is not used.
        /// </summary>
        /// <remarks>https://docs.microsoft.com/en-us/dotnet/api/system.drawing.image.getthumbnailimage?view=netcore-2.2</remarks>
        /// <returns>
        /// Always returns <see langword="false"/> because the callback exists only to
        /// satisfy the <see cref="Image.GetThumbnailImage"/> delegate contract.
        /// </returns>
        private static bool GetThumbnailImageCallBack()
        {
            return false;
        }

        /// <summary>
        /// Creates a thumbnail from an image stream, writes it as a PNG file in the
        /// specified output directory, and optionally deletes the original file.
        /// When <paramref name="isLocked"/> is <see langword="true"/>, the source
        /// aspect ratio is preserved and the requested width is treated as a maximum.
        /// </summary>
        /// <param name="sourceStream">The readable stream containing the source image.</param>
        /// <param name="orignalFullFileName">The source file name used to determine the extension and optional deletion target.</param>
        /// <param name="width">The requested thumbnail width in pixels.</param>
        /// <param name="height">The requested thumbnail height in pixels when aspect-ratio locking is disabled.</param>
        /// <param name="isLocked">Whether to preserve the source aspect ratio instead of forcing both dimensions.</param>
        /// <param name="outputPath">The directory in which the generated thumbnail is saved.</param>
        /// <param name="isoldfilename">Whether to derive the thumbnail name from the original file name instead of a generated identifier.</param>
        /// <param name="isDel">Whether to delete the file identified by <paramref name="orignalFullFileName"/> after saving.</param>
        /// <returns>The generated thumbnail file name, without the output directory.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceStream"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when the requested dimensions or output path are invalid for the underlying image APIs.</exception>
        /// <exception cref="System.Runtime.InteropServices.ExternalException">Thrown when the source image cannot be decoded or the thumbnail cannot be saved.</exception>
        public static string GetThumbnail(Stream sourceStream, string orignalFullFileName, int width, int height, bool isLocked, string outputPath, bool isoldfilename, bool isDel)
        {
            int outputWidth;
            int outputHeight;
            string thumbnailFileName;
            string thumbnailFileNameWithoutExtension;
            string thumbnailPath;
            using (Image image = Bitmap.FromStream(sourceStream))
            {
                if (isLocked)
                {
                    if (image.Width > width)
                    {
                        outputWidth = width;
                        outputHeight = (image.Height * width) / image.Width;
                    }
                    else
                    {
                        outputWidth = image.Width;
                        outputHeight = image.Height;
                    }
                }
                else
                {
                    outputWidth = width;
                    outputHeight = height;
                }
                using (Image thumbnail = image.GetThumbnailImage(outputWidth, outputHeight, GetThumbnailImageCallBack, IntPtr.Zero))
                {
                    if (isoldfilename)
                    {
                        thumbnailFileNameWithoutExtension = $"{System.IO.Path.GetFileNameWithoutExtension(orignalFullFileName)}_{ThumbnailPrefix}";
                    }
                    else
                    {
                        thumbnailFileNameWithoutExtension = $"{DateTime.Now.Ticks}{Guid.NewGuid().ToString("n").Substring(0, 8)}_{ThumbnailPrefix}";
                    }
                    thumbnailFileName = $"{thumbnailFileNameWithoutExtension}{System.IO.Path.GetExtension(orignalFullFileName)}";

                    thumbnailPath = System.IO.Path.Combine(outputPath, thumbnailFileName);
                    thumbnail.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            if (isDel && System.IO.File.Exists(orignalFullFileName))
            {
                System.IO.File.Delete(orignalFullFileName);
            }
            return thumbnailFileName;
        }

        /// <summary>
        /// Opens a source image file and delegates thumbnail creation to
        /// <see cref="GetThumbnail(Stream, string, int, int, bool, string, bool, bool)"/>.
        /// </summary>
        /// <param name="sourceFilePath">The full path of the source image file.</param>
        /// <param name="width">The requested thumbnail width in pixels.</param>
        /// <param name="height">The requested thumbnail height in pixels when aspect-ratio locking is disabled.</param>
        /// <param name="isLocked">Whether to preserve the source aspect ratio.</param>
        /// <param name="outputPath">The directory in which the generated thumbnail is saved.</param>
        /// <param name="isoldfilename">Whether to derive the thumbnail name from the original file name.</param>
        /// <param name="isDel">Whether to delete the source file after saving.</param>
        /// <returns>The generated thumbnail file name, without the output directory.</returns>
        /// <exception cref="FileNotFoundException">Thrown when <paramref name="sourceFilePath"/> does not identify an existing file.</exception>
        /// <exception cref="IOException">Thrown when the source file cannot be opened or the generated file cannot be written.</exception>
        public static string GetThumbnail(string sourceFilePath, int width, int height, bool isLocked, string outputPath, bool isoldfilename, bool isDel)
        {
            using (FileStream fs = new FileStream(sourceFilePath, FileMode.Open))
            {
                return GetThumbnail(fs, sourceFilePath, width, height, isLocked, outputPath, isoldfilename, isDel);
            }
        }

        //public static string GetThumbnail(IFormFile formFile, int width, int height, bool isLocked, string outputPath, bool isoldfilename, bool isDel)
        //{
        //    using (Stream stream = formFile.OpenReadStream())
        //    {
        //        return GetThumbnail(stream, formFile.FileName, width, height, isLocked, outputPath, isoldfilename, isDel);
        //    }
        //}
        //public static bool GetThumbnail(string sourceFilePath, int width, int height, bool isLocked, string outputPath, bool isoldfilename, bool isDel)
        //{
        //    string outputFilePath;
        //    if (!File.Exists(sourceFilePath))
        //    {
        //        throw new IOException(string.Format("File {0} does not exist!", sourceFilePath));
        //    }
        //    //if it is a folder path
        //    string Path = HttpContext.Current.Server.MapPath(outputPath);
        //    Path = System.IO.Path.GetDirectoryName(Path);
        //    string filename = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
        //    outputFilePath = System.IO.Path.Combine(Path, filename + ".jpg");


        //    int outputWidth, outputHeight;
        //    using (Image sourceImg = Image.FromFile(sourceFilePath))
        //    {
        //        if (isLocked)
        //        {
        //            //if (sourceImg.Width > sourceImg.Height)
        //            //{
        //            if (sourceImg.Width > width)
        //            {
        //                outputWidth = width;
        //                outputHeight = (sourceImg.Height * width) / sourceImg.Width;
        //            }
        //            else
        //            {
        //                outputWidth = sourceImg.Width;
        //                outputHeight = sourceImg.Height;
        //            }
        //        }
        //        else
        //        {
        //            outputWidth = width;
        //            outputHeight = height;
        //        }
        //        using (Bitmap bitmap = new Bitmap(outputWidth, outputHeight))
        //        {
        //            using (Graphics g = Graphics.FromImage(bitmap))
        //            {
        //                g.Clear(Color.FromName("white"));
        //                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        //                g.DrawImage(sourceImg, new Rectangle(0, 0, outputWidth, outputHeight), new Rectangle(0, 0, sourceImg.Width, sourceImg.Height), GraphicsUnit.Pixel);
        //                bitmap.Save(outputFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
        //            }
        //        }

        //    }
        //    if (isDel)
        //    {
        //        DeleteFile(sourceFilePath);
        //    }
        //    return true;

        //}

        ///// <summary>
        ///// Thumbnail image 
        ///// </summary>
        /////<param name="sourcePath">Source file physical path + file name</param>
        /////<param name="width">Scale to width</param>
        /////<param name="height">Height to scale to</param>
        /////<param name="locked">whether to keep the original aspect ratio</param>
        /////<param name="outputPath">Store folder + file name</param>
        /////<param name="isDel">whether to delete source file</param>
        /////<returns>file physical path</returns>
        //public static string GetThumbnail(string sourceFilePath, int width, int height, bool isLocked, string outputPath, bool isDelOriginalFile)
        //{
        //    string outputFilePath;
        //    if (!File.Exists(sourceFilePath))
        //    {
        //        throw new IOException(string.Format("File {0} does not exist!", sourceFilePath));
        //    }
        //    //if it is a folder path
        //    //string Path = HttpContext.Current.Server.MapPath(outputPath);
        //    //Path = System.IO.Path.GetDirectoryName(Path);
        //    //string filename = System.IO.Path.GetFileName(sourceFilePath);
        //    //outputFilePath = System.IO.Path.Combine(Path, filename);

        //    outputFilePath = (outputPath.StartsWith("~/")) ? HttpContext.Current.Server.MapPath(outputPath) : outputPath;//if it is the relative path of the application program, then
        //    // Convert for physical path
        //    int outputWidth, outputHeight;
        //    using (Image sourceImg = Image.FromFile(sourceFilePath))
        //    {
        //        if (isLocked)
        //        {
        //            if (sourceImg.Width > sourceImg.Height)
        //            {
        //                if (sourceImg.Width > width)
        //                {
        //                    outputWidth = width;
        //                    outputHeight = (sourceImg.Height * width) / sourceImg.Width;
        //                }
        //                else
        //                {
        //                    outputWidth = sourceImg.Width;
        //                    outputHeight = sourceImg.Height;
        //                }
        //            }
        //            else
        //            {
        //                if (sourceImg.Height > height)
        //                {
        //                    double rate = (height / (double)sourceImg.Height);
        //                    outputWidth = (int)(sourceImg.Width * rate);
        //                    outputHeight = height;
        //                }
        //                else
        //                {
        //                    outputWidth = sourceImg.Width;
        //                    outputHeight = sourceImg.Height;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            outputWidth = width;
        //            outputHeight = height;
        //        }
        //        if (sourceImg.RawFormat.Guid == ImageFormat.Gif.Guid)
        //        {
        //            double rate = 1;
        //            if (width < sourceImg.Width)
        //            {
        //                rate = ((double)width) / ((double)sourceImg.Width);
        //            }
        //            sourceImg.Dispose();
        //            GifHelper gif = new GifHelper();
        //            gif.GetThumbnail(sourceFilePath, rate, outputFilePath);
        //        }
        //        else
        //        {
        //            using (Bitmap bitmap = new Bitmap(outputWidth, outputHeight))
        //            {
        //                using (Graphics g = Graphics.FromImage(bitmap))
        //                {
        //                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        //                    g.DrawImage(sourceImg, new Rectangle(0, 0, outputWidth, outputHeight), new Rectangle(0, 0, sourceImg.Width, sourceImg.Height), GraphicsUnit.Pixel);
        //                    bitmap.Save(outputFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
        //                }
        //            }
        //        }
        //    }
        //    if (isDelOriginalFile)
        //    {
        //        DeleteFile(sourceFilePath);
        //    }
        //    return System.IO.Path.GetFileName(outputFilePath);
        //}

        /// <summary>
        /// Generates a time-based file-name token with a four-digit random suffix.
        /// This method does not create a file and is not intended to provide a security token.
        /// </summary>
        /// <returns>A string suitable for use as a generated file-name component.</returns>
        public static string GetRndFileName()
        {
            Random rnd = new Random();
            string fp = DateTime.Now.ToString("yyyyMMddhhmmss") + rnd.Next(1000, 9999).ToString();
            return fp;
        }

        //public static void UpdateFileName(string sourceFileName, string newFileName)
        //{
        //    File.Move(sourceFileName, HttpContext.Current.Server.MapPath(newFileName));
        //}

        #region Private Method

        /// <summary>
        /// Determines whether a path contains a file-name extension.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <returns>
        /// <see langword="true"/> when <see cref="Path.HasExtension(string)"/> reports
        /// an extension; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Propagated by <see cref="Path.HasExtension(string)"/> when
        /// <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        private static bool IsFilePath(string path)
        {
            if (System.IO.Path.HasExtension(path))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes a file when it exists and translates file-system failures into
        /// an <see cref="IOException"/>.
        /// </summary>
        /// <param name="filePath">The file path to delete.</param>
        /// <exception cref="IOException">
        /// Thrown when the file exists but cannot be deleted.
        /// </exception>
        private static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    throw new IOException(string.Format("删除文件出错！{0},错误代码:{1}", filePath, ex.Message));
                }
            }
        }


        #endregion
    }
}
