using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using EchoprintManager.Data;
using Newtonsoft.Json;

namespace EchoprintManager
{
    using System.Threading.Tasks;
    using SharedType;
    using Microsoft.Win32.SafeHandles;

    public class NCodegen : IDisposable
    {
        private Process codegen = new Process();
        private StreamWriter input = null;
        private Action<Code> callback = null;
        private long jobCount = 0;

        public NCodegen(int start, int end)
        {
            var workingDir = Path.Combine(Environment.CurrentDirectory, "codegen");

            codegen.StartInfo.WorkingDirectory = workingDir;
            codegen.StartInfo.Arguments = String.Format("-s {0} {1} ", start, end);
            codegen.StartInfo.CreateNoWindow = true;
            codegen.StartInfo.FileName = Path.Combine(workingDir, "codegen.exe");
            codegen.StartInfo.RedirectStandardError = true;
            codegen.StartInfo.RedirectStandardOutput = true;
            codegen.StartInfo.RedirectStandardInput = true;
            codegen.StartInfo.UseShellExecute = false;

            codegen.OutputDataReceived += OutputDataReceived;
            codegen.ErrorDataReceived += ErrorDataReceived;
        }

        public NCodegen(int start, int end, string workDir)
        {
            codegen.StartInfo.WorkingDirectory = workDir;
            codegen.StartInfo.Arguments = String.Format("-s {0} {1} ", start, end);
            codegen.StartInfo.CreateNoWindow = true;
            codegen.StartInfo.FileName = Path.Combine(workDir, "codegen.exe");
            codegen.StartInfo.RedirectStandardError = true;
            codegen.StartInfo.RedirectStandardOutput = true;
            codegen.StartInfo.RedirectStandardInput = true;
            codegen.StartInfo.UseShellExecute = false;

            codegen.OutputDataReceived += OutputDataReceived;
            codegen.ErrorDataReceived += ErrorDataReceived;
        }


        public NCodegen(string workDir)
        {
            codegen.StartInfo.WorkingDirectory = workDir;
            codegen.StartInfo.Arguments = String.Format("-s 0 0 ");
            codegen.StartInfo.CreateNoWindow = true;
            codegen.StartInfo.FileName = Path.Combine(workDir, "codegen.exe");
            codegen.StartInfo.RedirectStandardError = true;
            codegen.StartInfo.RedirectStandardOutput = true;
            codegen.StartInfo.RedirectStandardInput = true;
            codegen.StartInfo.UseShellExecute = false;

            codegen.OutputDataReceived += OutputDataReceived;
            codegen.ErrorDataReceived += ErrorDataReceived;
        }

        public static Task<Code> StartAsync(string path, int duration = 70)
        {
            return Task.Run(() =>
            {
                var taskCompletionSourcet = new TaskCompletionSource<Code>();
                using (var process = new Process())
                {
                    var workdir = Path.Combine(Environment.CurrentDirectory, "codegen");
                    process.StartInfo.WorkingDirectory = workdir;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.FileName = Path.Combine(workdir, "codegen.exe");
                    process.StartInfo.Arguments = "\"" + path + "\"" + " 10" + " " + duration + "";

                    process.Start();

                    using (Task processWaiter = Task.Factory.StartNew(() => process.WaitForExit()))
                    using (Task<string> outputReader = Task.Factory.StartNew(() => process.StandardOutput.ReadToEnd()))
                    using (Task<string> errorReader = Task.Factory.StartNew(() => process.StandardError.ReadToEnd()))
                    {
                        Task.WaitAll(processWaiter, outputReader, errorReader);
                        taskCompletionSourcet.TrySetResult(JsonConvert.DeserializeObject<List<Code>>(outputReader.Result)[0]);
                    }
                }
                return taskCompletionSourcet.Task;
            });
        }

        public static Task<Code> StartAsync_Retrieve_Full(string path)
        {
            return Task.Run(() =>
            {
                var taskCompletionSourcet = new TaskCompletionSource<Code>();
                using (var process = new Process())
                {
                    var workdir = Path.Combine(Environment.CurrentDirectory, "codegen");
                    process.StartInfo.WorkingDirectory = workdir;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.FileName = Path.Combine(workdir, "codegen.exe");
                    process.StartInfo.Arguments = "\"" + path + "\"" + " -s 0";

                    process.Start();

                    using (Task processWaiter = Task.Factory.StartNew(() => process.WaitForExit()))
                    using (Task<string> outputReader = Task.Factory.StartNew(() => process.StandardOutput.ReadToEnd()))
                    using (Task<string> errorReader = Task.Factory.StartNew(() => process.StandardError.ReadToEnd()))
                    {
                        Task.WaitAll(processWaiter, outputReader, errorReader);
                        taskCompletionSourcet.TrySetResult(JsonConvert.DeserializeObject<List<Code>>(outputReader.Result)[0]);
                    }
                }
                return taskCompletionSourcet.Task;
            });
        }

