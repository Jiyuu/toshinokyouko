using ReverseProxy.Data;
using System;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReverseProxy
{
    public sealed class Manager
    {
        #region singleton
        private static volatile Manager instance;
        private static object syncRoot = new Object();
        System.Timers.Timer timer = new System.Timers.Timer(TimeSpan.FromDays(1).TotalMilliseconds);


        public static Manager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new Manager();
                    }
                }

                return instance;
            }
        }
        #endregion

        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public void Init()
        {
            logger.Trace("Init");
            using (var db = new EFContext())
            {
                if (db.Posts.Any())
                    return;
                else
                {
                    for (int i = 0; i < 5; i++)
                    {

                        if (saveDandooruList(0))
                        {
                            System.Threading.Tasks.Task.Factory.StartNew(() => { try { updateImagesList(1); } catch { } });
                            return;
                        }
                        Thread.Sleep(1000);
                    }
                    throw new Exception("Couldnt get any data to start working");
                }
            }
        }

        private Manager()
        {
            timer.Elapsed += timer_Elapsed;
        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            updateImagesList();
        }


        private void updateImagesList(int i = 0)
        {
            logger.Trace("updateImagesList");
            while (saveDandooruList(i))
            { i++; }
        }

        private bool saveDandooruList(int page)
        {
            logger.Trace("saveDandooruList({0})", page);
            try
            {
                if (!DanbooruManager.MarkRequest())
                    return false;

                using (var wc = new System.Net.WebClient())
                {
                    var res = Newtonsoft.Json.Linq.JContainer.Parse(wc.DownloadString(string.Format(System.Web.HttpUtility.UrlDecode(System.Configuration.ConfigurationManager.AppSettings["DanbooruPostsURL"]), System.Configuration.ConfigurationManager.AppSettings["DabooruTags"], page)));
                    using (var db = new EFContext())
                    {
                        foreach (var rec in res)
                        {
                            int id = (int)rec["id"];
                            if (!db.Posts.Any(p => p.PostID == id) && !rec["tag_string"].ToString().Split(' ').Any(s => s == "comic") && allowedExtentions.Contains(Path.GetExtension(rec["file_url"].ToString())))
                            {

                                db.Posts.Add(new Post() { IsSaved = false, PostID = (int)rec["id"], URL = rec["file_url"].ToString(), Enabled = true });
                                db.SaveChanges();
                            }
                        }

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Trace(ex.ToString());
                return false;
            }
        }
        static string[] allowedExtentions = new string[] { ".gif", ".jpg", ".jpeg", ".png" };
        public string GetImage()
        {
            logger.Info("Getting Image");
            using (var db = new EFContext())
            {
                    var record = db.Posts.Where(p => p.Enabled && p.IsSaved).OrderBy(p => Guid.NewGuid()).FirstOrDefault();
                    if (record != null)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                logger.Info("downloading new picture");
                                addImageToList();
                            }
                            catch { }
                        });
                        logger.Info("Returning Image");
                        return record.URL;
                    }
                    else
                    {
                        try
                        {
                            return addImageToList();
                        }
                        finally {
                            logger.Info("Returning Image after download");
                        }
                    }
            }
        }

        public void DisablePicture(string url)
        {
            using (var db = new EFContext())
            {
                var post = db.Posts.FirstOrDefault(p => p.URL == url);
                post.Enabled = false;
                db.SaveChanges();
            }
        }
        private string addImageToList()
        {
            using (var db = new EFContext())
            {
                var record = db.Posts.Where(p => p.Enabled && !p.IsSaved).OrderBy(p => Guid.NewGuid()).FirstOrDefault();
                if (record == null)
                {
                    record = db.Posts.Where(p => p.Enabled && p.IsSaved).OrderBy(p => Guid.NewGuid()).FirstOrDefault();
                    if (record == null)
                        throw new Exception("Cannot get any posts");
                    return record.URL;
                }

                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    string fullurl = "http://danbooru.donmai.us" + record.URL;
                    string filename = Path.GetFileName(new Uri(fullurl).AbsolutePath);
                    string ext = Path.GetExtension(new Uri(fullurl).AbsolutePath);



                    wc.DownloadFile(fullurl, System.Web.HttpRuntime.AppDomainAppPath + "/TKimages/" + filename);

                    record.URL = "/TKimages/" + filename;
                    record.IsSaved = true;
                    if (ext != ".gif")
                        NormalizeSize(300, 350, System.Web.HttpRuntime.AppDomainAppPath + "/TKimages/" + filename);

                    db.SaveChanges();
                    return record.URL;
                }
            }
        }
        public void NormalizeSize(int maxWidth, int maxHeight, string path)
        {

            var image = System.Drawing.Image.FromFile(path);
            if ((maxWidth != 0 && image.Width > maxWidth) || (maxHeight != 0 && image.Height > maxHeight))
            {
                var ratio = (double)image.Width / (double)image.Height;
                int newWidth;
                int newHeight;
                if (maxHeight != 0)
                {
                    newHeight = (int)(Math.Min(image.Height, maxHeight));
                    newWidth = (int)(newHeight * ratio);
                }
                else
                {
                    newHeight = image.Height;
                    newWidth = image.Width;
                }
                if (maxWidth != 0 && newWidth > maxWidth)
                {
                    newWidth = maxWidth;
                    newHeight = (int)((1 / ratio) * newWidth);
                }

                var newImage = new Bitmap(newWidth, newHeight);
                Graphics thumbGraph = Graphics.FromImage(newImage);

                thumbGraph.CompositingQuality = CompositingQuality.HighQuality;
                thumbGraph.SmoothingMode = SmoothingMode.HighQuality;
                //thumbGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;

                thumbGraph.DrawImage(image, 0, 0, newWidth, newHeight);
                image.Dispose();

                newImage.Save(path, newImage.RawFormat);
            }
        }



    }
}