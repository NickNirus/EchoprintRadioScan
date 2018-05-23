// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EchonestAPI.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the EchonestResult type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Security.Policy;
using System.Web;
using Newtonsoft.Json;
using SharedType;

namespace EchoprintManager
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using SharedType;
    using EchoprintManager.Data;

    using Newtonsoft.Json.Linq;

    public enum EchonestResult
    {
        None,
        Error,
        NotFound,
        Success,
        Skip
    }

    /// <summary>
    /// The echonest api.
    /// </summary>
    public class EchonestAPI
    {
        public static ManualResetEvent allDone = new ManualResetEvent(false);


        private const int BUFFER_SIZE = 1024;

        // private const int DefaultTimeout = 15 * 1000; // 15 sec timeout 
        private const int DefaultTimeout = 60 * 1000;


        // Abort the request if the timer fires. 
        private static void TimeoutCallback(object state, bool timedOut)
        {
            if (timedOut)
            {
                var request = state as HttpWebRequest;
                if (request != null)
                {
                    request.Abort();
                }
            }
        }

        // private string NO_RESULTS = "NO_RESULTS_HISTOGRAM_DECREASED";

        public static Task<SharedType.Code> GetFingerprintAsync(MemoryStream stream, string filename, int length = 0)
        {
            return Task.Run(() =>
            {
                var fn = Path.Combine(Environment.CurrentDirectory, filename);
                using (var fileStream = new FileStream(fn, FileMode.Create))
                {
                    stream.Position = 0;
                    stream.CopyTo(fileStream);


                    var taskCompletionSourcet = new TaskCompletionSource<SharedType.Code>();
                    using (var process = new System.Diagnostics.Process())
                    {
                        var workdir = Path.Combine(Environment.CurrentDirectory, "codegen");
                        process.StartInfo.WorkingDirectory = workdir;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.FileName = Path.Combine(workdir, "codegen.exe");
                        //process.StartInfo.Arguments = "\"" + path + "\"" + " -s 60";
                        process.StartInfo.Arguments = "\"" + fn + "\""; // + " -s " + length;


                        process.Start();

                        using (Task processWaiter = Task.Factory.StartNew(() => process.WaitForExit()))
                        using (Task<string> outputReader = Task.Factory.StartNew(() => process.StandardOutput.ReadToEnd()))
                        using (Task<string> errorReader = Task.Factory.StartNew(() => process.StandardError.ReadToEnd()))
                        {
                            Task.WaitAll(processWaiter, outputReader, errorReader);
                            taskCompletionSourcet.TrySetResult(JsonConvert.DeserializeObject<List<SharedType.Code>>(outputReader.Result)[0]);
                        }
                    }

                    return taskCompletionSourcet.Task;
                }
            });
        }

        public static HttpResult IdentifySongAsync(Code item, string tburl)
        {
            //var url = string.Format("http://{1}/query?code={0}&version=4.12", item.code, tburl);
            var url = string.Format("http://{1}/query?fp_code={0}", item.code, tburl);

            var myRequestState = new RequestState();

            try
            {
                var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.ContentType = "application/x-www-form-urlencoded";
                myHttpWebRequest.Method = "GET";

                myRequestState.request = myHttpWebRequest;

                var result = myHttpWebRequest.GetResponse();
                return new HttpResult
                {
                    IsSucceeded = true,
                    Data = new StreamReader(result.GetResponseStream(), Encoding.UTF8).ReadToEnd()
                };

            }
            catch (WebException e)
            {
                return new HttpResult
                {
                    IsSucceeded = false,
                    Error = e.Message
                };
            }
            catch (Exception e)
            {
                return new HttpResult
                {
                    IsSucceeded = false,
                    Error = e.Message
                };
            }

            //return null;
        }

        public static void IdentifySong(Code item, string tburl, Action<HttpResult> resultCallback)
        {
            if(item == null || item.code == null)
            {
                return;
            }
            var url = string.Format("http://{1}/query?fp_code={0}", item.code, tburl);
            var myRequestState = new RequestState();

            try
            {
                var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.ContentType = "application/x-www-form-urlencoded";
                myHttpWebRequest.Method = "GET";

                myRequestState.request = myHttpWebRequest;
                myRequestState.resultCallback = resultCallback;

                var result = myHttpWebRequest.BeginGetResponse(RespCallback, myRequestState);

                // this line implements the timeout, if there is a timeout, the callback fires and the request becomes aborted
                ThreadPool.RegisterWaitForSingleObject(
                    result.AsyncWaitHandle,
                    TimeoutCallback,
                    myHttpWebRequest,
                    DefaultTimeout,
                    true);

                // The response came in the allowed time. The work processing will happen in the  
                // callback function.
                allDone.WaitOne();
            }
            catch (WebException e)
            {
                Console.WriteLine("\nMain Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
                Console.WriteLine("Press any key to continue..........");

                SafeClose(myRequestState);
                SafeInvokeCallback(myRequestState.resultCallback, false, null);
            }
            catch (Exception e)
            {
                Console.WriteLine("\nMain Exception raised!");
                Console.WriteLine("Source :{0} ", e.Source);
                Console.WriteLine("Message :{0} ", e.Message);
                Console.WriteLine("Press any key to continue..........");
                Console.Read();

                SafeClose(myRequestState);
                SafeInvokeCallback(myRequestState.resultCallback, false, null);
            }

        }

        //public static bool ExistArtist(string json)
        //{
        //    if (string.IsNullOrWhiteSpace(json)) return false;
        //    return !string.IsNullOrWhiteSpace(JObject.Parse(json)["match"]["track_id"].Value<string>().Trim());
        //}

        //private HttpWebRequest request;
        public void InsertSongToDataBase(Code item, string tburl, Action<string> callback)
        {
            //check add meesing params
            if (string.IsNullOrWhiteSpace(item.metadata.artist))
            {
                item.metadata.artist = "Unknown";
            }

            if (string.IsNullOrWhiteSpace(item.metadata.title))
            {
                item.metadata.title = "Unknown";
            }

            if (string.IsNullOrWhiteSpace(item.metadata.genre))
            {
                item.metadata.genre = "Unknown";
            }


            //var result = EchonestResult.None;
            // while (result == EchonestResult.None)
            // {
            var url = string.Format("http://{0}/ingest2", tburl);
            //var url = string.Format("http://{1}/query?code={0}&version=4.12", item.code, tburl);

            var query = JObject.FromObject(new
            {
                json = item
            });

            var buffer = Encoding.UTF8.GetBytes(query.ToString());
            //url = Uri.EscapeDataString(url);

            var request = WebRequest.Create(url) as HttpWebRequest;
            request.ServicePoint.ConnectionLeaseTimeout = 50000;
            request.ServicePoint.MaxIdleTime = 50000;

            request.ContentLength = buffer.Length;
            request.ContentType = "application/json";
            request.Method = "POST";

            try
            {
                var output = string.Empty;
                using (var input = request.GetRequestStream())
                {
                    input.Write(buffer, 0, buffer.Length);
                    using (var response = request.GetResponse())
                    {
                        using (var outstream = response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(outstream))
                            {
                                output = reader.ReadToEnd();

                                var info = JObject.Parse(output);

                                if (info["success"] != null && info["success"].Value<bool>())
                                {
                                    item.metadata.Id = Convert.ToUInt32(info["track_id"].Value<string>().Trim());
                                    item.metadata.artist_id = info["artist_id"].Value<string>().Trim();
                                    item.metadata.title = HttpUtility.UrlDecode(info["track"].Value<string>().Trim());
                                    item.metadata.TrackId = info["track_id"].Value<uint>();
                                    item.metadata.artist = HttpUtility.UrlDecode(info["artist"].Value<string>().Trim());

                                    //result = EchonestResult.Success;
                                }
                                //  else
                                //  result = EchonestResult.NotFound;

                                callback(output);
                            }
                        }
                    }
                }
            }

            catch (WebException ex)
            {
                if (ex.Response != null && ex.Response.Headers["X-RateLimit-Remaining"] == "0")
                {
                    // slow things down and retry
                    Thread.Sleep(10000);
                }
                else
                {
                    //  result = EchonestResult.Error;
                    throw new WebException(ex.Message);
                }
            }
            catch (Exception ex)
            {
                //  result = EchonestResult.Error;
                throw new WebException(ex.Message);
            }
            // }

            //  return result;
        }

        public void InsertSongToDataBaseLinux(Code item, string url, string query, Action<string> callback)
        {
            var buffer = Encoding.UTF8.GetBytes(query);
            //url = Uri.EscapeDataString(url);




            var request = WebRequest.Create(url) as HttpWebRequest;
            request.ServicePoint.ConnectionLeaseTimeout = 50000;
            request.ServicePoint.MaxIdleTime = 50000;
            request.Credentials = new NetworkCredential("andrei", "werwer1!");
            request.ContentLength = buffer.Length;
            request.ContentType = "application/json";
            request.Method = "POST";

            try
            {
                var output = string.Empty;
                using (var input = request.GetRequestStream())
                {
                    input.Write(buffer, 0, buffer.Length);
                    using (var response = request.GetResponse())
                    {
                        using (var outstream = response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(outstream))
                            {
                                output = reader.ReadToEnd();

                                var info = JObject.Parse(output);

                                if (info["success"] != null && info["success"].Value<bool>())
                                {
                                    item.metadata.Id = Convert.ToUInt32(info["track_id"].Value<string>().Trim());
                                    item.metadata.artist_id = info["artist_id"].Value<string>().Trim();
                                    item.metadata.title = HttpUtility.UrlDecode(info["track"].Value<string>().Trim());
                                    item.metadata.TrackId = info["track_id"].Value<uint>();
                                    item.metadata.artist = HttpUtility.UrlDecode(info["artist"].Value<string>().Trim());

                                    //result = EchonestResult.Success;
                                }
                                //  else
                                //  result = EchonestResult.NotFound;

                                callback(output);
                            }
                        }
                    }
                }
            }

            catch (WebException ex)
            {
                if (ex.Response != null && ex.Response.Headers["X-RateLimit-Remaining"] == "0")
                {
                    // slow things down and retry
                    Thread.Sleep(10000);
                }
                else
                {
                    //  result = EchonestResult.Error;
                    throw new WebException(ex.Message);
                }
            }
            catch (Exception ex)
            {
                //  result = EchonestResult.Error;
                throw new WebException(ex.Message);
            }
            // }

            //  return result;
        }


        public async Task<string> InsertSongToDataBaseAsync(Code item, string tburl, Action<string> message)
        {
            //check add meesing params
            if (string.IsNullOrWhiteSpace(item.metadata.artist))
            {
                item.metadata.artist = "Unknown";
            }

            if (string.IsNullOrWhiteSpace(item.metadata.title))
            {
                item.metadata.title = "Unknown";
            }

            if (string.IsNullOrWhiteSpace(item.metadata.genre))
            {
                item.metadata.genre = "Unknown";
            }

            var query = string.Format("fp_code={0}&length={1}&codever={2}&artist={3}&release={4}&track={5}&source={6}&genre={7}&bitrate={8}&sample_rate={9}",
                            item.code,
                            item.metadata.duration,
                            item.metadata.version,
                             item.metadata.artist,
                            item.metadata.release,
                            item.metadata.title,
                            item.metadata.filename,
                            item.metadata.genre,
                            item.metadata.bitrate,
                            item.metadata.sample_rate);

            var url = string.Format("http://{0}/ingest?{1}", tburl, query);

            try
            {
                var client = new JsonPost();
                var output = client.postData(query.ToString(), url, message);

                if (string.IsNullOrWhiteSpace(output))
                    return "";

              //  var info = JObject.Parse(output);

                message(output);

                //if (info["success"] != null && info["success"].Value<bool>())
                //{
                //   // item.metadata.Id = Convert.ToUInt32(info["track_id"].Value<string>().Trim());
                //   // item.metadata.artist_id = info["artist_id"].Value<string>().Trim();
                //   // item.metadata.title = HttpUtility.UrlDecode(info["track"].Value<string>().Trim());
                //   // item.metadata.TrackId = info["track_id"].Value<uint>();
                //   // item.metadata.artist = HttpUtility.UrlDecode(info["artist"].Value<string>().Trim());
                //}
                //else
                //{
                //    message(output);
                //}

                return "";
            }

            catch (WebException ex)
            {
                if (ex.Response != null && ex.Response.Headers["X-RateLimit-Remaining"] == "0")
                {
                    // slow things down and retry
                    Thread.Sleep(1000);
                }
                else
                {
                    Thread.Sleep(3000);
                    return ex.StackTrace;
                    //throw new WebException(ex.Message);
                }
            }
            catch (Exception ex)
            {
                //  result = EchonestResult.Error;
                throw new WebException(ex.StackTrace);
            }

            return null;
        }

        private static void RespCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                // State of request is asynchronous.
                var myRequestState = (RequestState)asynchronousResult.AsyncState;
                var myHttpWebRequest = myRequestState.request;
                myRequestState.response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(asynchronousResult);

                // Read the response into a Stream object.
                var responseStream = myRequestState.response.GetResponseStream();
                myRequestState.streamResponse = responseStream;

                // Begin the Reading of the contents of the HTML page and print it to the console.
                var asynchronousInputRead = responseStream.BeginRead(
                    myRequestState.BufferRead,
                    0,
                    BUFFER_SIZE,
                   ReadCallBack,
                    myRequestState);
                return;
            }
            catch (WebException e)
            {
                Console.WriteLine("\nRespCallback Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
            }

            // allDone.Set();
        }

        public void InsertSongToDataBaseChecking(Code item, string tburl, Action<string> callback)
        {
            IdentifySong(item, tburl, (result) =>
            {
                var data = RetrieveInfo(new Code(), result.Data);
                if (data.error == "NO_RESULTS_HISTOGRAM_DECREASED")
                {
                    InsertSongToDataBase(item, tburl, callback);
                    return;
                }

                callback("");

            });
        }

        public async Task<string> AsyncInsertSongToDataBaseChecking(Code item, string tburl, string file, Action<string> message, bool skipCheck = false)
        {
            if (skipCheck)
            {
                return await InsertSongToDataBaseAsync(item, tburl, null);
            }

            var result = IdentifySongAsync(item, tburl);

            if (result.Error != null)
            {
                Console.WriteLine(result.Error);
                return result.Error;
            }

            var data = RetrieveInfo(new Code(), result.Data);
            if (data.error != null && (data.error == "NO_RESULTS_HISTOGRAM_DECREASED" || data.error.Contains("no results found")))
            {
                if (!string.IsNullOrWhiteSpace(file))
                {
                    var data_full = NCodegen.StartAsync_Retrieve_Full(file).Result;
                    return await InsertSongToDataBaseAsync(data_full, tburl, message);
                }
                else {
                    return await InsertSongToDataBaseAsync(item, tburl, message);
                }
              
            }
            else {
                message("Track Id: " + data.metadata.Track_Id + "; Artist: " + data.metadata.artist + "; Track: " + data.metadata.title);
            }
            return null;
        }


        public static Code RetrieveInfo(Code code, string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return null;

            if (data.Length > 1)
            {

                var j = JObject.Parse(data);

                if (j["match"].Type == JTokenType.Boolean)
                {
                    if (j["match"].Value<bool>())
                    {
                        var song = j["track_info"];

                        var trackId = song["track_id"].Value<string>().Trim();
                        //  var artist_id = song["artist_id"].Value<string>().Trim();
                        var title = song["track"] != null ? song["track"].Value<string>().Trim() : "";
                        var artist = song["artist"] !=null ?  song["artist"].Value<string>().Trim() : "";
                        var duration = song["length"].Value<int>();

                        // var coincidences = GetCoincidences(song);

                        code.metadata.Track_Id = trackId;
                        //       code.metadata.artist_id = artist_id;
                        code.metadata.title = title;
                        code.metadata.artist = artist;
                        code.metadata.duration = duration;
                        //   code.metadata.Coincidences = coincidences;
                    }
                    else
                    {
                        code.error = j["message"].Value<string>();
                        Console.WriteLine("Info: " + string.Format("Response => {0}", code.error));
                        return code;
                    }

                }

                //if (j["match"].Value<JObject>() != null)
                //{
                //    var song = j["match"];

                //    var trackId = song["track_id"].Value<string>().Trim();
                //    var artist_id = song["artist_id"].Value<string>().Trim();
                //    var title = song["track"].Value<string>().Trim();
                //    var artist = song["artist"].Value<string>().Trim();
                //    var duration = song["length"].Value<int>();

                //    var coincidences = GetCoincidences(song);

                //    code.metadata.TrackId = Convert.ToUInt32(trackId);
                //    code.metadata.artist_id = artist_id;
                //    code.metadata.title = title;
                //    code.metadata.artist = artist;
                //    code.metadata.duration = duration;
                //    code.metadata.Coincidences = coincidences;


                //    var result = string.Format(@"
                //    **************************************
                //    trackId:{0}
                //    artist_id:{1}
                //    title:{2}          
                //    artist:{3}
                //    duration:{4}
                //    coincidences:{5}
                //    ***************************************", trackId, artist_id, title, artist, duration, coincidences);
                //    Console.WriteLine("Info:" + result);


                //}
                //else if (j["status"].Value<string>() != null)
                //{
                //    var status = j["status"];
                //    code.error = j["status"].Value<string>();
                //    Console.WriteLine("Info:" + string.Format("Response=>{0}", status));
                //}
            }
            return code;
        }

        //private static double GetCoincidences(JToken song)
        //{
        //    var score = song["score"].Value<int>();
        //    var ascore = song["ascore"].Value<int>();

        //    var result = (ascore * 100) / score;

        //    Console.WriteLine("Result Accuracy result: " + result);
        //    if (result > 100)
        //        return 100;

        //    var adjusted = (result * 100) / 60;
        //    Console.WriteLine("Result Accuracy adjusted: " + adjusted);

        //    return adjusted;
        //}

        private static void ReadCallBack(IAsyncResult asyncResult)
        {
            var myRequestState = (RequestState)asyncResult.AsyncState;

            try
            {
                var responseStream = myRequestState.streamResponse;
                int read = responseStream.EndRead(asyncResult);
                // Read the HTML page and then print it to the console. 
                if (read > 0)
                {
                    myRequestState.requestData.Append(Encoding.ASCII.GetString(myRequestState.BufferRead, 0, read));
                    responseStream.BeginRead(
                        myRequestState.BufferRead,
                        0,
                        BUFFER_SIZE,
                        ReadCallBack,
                        myRequestState);
                    return;
                }
                else
                {

                    var stringContent = myRequestState.requestData.ToString();

                    SafeClose(myRequestState);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("<<<HttpRequestHelper completed http request, duration {0} ms.->>>>> Length:{1}",
                        (DateTime.Now - myRequestState.startTime).TotalMilliseconds, stringContent.Length);

                    SafeInvokeCallback(myRequestState.resultCallback, myRequestState.httpError ? false : true, stringContent);
                }
            }
            catch (WebException e)
            {
                Console.WriteLine("\nReadCallBack Exception raised!:{0}", e);
                SafeClose(myRequestState);
                SafeInvokeCallback(myRequestState.resultCallback, false, null);
            }

            allDone.Set();
        }

        private static void SafeClose(RequestState state)
        {
            try
            {
                if (state.streamResponse != null)
                {
                    state.streamResponse.Close();
                }
                if (state.response != null)
                {
                    state.response.Close();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("HttpRequestHelper.SafeClose failed.", exc);
            }
        }
        private static void SafeInvokeCallback(Action<HttpResult> action, bool p, string stringContent)
        {
            try
            {
                action(new HttpResult { IsSucceeded = p, Data = stringContent });
            }
            catch (Exception exc)
            {
                Console.WriteLine("HttpRequestHelper.SafeInvokeCallback failed.", exc);
            }
        }
    }


    public class JsonPost
    {
        public string postData(string data, string uri, Action<string> message)
        {
            var webClient = new WebClient();

            try
            {
                webClient.Headers["content-type"] = "application/json";
                byte[] reqString = Encoding.Default.GetBytes(""); // Encoding.Default.GetBytes(data);
                byte[] resByte = webClient.UploadData(uri, "post", reqString);
                string resString = Encoding.Default.GetString(resByte);

                webClient.Dispose();
                return resString;
            }
            catch (Exception e)
            {
                message?.Invoke(e.Message);
            }

            return string.Empty;
        }
    }

    public class ExtendedWebClient : WebClient
    {

        private int timeout;
        public int Timeout
        {
            get
            {
                return timeout;
            }
            set
            {
                timeout = value;
            }
        }
        public ExtendedWebClient(Uri address)
        {
            this.timeout = 600000;//In Milli seconds
            var objWebClient = GetWebRequest(address);
        }
        protected override WebRequest GetWebRequest(Uri address)
        {
            var objWebRequest = base.GetWebRequest(address);
            objWebRequest.Timeout = this.timeout;
            return objWebRequest;
        }
    }

    public class RequestState
    {
        // This class stores the State of the request. 
        const int BUFFER_SIZE = 1024;
        public StringBuilder requestData;
        public DateTime startTime;
        public byte[] BufferRead;
        public HttpWebRequest request;
        public HttpWebResponse response;
        public Stream streamResponse;
        public Action<HttpResult> resultCallback;
        public bool httpError;
        public RequestState()
        {
            BufferRead = new byte[BUFFER_SIZE];
            requestData = new StringBuilder(string.Empty);
            request = null;
            streamResponse = null;
            startTime = DateTime.Now;
        }
        //public Code Code;
    }

    public class HttpResult
    {
        public bool IsSucceeded { get; set; }

        public string Data { get; set; }

        public string Error { get; set; }
    }

}
