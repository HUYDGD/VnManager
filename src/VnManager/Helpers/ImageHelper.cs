﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VnManager.MetadataProviders.Vndb;

namespace VnManager.Helpers
{
    public static class ImageHelper
    {









        /// <summary>
        /// Creates a thumbnail image from a specified image stream
        /// </summary>
        /// <param name="stream">MemoryStream of Image</param>
        /// <param name="maxPixels">Factor of original size. If 0, defaults to 80</param>
        /// <returns></returns>
        public static Image GetThumbnailImage(Stream stream, int maxPixels)
        {
            if (maxPixels < 80) maxPixels = 80;
            //const int maxPixels = 150;
            if (stream == null) return null;
            if (stream.Length < 20) return null; //memory streams for an image should be big, this should prevent streams with only a few bytes

            Image originalImg = Image.FromStream(stream);
            if (originalImg == null) return null;
            stream.Dispose();
            //get thumbnail size
            double originalWidth = originalImg.Width;
            double originalHeight = originalImg.Height;
            double factor;
            if (originalWidth > originalHeight)
            {
                factor = (double)maxPixels / originalWidth;
            }
            else
            {
                factor = (double)maxPixels / originalHeight;
            }
            Size thumbnailSize = new Size((int)(originalWidth * factor), (int)(originalHeight * factor));


            Bitmap bitmap = new Bitmap(thumbnailSize.Width, thumbnailSize.Height);
            Graphics g = Graphics.FromImage((Image)bitmap);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(originalImg, 0, 0, thumbnailSize.Width, thumbnailSize.Height);

            var img = (Image)bitmap;
            bitmap.Dispose();
            return img;

        }



        public static async Task DownloadImagesWithThumbnails(List<ScreenShot> imageList, string imageDirectory)
        {
            try
            {
                if (imageList == null || !imageList.Any()) return;
                using (var client = new WebClient())
                {
                    foreach (var screen in imageList)
                    {
                        if (screen.Url == null) continue;
                        var imageDir = $@"{imageDirectory}\{Path.GetFileName(screen.Url)}";
                        var thumbDir = $@"{imageDirectory}\thumbs\{Path.GetFileName(screen.Url)}";

                        var imageStream = new MemoryStream(await client.DownloadDataTaskAsync(new Uri(screen.Url)));
                        var thumbImg = GetThumbnailImage(imageStream,0);
                        if (thumbImg == null) continue;
                        if (screen.IsNsfw && App.UserSettings.IsVisibleSavedNsfwContent == false)
                        {
                            Secure.FileEncryptStream(imageStream, imageDir, "FileEnc");

                            var thumbStream = new MemoryStream();
                            thumbImg.Save(thumbStream, ImageFormat.Jpeg);
                            Secure.FileEncryptStream(thumbStream, thumbDir, "FileEnc");
                            await thumbStream.DisposeAsync();
                        }
                        else
                        {
                            Image.FromStream(imageStream).Save(imageDir);
                            thumbImg.Save(thumbDir);
                        }
                        await imageStream.DisposeAsync();
                    }
                }
                
            }
            catch (Exception ex)
            {
                App.Logger.Warning(ex, "Failed to download images");
            }
            
        }




        public static async Task DownloadImage(string url, bool isNsfw, string path)
        {
            try
            {
                if (string.IsNullOrEmpty(url)) return;
                using (var client = new WebClient())
                {
                    if (isNsfw && App.UserSettings.IsVisibleSavedNsfwContent == false)
                    {

                        byte[] imageBytes = await client.DownloadDataTaskAsync(new Uri(url));
                        var memStream = new MemoryStream(imageBytes);
                        if (memStream.Length < 1) return;
                        Secure.FileEncryptStream(memStream, path, "FileEnc");
                        await memStream.DisposeAsync();
                    }
                    else
                    {
                        await client.DownloadFileTaskAsync(new Uri(url), path);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.Warning(ex,"Failed to download image");
            }
        }



        
    }
    public struct ScreenShot : IEquatable<ScreenShot>
    {
        public string Url;
        public bool IsNsfw;

        public bool Equals([AllowNull] ScreenShot other)
        {
            return Url == other.Url && IsNsfw == other.IsNsfw;
        }

    }
}
