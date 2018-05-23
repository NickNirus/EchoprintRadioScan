using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using Newtonsoft.Json;
using EchoprintManager;
using SharedType;
using System.Configuration;

namespace RunTimeAnalyzer
{
    public partial class RunTimeAnalyzer : Form
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //private static string serverUrl = "192.168.99.184:3016";
        private static readonly string filename = ConfigurationManager.AppSettings["buffer"];
        private MemoryStream TotalBuff;
        private DateTime lastTime;

        public event EventHandler Received;
        public event EventHandler Step_Identify;

        protected virtual void OnReceived(ReceivedEventArgs e)
        {
            EventHandler handler = Received;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnStep_Identify(ReceivedStepArgs e)
        {
            EventHandler handler = Step_Identify;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public RunTimeAnalyzer()
        {
            InitializeComponent();
            TotalBuff = new MemoryStream();
        }

        private void textBox2_TextChanged(object sender, EventArgs e) { }

        private void button1_Click(object sender, EventArgs e)
        {
            log.Info("Start :" + DateTime.Now);
            this.Received += RunTimeAnalyzer_Received;
            this.Step_Identify += RunTimeAnalyzer_Identify;

            ThreadPool.QueueUserWorkItem(state => { Download("RadioTest"); });
        }

        private void RunTimeAnalyzer_Identify(object sender, EventArgs e)
        {
            var data = e as ReceivedStepArgs;
            var t1 = DateTime.Now.Ticks;
            var msg = "Unknown error";
            log.Warn(DateTime.Now.Second + " - Array length: " + data.Stream.Length + " - Step: " + data.Step);
            var result = EchonestAPI.GetFingerprintAsync(data.Stream, filename).Result;

            var meta = Identify(result);
            //Ingest(data.station, meta.track_info.release == null ? "" : meta.track_info.release, JsonConvert.SerializeObject(meta),JsonConvert.SerializeObject(result));
            if (meta.track_info.artist == null)
            {
                msg = "No match: " + meta.message;
            }
            else
            {
                msg = "Result: " + (DateTime.Now.Ticks - t1) / TimeSpan.TicksPerMillisecond + " - track: " + meta.track_info.artist + ": " + meta.track_info.track;
            }
            //  log.Warn("codgentime: " + (DateTime.Now.Ticks - t1) / TimeSpan.TicksPerMillisecond + " - length meta: " + result.code_count);
            log.Warn(msg);
            AppendText(interval.Text + " seconds -> " + msg);
        }

        private EchoResponse Identify(SharedType.Code data)
        {
            var result = EchonestAPI.IdentifySongAsync(data, apipath.Text);
            var item = JsonConvert.DeserializeObject<EchoResponse>(result.Data);
            return item;
        }

        private void RunTimeAnalyzer_Received(object sender, EventArgs e)
        {
            var Received = e as ReceivedEventArgs;

            TotalBuff.Write(Received.downBuffer, 0, Received.bytesSize);


            if (lastTime.Year == 1)
            {
                lastTime = DateTime.Now;
                return;
            }
            if ((DateTime.Now - lastTime).TotalSeconds >= Convert.ToInt32(interval.Text))
            {
                lastTime = DateTime.Now;
                //  is5 = true;
                // is7 = false; //temp
                var obj = new ReceivedStepArgs() { station = textBox1.Text, Step = 1 };
                TotalBuff.WriteTo(obj.Stream);
                TotalBuff.SetLength(0);
                this.OnStep_Identify(obj);
            }
        }

        public void Download(string station)
        {
            AppendText("Start");
            AppendText("Get codegen from broadcast...");

            using (WebClient wcDownload = new WebClient())
            {
                try
                {
                    var webRequest = (HttpWebRequest)WebRequest.Create(textBox1.Text);
                    webRequest.Headers.Add("Icy-MetaData", "1");
                    webRequest.ContentType = "text/html;charset=utf-8";
                    webRequest.Timeout = 1000 * 60;
                    webRequest.ReadWriteTimeout = 1000 * 60;

                    webRequest.Credentials = CredentialCache.DefaultCredentials;
                    var webResponse = (HttpWebResponse)webRequest.GetResponse();

                    var stream = webResponse.GetResponseStream();

                    Int64 fileSize = webResponse.ContentLength;
                    var strResponse = wcDownload.OpenRead(textBox1.Text);
                    int bytesSize = 0;
                    byte[] downBuffer = new byte[2048];
                    var title = "";
                    var currenttitle = station;
                    RadioPost lastpost = new RadioPost() { Station = station };

                    while ((bytesSize = strResponse.Read(downBuffer, 0, downBuffer.Length)) > 0)
                    {
                        var rest = ThreadPool.QueueUserWorkItem(state =>
                        {
                            //currenttitle = getFIleName(stream);
                            if (!string.IsNullOrWhiteSpace(currenttitle) && title != currenttitle)
                            {
                                title = currenttitle;
                                Console.WriteLine(DateTime.Now.ToString("HH.mm") + ":  " + "Station Test: ");
                            }
                        });

                        OnReceived(new ReceivedEventArgs() { station = "test", Title = title, bytesSize = bytesSize, downBuffer = downBuffer });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + " " + textBox1.Text);
                    AppendText(ex.Message);
                }
            }
        }

        delegate void AppendTextDelegate(string text);

        private void AppendText(string text)
        {
            if (messager.InvokeRequired)
            {
                text = text.Insert(0, DateTime.Now.ToString("h:mm:ss:   "));
                messager.Invoke(new AppendTextDelegate(this.AppendText), new object[] { text });
            }
            else
            {
                messager.AppendText(text + Environment.NewLine);
                messager.SelectionStart = messager.Text.Length;
                messager.ScrollToCaret();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //messager.SelectionStart = messager.Text.Length;
            //messager.ScrollToCaret();
            //messager.AutoScrollOffset
        }

        //private static string CreateFileName()
        //{
        //    var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        //    var stringChars = new char[10];
        //    var random = new Random();

        //    for (int i = 0; i < stringChars.Length; i++)
        //    {
        //        stringChars[i] = chars[random.Next(chars.Length)];
        //    }

        //    return new String(stringChars) + ".mp3";
        //}
    }

    public class ReceivedStepArgs : EventArgs
    {
        public ReceivedStepArgs()
        {
            this.Stream = new MemoryStream();
        }

        public int Step { get; set; }
        public string station { get; set; }
        public MemoryStream Stream { get; set; }
    }

    //public class Code
    //{
    //    public Code()
    //    {
    //        metadata = new Metadata();
    //    }
    //    public Metadata metadata { get; set; }
    //    public int code_count { get; set; }
    //    public string code { get; set; }
    //    public int tag { get; set; }
    //}

    public class Metadata
    {

        public float codegen_time { get; set; }
        public string title { get; set; }
        public float decode_time { get; set; }

        public string artist { get; set; }

        public int start_offset { get; set; }
        public string filename { get; set; }
        public int given_duration { get; set; }
        public float version { get; set; }

        public int sample_rate { get; set; }

        public int samples_decoded { get; set; }
        public int duration { get; set; }
        public string release { get; set; }
        public string genre { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string track_id { get; set; }
        public int bitrate { get; set; }
    }


    public class ReceivedEventArgs : EventArgs
    {
        public string station { get; set; }

        public string Title { get; set; }
        public byte[] downBuffer { get; set; }
        public int bytesSize { get; set; }
        public RadioPost lastpost { get; set; }
    }

    public class RadioPost
    {
        public string Station { get; set; }
        public string Title { get; set; }

    }
}
