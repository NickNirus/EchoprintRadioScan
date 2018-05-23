using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PopClient
{
    using System.Configuration;
    using System.Drawing.Text;
    using System.IO;
    using System.Threading;

    static class Program
    {
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

       // public static StreamWriter logger;
        public static Form1 MainForm;
        delegate void SetTextCallback(string text);
        delegate void SetTextCallbackNest(string text, bool isNest);
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            try
            {
                //if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "_EchoLogger")))
                //{
                //    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "_EchoLogger"));
                //}

                //var filename = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "_EchoLogger"), "Log.txt");

                //logger = !File.Exists(filename) ? new StreamWriter(filename, true) : File.AppendText(filename);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += OnGuiUnhandedException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                Application.Run(MainForm = new Form1());
            }
            catch (Exception e)
            {
                HandleUnhandledException(e);
            }
        }


        private static void HandleUnhandledException(Object o)
        {
            var e = o as Exception;

            if (e != null)
            {
                log.Error(e.Data + Environment.NewLine);
            }
        }

        private static void OnUnhandledException(Object sender, UnhandledExceptionEventArgs e)
        {
            HandleUnhandledException(e.ExceptionObject);
        }

        private static void OnGuiUnhandedException(object sender, ThreadExceptionEventArgs e)
        {
            HandleUnhandledException(e.Exception);
        }

        /// <summary>
        /// The message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public static void Message(string message)
        {

            if (MainForm.textBox3.InvokeRequired)
            {
                var d = new SetTextCallback(Message);
                MainForm.Invoke(d, new object[] { message });
            }
            else
            {
                var timestamp = DateTime.Now;
                MainForm.textBox3.AppendText(timestamp.ToLongTimeString() + "\t" + message + Environment.NewLine);
                log.Info(message + Environment.NewLine);
            }

        }


        public static void SetMetaData(string message)
        {
            if (MainForm.JsonOutput.InvokeRequired)
            {
                var d = new SetTextCallback(SetMetaData);
                MainForm.Invoke(d, new object[] { message });
            }
            else
            {
                MainForm.JsonOutput.AppendText(Environment.NewLine + message + Environment.NewLine);
                // logger.Write(message + Environment.NewLine);
            }
        }


        public static void SetMetaDataResponse(string message)
        {
            if (MainForm.textBox6.InvokeRequired)
            {
                var d = new SetTextCallback(SetMetaDataResponse);
                MainForm.Invoke(d, new object[] { message });
            }
            else
            {
                MainForm.textBox6.AppendText(Environment.NewLine + message + Environment.NewLine);
                //  logger.Write(message + Environment.NewLine);
            }
        }

        public static void ClearTextBox()
        {
            MainForm.textBox3.Text = string.Empty;
            MainForm.JsonOutput.Text = string.Empty;
            MainForm.textBox6.Text = string.Empty;
        }

        /// <summary>
        /// The update setting.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public static void UpdateSetting(string key, string value)
        {
            var configuration =
              ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings[key].Value = value;

            configuration.Save(ConfigurationSaveMode.Modified);

            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