        //public static Task<Code> StartAsync3(string path)
        //{
        //    return Task.Run(() =>
        //    {
        //        var taskCompletionSourcet = new TaskCompletionSource<Code>();
        //        using (var process = new Process())
        //        {
        //            var workdir = Path.Combine(Environment.CurrentDirectory, "codegen");
        //            process.StartInfo.WorkingDirectory = workdir;
        //            process.StartInfo.UseShellExecute = false;
        //            process.StartInfo.CreateNoWindow = true;
        //            process.StartInfo.RedirectStandardError = true;
        //            process.StartInfo.RedirectStandardOutput = true;
        //            process.StartInfo.FileName = Path.Combine(workdir, "codegen.exe");
        //            process.StartInfo.Arguments = "\"" + path + "\"" + " -s 40";

        //            process.Start();

        //            using (Task processWaiter = Task.Factory.StartNew(() => process.WaitForExit()))
        //            using (Task<string> outputReader = Task.Factory.StartNew(() => process.StandardOutput.ReadToEnd()))
        //            using (Task<string> errorReader = Task.Factory.StartNew(() => process.StandardError.ReadToEnd()))
        //            {
        //                Task.WaitAll(processWaiter, outputReader, errorReader);
        //                taskCompletionSourcet.TrySetResult(JsonConvert.DeserializeObject<List<Code>>(outputReader.Result)[0]);
        //            }
        //        }
        //        return taskCompletionSourcet.Task;
        //    });
        //}

        public void Start(Action<Code> handleResult)
        {
            if (handleResult == null)
                throw new ArgumentNullException("handleResult");
            try
            {
                callback = handleResult;

                codegen.Start();

                codegen.BeginOutputReadLine();
                codegen.BeginErrorReadLine();

                input = codegen.StandardInput;
            }
            catch (Exception)
            {
                codegen.Dispose();
                //throw;
            }
        }

        public void AddFile(string path)
        {
            AddFile(new FileInfo(path));
        }

        string[] supportedExtensions = { ".mp3", ".m4a", ".mp4", ".aif", ".aiff", ".flac", ".au", ".wav", ".aac", ".flv", "" };

        public void AddFile(FileInfo file)
        {
            try
            {
                if (!file.Exists)
                    throw new FileNotFoundException("File not found", file.Name);
                else if (!supportedExtensions.Any(ext => ext == file.Extension))
                    return;

                Interlocked.Increment(ref jobCount);

                if (input != null)
                {
                    input.WriteLine(file.FullName);
                }
            }
            catch
            {
                SignalJobCompleted();
            }
        }

        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (String.IsNullOrEmpty(e.Data))
                    return;

                throw new IOException(String.Format("A non-fatal exception occurred during process execution: \"{0}\"", e.Data));
            }
            finally
            {
                SignalJobCompleted();
            }
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (e.Data == null || "[]".IndexOf(e.Data) != -1)
                    return;

                Code data = null;
                if (e.Data.EndsWith(","))
                    data = JsonConvert.DeserializeObject<Code>(e.Data.Substring(0, e.Data.Length - 1));
                else
                    data = JsonConvert.DeserializeObject<Code>(e.Data);

                if (data != null)
                {
                    callback.BeginInvoke(data, (result) =>
                    {
                        callback.EndInvoke(result);
                        SignalJobCompleted();
                    }, null);
                }
            }
            catch
            {
                SignalJobCompleted();
            }
        }

        ManualResetEventSlim allCompleted = new ManualResetEventSlim(false);
        private void SignalJobCompleted()
        {
            var remaining = Interlocked.Decrement(ref jobCount);

            if (disposed && remaining <= 0)
                allCompleted.Set();
        }

        public void WaitForAll()
        {

        }

        bool disposed = false;
        public void Dispose()
        {
            if (disposed)
                throw new ObjectDisposedException("NCodegen");

            disposed = true;
            input.Close();
            codegen.WaitForExit();
            codegen.Dispose();

            allCompleted.Wait();
        }
    }
}
