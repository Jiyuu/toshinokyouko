using ReverseProxy.Data;
using System;
using System.Linq;
using System.Threading;

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
                            return;

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


        private void updateImagesList()
        {
            logger.Trace("updateImagesList");
            int i = 0;
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
                    var res = Newtonsoft.Json.Linq.JObject.Parse(wc.DownloadString(string.Format(System.Web.HttpUtility.UrlDecode(System.Configuration.ConfigurationManager.AppSettings["DanbooruPostsURL"]), System.Configuration.ConfigurationManager.AppSettings["DabooruTags"], page)));
                    using (var db = new EFContext())
                    {
                        foreach (var result in res)
                        {
                            if (!db.Posts.Any(p => p.PostID == (int)res["id"]))
                            {
                                db.Posts.Add(new Post() { IsSaved = false, PostID = (int)res["id"], URL = res["file_url"].ToString() });
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

        public string GetImage()
        {
            return null;
        }

    }
}