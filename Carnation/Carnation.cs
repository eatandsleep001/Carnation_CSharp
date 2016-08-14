using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CarnationNamespace
{
    class Carnation
    {
        private Uri uri = null;
        private int success = 0;
        private int countView = 0;
        private int currentIndex = 0;
        private Mutex mutex = null;
        List<WebProxy> listProxy = null;

        public static string ReadAllTextInFile(string Filename)
        {
            string result = null;
            FileStream fileStream = new FileStream(Filename, FileMode.Open, FileAccess.Read);

            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                result = streamReader.ReadToEnd();

            return result;
        }

        public static string RandomString(string Characters, int Length)
        {
            Random random = new Random();
            return new string(Enumerable.Repeat(Characters, Length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public Carnation(string Url)
        {
            if (!Uri.TryCreate(Url, UriKind.Absolute, out this.uri))
            {
                Console.WriteLine("Cannot create Uri");
            }

            this.success = 0;
            this.countView = 0;
            this.currentIndex = 0;

            this.mutex = new Mutex();

            this.listProxy = new List<WebProxy>();
        }

        private void CreateListProxy()
        {
            string data = Carnation.ReadAllTextInFile("proxies.txt");
            string temp;
            string proxy;
            int port;

            MatchCollection matchCollection = Regex.Matches(data, @"\d{1,3}(\.\d{1,3}){3}:\d{1,5}");

            if (matchCollection.Count > 0)
            {
                foreach (Match match in matchCollection)
                {
                    temp = match.Value;
                    proxy = temp.Split(':')[0];
                    int.TryParse(temp.Split(':')[1], out port);

                    if (port <= 65535 && port > 0)
                    {
                        this.listProxy.Add(new WebProxy(proxy, port));
                    }
                }
            }
        }

        private HttpStatusCode Get(WebProxy webProxy, int timeout)
        {
            HttpStatusCode result = HttpStatusCode.BadRequest;
            HttpWebRequest httpWebRequest = null;
            HttpWebResponse httpWebResponse = null;

            try
            {
                httpWebRequest = (HttpWebRequest)WebRequest.Create(this.uri);
                httpWebRequest.Accept = @"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                httpWebRequest.Proxy = webProxy;
                httpWebRequest.Timeout = timeout;
                httpWebRequest.Headers.Add(@"Accept-Language", @"vi-VN,vi;q=0.8,fr-FR;q=0.6,fr;q=0.4,en-US;q=0.2,en;q=0.2");
                httpWebRequest.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.82 Safari/537.36 ";

                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (Exception ex) { }

            if (httpWebResponse != null)
            {
                result = httpWebResponse.StatusCode;

                httpWebResponse.Close();
            }

            return result;
        }

        private void Do(int threadID, int timeout)
        {
            HttpStatusCode httpStatusCode;
            WebProxy webProxy;

            while (true)
            {
                this.mutex.WaitOne();

                webProxy = this.listProxy[this.currentIndex];

                this.currentIndex++;

                this.mutex.ReleaseMutex();

                httpStatusCode = this.Get(webProxy, timeout);

                this.mutex.WaitOne();

                this.countView++;

                if (httpStatusCode == HttpStatusCode.OK)
                {
                    this.success++;
                }

                Console.WriteLine(
                    string.Format("Thread {0,3}:", threadID).PadRight(15, ' ') +
                    string.Format("{0,3}", webProxy.Address).PadRight(30, ' ') +
                    string.Format("{0,5}|{1,0}|{2,0}", this.success, this.countView, httpStatusCode));

                if (this.countView >= this.listProxy.Count)
                {
                    this.mutex.ReleaseMutex();
                    break;
                }
                this.mutex.ReleaseMutex();
            }
        }

        public void Run()
        {
            List<Thread> threads = new List<Thread>();
            int threadCount = 1;
            int timeout = 10000;
            IniFile iniFile = new IniFile(@"Settings.ini");

            int.TryParse(iniFile.Read(@"Threads", @"Settings"), out threadCount);
            int.TryParse(iniFile.Read(@"Timeout", @"Settings"), out timeout);

            this.CreateListProxy();

            if (threadCount > this.listProxy.Count)
                threadCount = this.listProxy.Count;

            Console.Title += @" " + this.uri.AbsoluteUri;

            for (int i = 0; i < threadCount; i++)
            {
                threads.Add(new Thread(delegate () { this.Do(i, timeout); }));
                threads[i].Start();
            }
        }
    }
}