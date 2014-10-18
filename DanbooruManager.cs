using ReverseProxy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReverseProxy
{
    public static class DanbooruManager
    {
        public static bool CanRequest()
        {
            using (var db = new EFContext())
            {
                var time = DateTime.Now.AddHours(-1);
                db.Requests.RemoveRange(db.Requests.Where(r => r.RequestTime < time));
                db.SaveChanges();
                return int.Parse(System.Configuration.ConfigurationManager.AppSettings["DanbooruRPS"]) > (db.Requests.Count() + 1);
            }
        }

        public static bool MarkRequest()
        {
            using (var db = new EFContext())
            {
                var time =DateTime.Now.AddHours(-1);
                db.Requests.RemoveRange(db.Requests.Where(r => r.RequestTime < time));
                db.SaveChanges();

                if (int.Parse(System.Configuration.ConfigurationManager.AppSettings["DanbooruRPS"]) > (db.Requests.Count() + 1))
                {

                    db.Requests.Add(new Request() { RequestTime = DateTime.Now });
                    db.SaveChanges();
                    return true;
                }
                else
                    return false;
            }
        }

    }
}