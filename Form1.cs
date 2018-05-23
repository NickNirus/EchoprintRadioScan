// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Form1.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the Form1 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EchoprintManager;


namespace PopClient
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;

    //using EchoprintManager;
    //using EchoprintManager.Data;

    using Newtonsoft.Json;
    using System.Text;
    using NAudio.Wave;
    using NAudio.Lame;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using SharedType;

    public partial class Form1 : Form
    {
        public static string serverUrl;

        public Form1()
        {
            InitializeComponent();

            //textBox1.Text = ConfigurationManager.AppSettings["workingDir"];
            textBox2.Text = ConfigurationManager.AppSettings["samplefiles"];

            textBox4.Text = ConfigurationManager.AppSettings["starttime"];
            textBox5.Text = ConfigurationManager.AppSettings["endtime"];
            serverUrl = tburl.Text;
        }


        //private void repairCompositionMetadata()
        //{
        //    var isRepair = cbRepairMetadata.Enabled;
        //    string[] files = Directory.GetFiles(textBox2.Text ?? ConfigurationManager.AppSettings["samplefiles"], "*.mp3", SearchOption.AllDirectories);
        //}
        private string GetArtist(string fileName)
        {
            if (!fileName.Contains('-'))
                return "";
            return !string.IsNullOrWhiteSpace(fileName) ? fileName.Replace(".mp3", "").Split('-')[0].TrimEnd().TrimStart() : "Undefined";
        }
        private string GetTitle(string fileName)
        {
            if (!fileName.Contains('-'))
                return "";
            return !string.IsNullOrWhiteSpace(fileName) ? fileName.Replace(".mp3", "").Split('-')[1].TrimEnd().TrimStart() : "Undefined";
        }
        private string[] repairMp3Tags(string[] files)
        {
            Program.Message("Start repair...");
            var result = new List<string>();
            bool isStoped = false;
            try
            {
                var index = 1;
                if (progressBar4.InvokeRequired)
                {
                    progressBar4.Invoke((MethodInvoker)delegate
                    {
                        progressBar4.Maximum = files.Count();
                        progressBar4.Minimum = 1;
                        progressBar4.Step = 1;
                    });
                }
                else
                {
                    progressBar4.Maximum = files.Count();
                    progressBar4.Minimum = 1;
                    progressBar4.Step = 1;
                }
                foreach (var f in files)
                {
                    var artist = string.Empty;
                    var title = string.Empty;

                    var name = Path.GetFileName(f).Replace(".mp3", "");
                    //name = name.Substring(4);
                    var splitname = name.Split('-');

                    Program.Message(string.Format("{0:HH:mm:ss} - {1}", DateTime.Now, f));
                    doProgressImportedFields(index++);

                    if (splitname.Length > 2)
                    {
                        var testDialog = new dialog();
                        testDialog.StartPosition = FormStartPosition.CenterParent;
                        testDialog.textBox1.Text = name;
                        if (testDialog.InvokeRequired)
                        {
                            testDialog.Invoke(new MethodInvoker(delegate
                            {
                                var dialog = testDialog.ShowDialog(this);
                                if (dialog == DialogResult.Cancel)
                                {
                                    isStoped = true;
                                }

                                // Show testDialog as a modal dialog and determine if DialogResult = OK.
                                if (dialog == DialogResult.OK)
                                {
                                    name = testDialog.textBox1.Text;
                                    splitname = name.Split('-');
                                }
                                testDialog.Close();
                            }));
                        }
                        else
                        {
                            var dialog = testDialog.ShowDialog(this);
                            if (dialog == DialogResult.Cancel)
                            {
                                isStoped = true;
                            }

                            // Show testDialog as a modal dialog and determine if DialogResult = OK.
                            if (dialog == DialogResult.OK)
                            {
                                name = testDialog.textBox1.Text;
                                splitname = name.Split('-');
                            }

                            testDialog.Close();
                        }
                        // MessageBox.Show(string.Format("Contains multiple '-', Please rename file: {0}", f));
                    }
                    else if (splitname.Length == 1)
                    {
                        var testDialog = new dialog();
                        testDialog.StartPosition = FormStartPosition.CenterParent;
                        testDialog.textBox1.Text = name;

                        // Show testDialog as a modal dialog and determine if DialogResult = OK.
                        if (testDialog.ShowDialog(this) == DialogResult.OK)
                        {
                            name = testDialog.textBox1.Text;
                            splitname = name.Split('-');
                        }

                        testDialog.Close();
                    }

                    artist = splitname[0];
                    title = splitname[1];

                    if (string.IsNullOrWhiteSpace(artist))
                    {
                        MessageBox.Show(string.Format("IsNullOrWhiteSpace artist from file: {0}", f));
                        //  throw new Exception(string.Format("IsNullOrWhiteSpace artist from file: {0}", f));
                    }

                    if (string.IsNullOrWhiteSpace(title))
                    {
                        MessageBox.Show(string.Format("IsNullOrWhiteSpace title from file: {0}", f));
                        //   throw new Exception(string.Format("IsNullOrWhiteSpace title from file: {0}", f));
                    }
                    try
                    {
                        TagLib.File file = TagLib.File.Create(f);
                        file.Tag.Title = CyrilicToLatin(title);
                        file.Tag.Artists = new[] { CyrilicToLatin(artist) };
                        file.Tag.Performers = new[] { CyrilicToLatin(artist) };
                        file.Save();
                    }
                    catch (Exception ex)
                    {
                        Program.Message(ex.Message);
                        //MessageBox.Show(string.Format("save tag for: {0}-{1}", ex.Message, f));
                    }
                    result.Add(CyrilicToLatin(f));
                    var newFile = Path.Combine(Path.GetDirectoryName(f), CyrilicToLatin(name) + ".mp3");
                    if (!File.Exists(newFile))
                    {
                        File.Move(f, newFile);
                    }
                }
                if (isStoped)
                {
                    MessageBox.Show("Stoped");
                }
                else
                {
                    MessageBox.Show(string.Format("Success renamed files: {0}", files.Length));
                }

            }
            catch (Exception ex)
            {
                Program.Message(string.Format("{0:HH:mm:ss} - {1}", DateTime.Now, ex.Message));
            }
            return result.ToArray<string>();
        }

        private static int numberOfFiles = 0;
        //private static List<_Code> resultListNest = new List<_Code>();
        private static List<Code> resultListNest = new List<Code>();

        public void CallBack(object state)
        {
            string[] files = Directory.GetFiles(textBox2.Text ?? ConfigurationManager.AppSettings["samplefiles"], "*.mp3", SearchOption.AllDirectories);
            // var result = new List<_Code>();
            // var api = new EchonestAPI();
            //  repairCompositionMetadata();
            int index = 0;
            using (var codegen = new NCodegen(Convert.ToInt32(textBox4.Text), Convert.ToInt32(textBox5.Text)))
            {
                codegen.Start((data) =>
                {
                    doProgress(++index);

                    if (string.IsNullOrEmpty(data.metadata.title))
                    {
                        Program.Message(string.Format("metadata.title is Empty {0:HH:mm:ss} - {1}", DateTime.Now, data.metadata.filename));
                        data.metadata.title = GetTitle(data.metadata.filename);
                        Program.Message(string.Format("auto repair with: {0}", data.metadata.title));
                    }

                    if (string.IsNullOrEmpty(data.metadata.release))
                    {
                        Program.Message(string.Format("metadata.release is Empty  {0:HH:mm:ss} - {1}", DateTime.Now, data.metadata.filename));
                        data.metadata.release = data.metadata.filename;
                        Program.Message(string.Format("repair with: {0}", data.metadata.release));
                    }


                    if (string.IsNullOrEmpty(data.metadata.artist))
                    {
                        Program.Message(string.Format("metadata.artist is Empty {0:HH:mm:ss} - {1}", DateTime.Now, data.metadata.filename));
                        data.metadata.artist = GetArtist(data.metadata.filename);
                        Program.Message(string.Format("repair with: {0}", data.metadata.artist));
                    }

                    resultListNest.Add(data);
                    Program.SetMetaData(JsonConvert.SerializeObject(data, Formatting.Indented));
                });


                numberOfFiles = files.Length;

                if (progressBar1.InvokeRequired)
                {
                    progressBar1.Invoke((MethodInvoker)delegate
                    {
                        progressBar1.Maximum = numberOfFiles;
                        progressBar1.Minimum = 1;
                        progressBar1.Step = 1;
                    });
                }
                else
                {
                    progressBar1.Maximum = numberOfFiles;
                    progressBar1.Minimum = 1;
                    progressBar1.Step = 1;
                }


                if (progressBar4.InvokeRequired)
                {
                    progressBar4.Invoke((MethodInvoker)delegate
                    {
                        progressBar4.Maximum = numberOfFiles;
                        progressBar4.Minimum = 1;
                        progressBar4.Step = 1;
                    });
                }
                else
                {
                    progressBar4.Maximum = numberOfFiles;
                    progressBar4.Minimum = 1;
                    progressBar4.Step = 1;
                }


                for (int i = 0; i < files.Count(); i++)
                {
                    Program.Message(string.Format("{0:HH:mm:ss} - {1}", DateTime.Now, files[i]));
                    codegen.AddFile(files[i]);
                    doProgressImportedFields(i + 1);
                }
            }
        }

        private void doProgress(int index)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke((MethodInvoker)delegate
                {
                    progressBar1.Value = index;
                });
            }
            else
            {
                progressBar1.Value = index;
            }


            if (lbIndexImportNest.InvokeRequired)
            {
                lbIndexImportNest.Invoke((MethodInvoker)delegate
                {
                    lbIndexImportNest.Text = index.ToString();
                });
            }
            else
            {
                lbIndexImportNest.Text = index.ToString();
            }
        }


        private void doProgressImportedFields(int index)
        {
            if (progressBar4.InvokeRequired)
            {
                progressBar4.Invoke((MethodInvoker)delegate
                {
                    progressBar4.Value = index;
                });
            }
            else
            {
                progressBar4.Value = index;
            }

            //if (lbImportedFiles.InvokeRequired)
            //{
            //    lbImportedFiles.Invoke((MethodInvoker)delegate
            //    {
            //        lbImportedFiles.Text = index.ToString();
            //    });
            //}
            //else
            //{
            //    lbImportedFiles.Text = index.ToString();
            //}
        }

        private static string[] CyrilicToLatinL = "a,b,v,g,d,e,zh,z,i,j,k,l,m,n,o,p,r,s,t,u,f,kh,c,ch,sh,sch,j,y,j,e,yu,ya".Split(',');
        private static string[] CyrilicToLatinU = "A,B,V,G,D,E,Zh,Z,I,J,K,L,M,N,O,P,R,S,T,U,F,Kh,C,Ch,Sh,Sch,J,Y,J,E,Yu,Ya".Split(',');

        public static string CyrilicToLatin(string s)
        {
            var sb = new StringBuilder((int)(s.Length * 1.5));
            foreach (char c in s)
            {
                if (c >= '\x430' && c <= '\x44f') sb.Append(CyrilicToLatinL[c - '\x430']);
                else if (c >= '\x410' && c <= '\x42f') sb.Append(CyrilicToLatinU[c - '\x410']);
                else if (c == '\x401') sb.Append("Yo");
                else if (c == '\x451') sb.Append("yo");
                else sb.Append(c);
            }
            return sb.ToString();
        }
        private string getNextFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName);

            int i = 0;
            while (File.Exists(fileName))
            {
                if (i == 0)
                    fileName = fileName.Replace(extension, "(" + ++i + ")" + extension);
                else
                    fileName = fileName.Replace("(" + i + ")" + extension, "(" + ++i + ")" + extension);
            }

            return fileName;
        }

        public void GetCodeGen(object state)
        {
            string[] files = Directory.GetFiles(textBox2.Text);

            if (files.Length > 2)
            {
                Program.SetMetaData("[");
            }

            using (var codegen = new NCodegen(Convert.ToInt32(textBox4.Text), Convert.ToInt32(textBox5.Text)))
            {

                codegen.Start((data) =>
                {
                    // CodeGen = data;
                    if (files.Length > 2)
                    {
                        Program.SetMetaData(",");
                    }
                    Program.SetMetaData(string.Empty);
                    Program.SetMetaData(JsonConvert.SerializeObject(data, Formatting.Indented));

                });
                foreach (var item in files)
                {
                    Program.Message(string.Format("{0:HH:mm:ss} - {1}", DateTime.Now, item));

                    codegen.AddFile(item);
                }
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            progressBar1.Show();
            JsonOutput.Clear();
            textBox6.Clear();
            ThreadPool.QueueUserWorkItem(CallBack);
        }

        public void AppendText(string what)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate () { AppendText(what); }));
            }
            else
            {
                DateTime timestamp = DateTime.Now;
                textBox3.AppendText(timestamp.ToLongTimeString() + "\t" + what + Environment.NewLine);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Program.ClearTextBox();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.UpdateSetting("starttime", textBox4.Text ?? "10");
            Program.UpdateSetting("endtime", textBox5.Text ?? "30");
        }

        private void button1_ClickOpen(object sender, System.EventArgs e)
        {
            // var fbd = new FolderBrowserDialog() { SelectedPath = textBox1.Text ?? "c:\\" };
            //  fbd.ShowDialog();
            // textBox1.Text = fbd.SelectedPath;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(GetCodeGen);
        }

        private static string SelectedServer;

        public delegate void ProgressCallback(long position, long total);
        public void Copy(Stream inputStream, string outputFile, ProgressCallback progressCallback)
        {
            using (var outputStream = File.OpenWrite(outputFile))
            {
                const int bufferSize = 4096;
                while (inputStream.Position < inputStream.Length)
                {
                    byte[] data = new byte[bufferSize];
                    int amountRead = inputStream.Read(data, 0, bufferSize);
                    outputStream.Write(data, 0, amountRead);

                    if (progressCallback != null)
                        progressCallback(inputStream.Position, inputStream.Length);
                }
                outputStream.Flush();
            }
        }

        public bool ShowDialogBox(Code fromDb, Code newCode)
        {
            var testDialog = new dialog();

            //from db
            testDialog.textBox1.Text = fromDb.metadata.artist;
            // Show testDialog as a modal dialog and determine if DialogResult = OK.
            if (testDialog.ShowDialog(this) == DialogResult.OK)
            {
                return true;
            }
            else
            {
                return false;
            }
            testDialog.Dispose();
        }


        private static List<Task> tasks = new List<Task>();
        //private static List<_Code> CodeList = new List<_Code>();
        private static List<Code> CodeList = new List<Code>();
        private static List<Code> CodeListBase = new List<Code>();

        public static bool isnext = false;
        //private System.Timers.Timer tTimer;

        //private void EnableImport()
        //{
        //    if (button7.InvokeRequired)
        //    {
        //        button7.Invoke(
        //            (MethodInvoker)delegate
        //            {
        //                button7.Enabled = true;
        //            });
        //    }
        //    else
        //    {
        //        button7.Enabled = true;
        //    }
        //}

        private EchonestAPI Api;

        private async Task<string> SetItem(Code item, bool skip_Check_existed, string file)
        {
            if (item == null)
            {
                return null;
            }

            Api = new EchonestAPI();
            item.metadata.version = 4.12f;

            var result = await Api.AsyncInsertSongToDataBaseChecking(item, SelectedServer, file, Program.Message, skip_Check_existed);
            Program.Message(JsonConvert.SerializeObject(result));
            return result;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            var url = "";
            //this.Invoke(new MethodInvoker(delegate () { url = comboBox1.SelectedItem.ToString(); }));
            this.Invoke(new MethodInvoker(delegate () { url = tburl.Text; }));
            var code = new Code();
            this.Invoke(new MethodInvoker(delegate () { code = JsonConvert.DeserializeObject<Code>(JsonOutput.Text); }));

            ThreadPool.QueueUserWorkItem(state =>
            {
                EchonestAPI.IdentifySong(code, url, (result) =>
                    {
                        Program.SetMetaDataResponse(result.Data);
                        Program.Message(result.Data);
                    });
            });
        }

        private void button15_Click(object sender, EventArgs e)
        {
            textBox3.Clear();
        }

        private void SavetoFile_Click(object sender, EventArgs e)
        {
            try
            {
                var fn = getNextFileName(Path.Combine(Directory.GetCurrentDirectory(), "allInOne_7.json"));
                // var CodeList = new List<_Code>();
                var CodeList = new List<Code>();
                for (int i = 10000; i < resultListNest.Count - 1; i++)
                {
                    CodeList.Add(resultListNest[i]);
                }

                File.WriteAllText(fn, JsonConvert.SerializeObject(resultListNest, Formatting.Indented));

            }
            catch (Exception ex)
            {
                Program.Message(ex.Message);
                //throw;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog() { SelectedPath = textBox2.Text ?? "c:\\" };
            fbd.ShowDialog();
            textBox2.Text = fbd.SelectedPath;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += Bw_DoWork;
                bw.RunWorkerCompleted += Bw_RunWorkerCompleted;
                bw.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                Program.Message(ex.Message);
            }
        }

        private void Bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //ConvertAllWavToMp3();
            repairMp3Tags(getmp3Files());
        }

        private void Bw_DoWork(object sender, DoWorkEventArgs e)
        {
            ConvertAllWavToMp3();
        }

        private void ConvertAllWavToMp3()
        {

            RepairTitle();

            Program.Message("Start converting...");
            var files = getAllFiles();

            foreach (var file in files)
            {
                if (file.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    string mp3Name = file.Replace(".wav", ".mp3");

                    if (File.Exists(mp3Name))
                        continue;

                    WaveToMP3(file, mp3Name);
                }
            }

            Program.Message("Finished converting!");
        }

        private void RepairTitle()
        {
            Program.Message("Start converting...");
            var files = getmp3Files();
            var count = 0;
            foreach (var file in files)
            {

                string fileName = Path.GetFileName(file);
                var fileNameFormated = CyrilicToLatin(fileName);

                var mp3Name = Path.Combine(Path.GetDirectoryName(file), fileNameFormated);


                if (!File.Exists(mp3Name))
                {
                    File.Move(file, mp3Name);
                }

                if (fileName != fileNameFormated)
                {
                    count++;
                    File.Move(file, file + ".Translated");
                }

            }

            Program.Message("Finish converting! " + count + " items.");
        }

        private string[] getAllFiles()
        {
            var set = new HashSet<string> { ".mp3", ".wav" };

            var files = Directory.GetFiles(textBox2.Text ?? ConfigurationManager.AppSettings["samplefiles"], "*.*", SearchOption.AllDirectories)
          .Where(f => set.Contains(
              new FileInfo(f).Extension,
              StringComparer.OrdinalIgnoreCase));

            return files.ToArray();
        }

        private string[] getmp3Files()
        {
            var set = new HashSet<string> { ".mp3" };

            var files = Directory.GetFiles(textBox2.Text ?? ConfigurationManager.AppSettings["samplefiles"], "*.*", SearchOption.AllDirectories)
          .Where(f => set.Contains(
              new FileInfo(f).Extension,
              StringComparer.OrdinalIgnoreCase));

            return files.ToArray();
        }

        public static void WaveToMP3(string waveFileName, string mp3FileName, int bitRate = 128)
        {

            try
            {
                var readerStream = File.OpenRead(waveFileName);
                BinaryReader br = new BinaryReader(readerStream);
                if (br.ReadInt32() != mmioStringToFOURCC("RIFF", 0))
                {
                    readerStream.Dispose();
                    readerStream.Close();
                    File.Move(waveFileName, mp3FileName);
                    Program.Message(string.Format("Renamed {0} to mp3", waveFileName));
                    File.Delete(waveFileName);
                }
                else
                {
                    using (var reader = new AudioFileReader(waveFileName))
                    using (var writer = new LameMP3FileWriter(mp3FileName, reader.WaveFormat, bitRate))
                    {
                        reader.CopyTo(writer);
                        Program.Message(string.Format("Converted {0} to mp3", waveFileName));
                        if (reader != null)
                        {
                            reader.CopyTo(writer);
                            //reader.Dispose();                           
                            //reader.Close();
                        }

                        //if (writer != null)
                        //{
                        //    writer.Dispose();                           
                        //    writer.Close();
                        //}
                    }

                    //File.Delete(waveFileName);
                }
            }
            catch (Exception ex)
            {
                Program.Message(string.Format("{0} ", ex.Message));
            }
        }

        [DllImport("winmm.dll")]
        public static extern Int32 mmioStringToFOURCC([MarshalAs(UnmanagedType.LPStr)] String s, int flags);

        private void button23_Click(object sender, EventArgs e)
        {
            Program.Message("copy all prepared files to new location");
            var files = getmp3Files();
            var exist = 0;
            foreach (var item in files)
            {
                string fileName = Path.GetFileName(item);
                fileName = Path.Combine("E:\\Music\\Filtered2", fileName);
                if (!File.Exists(fileName))
                {
                    File.Move(item, fileName);
                }
                else
                {
                    exist++;
                }
            }
            Program.Message("Exist Files: " + exist);
            Program.Message("Total uniq files:" + files.Count());
        }

        private void button26_Click(object sender, EventArgs e)
        {
            SelectedServer = tburl.Text;
            ThreadPool.QueueUserWorkItem(PrepareFiles, new
            {
                path = textBox2.Text
            });
        }


        private void button27_Click(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(PrepareFiles);
        }

        private async void PrepareFiles(dynamic state)
        {
            AppendText("Started:");
            string path = state.GetType().GetProperty("path").GetValue(state, null);

            string[] files = Directory.GetFiles(path);

            //   var url = "";
            this.Invoke(new MethodInvoker(delegate () { SelectedServer = tburl.Text; }));

            foreach (var f in files)
            {
                // var data = EchoStartAsync(f).Result;
                var data = NCodegen.StartAsync(f, 30).Result;

                await SetItem(data, false, f);

                //EchonestAPI.IdentifySong(data, url, (result) =>
                //{
                //    Program.Message(result.Data);
                //});
            }
        }

        private void button27_Click_1(object sender, EventArgs e)
        {
            Stream myStream = null;
            SelectedServer = tburl.Text;


            var ofd = new OpenFileDialog();

            ofd.InitialDirectory = "c:\\";
            ofd.Filter = "JSON extension (*.json)|*.json|All files (*.*)|*.*";
            ofd.FilterIndex = 2;
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var startIndex = 0;
                    //this.Invoke(new MethodInvoker(delegate () { startIndex = Convert.ToInt32(textBox8.Text); }));

                    ThreadPool.QueueUserWorkItem(async state =>
                    {
                        try
                        {

                            if ((myStream = ofd.OpenFile()) != null)
                            {
                                using (myStream)
                                {
                                    var serializer = new JsonSerializer();

                                    using (var reader = new JsonTextReader(new StreamReader(myStream)))
                                    {
                                        var count = 0;

                                        while (reader.Read())
                                        {
                                            if (reader.TokenType == JsonToken.StartObject)
                                            {
                                                count++;

                                                if (count <= startIndex && startIndex != 0)
                                                {
                                                    continue;
                                                }

                                                //var item = serializer.Deserialize<_Code>(reader);
                                                var item = serializer.Deserialize<Code>(reader);
                                                Program.Message("[" + count + "]");

                                                await SetItem(item, false, "");

                                                if (label30.InvokeRequired)
                                                {
                                                    label30.Invoke((MethodInvoker)delegate
                                                    {
                                                        label30.Text = count.ToString();
                                                    });
                                                }
                                                else
                                                {
                                                    label30.Text = count.ToString();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Program.Message("Error: -" + ex);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Program.Message("Error: Could not read file from disk. Original error: " + ex);
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }

            Program.Message("Finish    ");
        }
    }
}
