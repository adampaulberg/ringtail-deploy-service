using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;

namespace InstallerService
{
    /// <summary>
    /// Logger task
    /// </summary>
    public class ApiLogger
    {
        public ApiLogger(string path)
        {
            this.Path = path;

            this.WriteQueue = new ConcurrentQueue<string>();
            this.WatchQueueTask = new Task(WatchQueue);
            this.WatchQueueTask.Start();

            Log("Starting logger");
        }

        public string Path { get; set; }

        Task WatchQueueTask { get; set; }
        ConcurrentQueue<string> WriteQueue { get; set; }

        public void Log(string data)
        {
            var logdata = string.Format("{0} - {1}", GetDateString(), data);
            WriteQueue.Enqueue(logdata);
        }

        public void Log(HttpRequestMessage request)
        {
            var logdata = string.Format("{0} - Beg: {1} {2}", GetDateString(), request.Method, request.RequestUri);
            WriteQueue.Enqueue(logdata);
        }

        public void Log(HttpRequestMessage request, HttpResponseMessage response)
        {
            var logdata = string.Format("{0} - End: {1} {2} - {3}", GetDateString(), request.Method, request.RequestUri, (int)response.StatusCode);
            WriteQueue.Enqueue(logdata);
        }

        string GetDateString()
        {
            return (DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        void WatchQueue()
        {
            while (true)
            {
                string logdata;
                if (WriteQueue.TryDequeue(out logdata))
                {
                    WriteLogData(logdata);
                }
                System.Threading.Thread.Sleep(100);
            }
        }

        void WriteLogData(string logdata)
        {
            try
            {
                using (var fs = new FileStream(this.Path, FileMode.Append))
                using (var sw = new StreamWriter(fs))
                {
                    sw.WriteLine(logdata);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


    }
}
