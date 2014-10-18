using System;
using System.Configuration;
using System.Web;
using System.Net;
using System.Text;
using System.IO;


namespace ReverseProxy
{
    /// <summary>
    /// Handler that intercept Client's request and deliver the web site
    /// </summary>
    public class ReverseProxy : IHttpHandler
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Method calls when client request the server
        /// </summary>
        /// <param name="context">HTTP context for client</param>
        public void ProcessRequest(HttpContext context)
        {

            Manager.Instance.Init();

            logger.Trace(context.Request.Url);
            //read values from configuration file
            int proxyMode = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["ProxyMode"]);
            string remoteWebSite = System.Configuration.ConfigurationManager.AppSettings["RemoteWebSite"];

            string remoteUrl;
            if (proxyMode == 0)
                remoteUrl = ParseURL(context.Request.Url.AbsoluteUri); //all site accepted
            else
                remoteUrl = context.Request.Url.AbsoluteUri.Replace(context.Request.Url.Host + (context.Request.Url.Port != 80 ? ":" + context.Request.Url.Port : ""), remoteWebSite).Replace("/ToshinoKyouko-search.php", "/tokyotosho-search.php"); //only one site accepted

            if (!context.Request.Url.PathAndQuery.StartsWith("/TKimages"))
            {
                if (context.Request.Url.PathAndQuery.StartsWith("/iyarashii/"))
                { 
                    if(!string.IsNullOrWhiteSpace(context.Request.QueryString["i"]))
                    {
                        string url = HttpUtility.UrlDecode(context.Request.QueryString["i"]);
                        Manager.Instance.DisablePicture(url);
                    }
                }

                //create the web request to get the remote stream
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(remoteUrl);
                if (context.Request.HttpMethod == "POST")
                {
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = context.Request.Form.ToString().Length;

                    using (StreamWriter requestWriter2 = new StreamWriter(request.GetRequestStream()))
                    {
                        requestWriter2.Write(context.Request.Form.ToString());
                    }

                }
                if (context.Request.Cookies.Count > 0)
                    request.CookieContainer = new CookieContainer();
                foreach (var item in context.Request.Cookies.AllKeys)
                {
                    request.CookieContainer.Add(new Cookie(item, context.Request.Cookies[item].Value, "/", remoteWebSite));
                }
                //TODO : you can add your own credentials system 
                //request.Credentials = CredentialCache.DefaultCredentials;

                HttpWebResponse response;
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch
                {
                    //remote url not found, send 404 to client 
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Write("<h2>Page not found</h2>");
                    context.Response.End();
                    return;
                }
                if (!string.IsNullOrEmpty(response.Headers["Set-Cookie"]))
                {
                    foreach (var item in (response.Headers["Set-Cookie"] + ",").Split(new string[] { "info," }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        context.Response.AppendHeader("Set-Cookie", replacedomain(item + "info", ".tokyotosho.info", context));

                    }
                }
                Stream receiveStream = response.GetResponseStream();

                if ((response.ContentType.ToLower().IndexOf("html") >= 0) || (response.ContentType.ToLower().IndexOf("javascript") >= 0))
                {
                    //this response is HTML Content, so we must parse it
                    StreamReader readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                    Uri test = new Uri(remoteUrl);
                    string content;
                    if (proxyMode == 0)
                        content = ParseHtmlResponse(readStream.ReadToEnd(), context.Request.ApplicationPath + "/http//" + test.Host);
                    else
                        content = ParseHtmlResponse(readStream.ReadToEnd(), context.Request.ApplicationPath);
                    //write the updated HTML to the client


                    context.Response.Write(content);
                    //close streams
                    readStream.Close();
                    response.Close();
                    context.Response.End();
                }
                else
                {
                    //the response is not HTML Content
                    byte[] buff = new byte[1024];
                    int bytes = 0;
                    while ((bytes = receiveStream.Read(buff, 0, 1024)) > 0)
                    {
                        //Write the stream directly to the client 
                        context.Response.OutputStream.Write(buff, 0, bytes);
                    }
                    //close streams
                    response.Close();
                    context.Response.End();
                }
            }
        }

        /// <summary>
        /// Get the remote URL to call
        /// </summary>
        /// <param name="url">URL get by client</param>
        /// <returns>Remote URL to return to the client</returns>
        public string ParseURL(string url)
        {
            if (url.IndexOf("http/") >= 0)
            {
                string externalUrl = url.Substring(url.IndexOf("http/"));
                return externalUrl.Replace("http/", "http://");
            }
            else
                return url;
        }

        /// <summary>
        /// Parse HTML response for update links and images sources
        /// </summary>
        /// <param name="html">HTML response</param>
        /// <param name="appPath">Path of application for replacement</param>
        /// <returns>HTML updated</returns>
        public string ParseHtmlResponse(string html, string appPath)
        {
            //html=html.Replace("\"/","\""+appPath+"/");
            //html=html.Replace("'/","'"+appPath+"/");
            //html=html.Replace("=/","="+appPath+"/");


            return replaceOtherDomain(html);
        }
        public string replaceOtherDomain(string html)
        {
            string img = getImg();
            html = html.Replace("<div id=\"main\">", "<div id=\"main\" style=\"background-image:url('" + img + "');background-repeat:no-repeat;background-size: auto 350px; \"><a href=\"javascript:var myRequest = new XMLHttpRequest();myRequest.open('GET','/iyarashii/?i=" + HttpUtility.UrlEncode(img) + "',true);myRequest.send();void(0)\" style=\"position:absolute;top:400px;left:40px;\" >iyarashii?</a>");
            html = html.Replace("Tokyo Toshokan", "Toshino Kyouko");
            html = html.Replace("<h1>Tokyo <span title=\"Japanese: Libary\">Toshokan</span></h1>", "<h1>Toshino Kyouko</h1>");
            html = html.Replace("<div class=\"centertext\">東京 図書館</div>", "<div class=\"centertext\">歳納 京子</div>");
            html = html.Replace("tokyotosho.info", "ToshinoKyouko.net");
            html = html.Replace("tokyotosho.se", "ToshinoKyouko.com");
            html = html.Replace("tokyo-tosho.net", "ToshinoKyoko.net");
            html = html.Replace("tokyotosho", "ToshinoKyouko");
            return html;
        }
        public string replacedomain(string text, string domain, HttpContext context)
        {
            return text.Replace(domain, context.Request.Url.Host);

        }


        static string[] imgs = new string[] { "http://animeholic.net/i/a/src/1350064805931.jpg", "http://animeholic.net/i/a/src/1350064668683.jpg", "http://animeholic.net/i/a/src/1349616956965.png", "http://animeholic.net/i/a/src/1349617287333.jpg", "http://animeholic.net/i/a/src/1349617087675.jpg", "http://animeholic.net/i/a/src/1349616876581.jpg", "http://animeholic.net/i/a/src/1349617018911.jpg", "http://animeholic.net/i/a/src/1349735207114.jpg", "http://animeholic.net/i/a/src/1347894857755.jpg" };

        public string getImg()
        {
            return Manager.Instance.GetImage();
            //Random r = new Random();
            //return imgs[r.Next(0, imgs.Length)];

        }

        /// 
        /// Specifies whether this instance is reusable by other Http requests
        /// 
        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}
