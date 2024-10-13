using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices;
using System.Timers;
using System.Net.Sockets;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace Bibledit
{

    public partial class Form1 : Form
    {
        // The file to save and retrieve the state of the main window.
        string windowstate = "windowstate.txt";
        // The server process, that is, the Bibledit core.
        Process BibleditCore;
        // Timer for checking on whether to open a URL in the external browser.
        private System.Timers.Timer externalUrlTimer;
        // Timer for checking on a focused reference from Paratext.
        private System.Timers.Timer focusedReferenceTimer;
        // Timer for initializing the browser once.
        private DispatcherTimer dispatcherTimer = null;
        // The port number to navigate to, determined by the Bibldit core.
        String portNumber = String.Empty;


        public Form1()
        {
            InitializeComponent();
            webView.NavigationStarting += NavigationStarting;
            webView.NavigationCompleted += NavigationCompleted;
            webView.SourceChanged += SourceChanged;
            InitializeAsync();

            // Create a timer with a one-second interval.
            externalUrlTimer = new System.Timers.Timer(1000);
            // Hook up the Elapsed event for the timer. 
            externalUrlTimer.Elapsed += OnTimedEvent;
            externalUrlTimer.AutoReset = false;
            externalUrlTimer.Start();

            // Create a timer for receiving the Paratext focused reference.
            focusedReferenceTimer = new System.Timers.Timer(1000);
            focusedReferenceTimer.Elapsed += OnFocusedReferenceTimedEvent;
            focusedReferenceTimer.AutoReset = false;
            focusedReferenceTimer.Start();
        }

        ~Form1()
        {
            externalUrlTimer.Stop();
            externalUrlTimer.Dispose();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            // Set screen size and state as was saved by the previous session.
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForAssembly();
            try
            {
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(windowstate, FileMode.Open, storage))
                using (StreamReader reader = new StreamReader(stream))
                {

                    // Read restore bounds value from file.
                    string value;
                    value = reader.ReadLine();
                    if (value == "Maximized")
                    {
                        WindowState = FormWindowState.Maximized;
                    }
                    else
                    {
                        int screen_left = Screen.FromControl(this).Bounds.Left;
                        int screen_width = Screen.FromControl(this).Bounds.Width;
                        int screen_top = Screen.FromControl(this).Bounds.Top;
                        int screen_height = Screen.FromControl(this).Bounds.Height;
                        int form_left = screen_left;
                        int form_width = screen_width;
                        int form_top = screen_top;
                        int form_height = screen_height;
                        bool reset_screen_sizes = false;
                        value = reader.ReadLine();
                        if (value != "") form_left = Int32.Parse(value);
                        value = reader.ReadLine();
                        if (value != "") form_top = Int32.Parse(value);
                        value = reader.ReadLine();
                        if (value != "") form_width = Int32.Parse(value);
                        value = reader.ReadLine();
                        if (value != "") form_height = Int32.Parse(value);
                        // Fix negative or bad values. Allow for a small error margin.
                        if (form_left < screen_left - 25) reset_screen_sizes = true;
                        if (form_width + form_left > screen_left + screen_width + 50) reset_screen_sizes = true;
                        if (form_top < screen_top - 25) reset_screen_sizes = true;
                        if (form_height + form_top > screen_height + screen_top + 50) reset_screen_sizes = true;
                        if (form_height < 300) reset_screen_sizes = true;
                        if (form_width < 400) reset_screen_sizes = true;
                        if (!reset_screen_sizes)
                        {
                            Left = form_left;
                            Top = form_top;
                            Width = form_width;
                            Height = form_height;
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // Handle case when file is not found in isolated storage.
                // This happens on starting the app for the first time.
                // Or when the file is no longer available.
            }


            // Kill any previous servers. This frees the port to connect to.
            foreach (var process in Process.GetProcessesByName("server"))
            {
                process.Kill();
            }
            BibleditCore = new Process();
            BibleditCore.StartInfo.WorkingDirectory = System.IO.Path.Combine(Application.StartupPath);
            BibleditCore.StartInfo.FileName = System.IO.Path.Combine(Application.StartupPath, "server.exe");
            //BibleditCore.StartInfo.WorkingDirectory = @"C:\bibledit-windows";
            //BibleditCore.StartInfo.FileName = @"C:\bibledit-windows\server.exe";
            BibleditCore.StartInfo.Arguments = "";
            BibleditCore.StartInfo.CreateNoWindow = true;
            BibleditCore.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            BibleditCore.EnableRaisingEvents = true;
            BibleditCore.Exited += new EventHandler(ProcessExited);
            BibleditCore.StartInfo.UseShellExecute = false;
            BibleditCore.StartInfo.RedirectStandardOutput = true;
            BibleditCore.EnableRaisingEvents = true;
            BibleditCore.OutputDataReceived += new DataReceivedEventHandler((sender2, e2) =>
            {
                String line = e2.Data;
                if (!String.IsNullOrWhiteSpace(line))
                {
                    String number = Regex.Match(e2.Data, @"\d+$").Value;
                    if (!String.IsNullOrWhiteSpace(number))
                    {
                        portNumber = number;
                        //Console.WriteLine(portNumber);
                    }
                }
            });
            BibleditCore.Start();

            // Asynchronously read the standard output of the spawned process.
            // This raises OutputDataReceived events for each line of output.
            BibleditCore.BeginOutputReadLine();
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // Save window state for the next time this window is opened.
                IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForAssembly();
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(windowstate, FileMode.Create, storage))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    // Write window state to file.
                    writer.WriteLine(WindowState.ToString());
                    writer.WriteLine(Location.X.ToString());
                    writer.WriteLine(Location.Y.ToString());
                    writer.WriteLine(Size.Width.ToString());
                    writer.WriteLine(Size.Height.ToString());
                }
            }
            catch (FileNotFoundException)
            {
                // Handle case when file cannot be stored in isolated storage.
                // This has happened in the case reported below.
                // https://github.com/bibledit/cloud/issues/266
            }

            // Kill the bibledit core.
            BibleditCore.EnableRaisingEvents = false;
            BibleditCore.CloseMainWindow();
            BibleditCore.Kill();
            BibleditCore.WaitForExit();
            BibleditCore.Close();
        }


        private void ProcessExited(object sender, System.EventArgs e)
        {
            // When the Bibledit server process exits or crashes, restart it straightaway.
            BibleditCore.Start();
        }


        private void Form1_Activated(object sender, EventArgs e)
        {
        }


        private void Form1_Deactivate(object sender, EventArgs e)
        {
        }


        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                // Connect to the local Bibledit client server.
                TcpClient socket = new TcpClient();
                socket.Connect("localhost", int.Parse(portNumber));
                // Fetch the link that indicates to open an external website.
                NetworkStream ns = socket.GetStream();
                StreamWriter sw = new StreamWriter(ns);
                sw.WriteLine("GET /assets/external HTTP/1.1");
                sw.WriteLine("");
                sw.Flush();
                // Read the response from the local Bibledit client server.
                String response;
                StreamReader sr = new StreamReader(ns);
                do
                {
                    response = sr.ReadLine();
                    // Check for a URL to open.
                    if ((response != null) && (response.Length > 4) && (response.Substring(0, 4) == "http"))
                    {
                        // Open the URL in default web browser.
                        System.Diagnostics.Process.Start(response);
                    }
                }
                while (response != null);
                // Close connection.
                socket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            // Restart the timeout.
            externalUrlTimer.Stop();
            externalUrlTimer.Start();
        }


        private void OnFocusedReferenceTimedEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                // Read the reference from Paratext from the registry.
                String SantaFeFocusKey = @"SOFTWARE\SantaFe\Focus\";
                String ScriptureReferenceKey = "ScriptureReference";
                //String ScriptureReferenceListKey = "ScriptureReferenceList";
                using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                using (var key = hkcu.OpenSubKey(SantaFeFocusKey + ScriptureReferenceKey))
                {
                    if (key != null)
                    {
                        var value = key?.GetValue("");
                        if (value != null)
                        {
                            String focus = value.ToString();
                            // Encode the space.
                            focus = Uri.EscapeUriString(focus);
                            // Connect to the local Bibledit client server.
                            TcpClient socket = new TcpClient();
                            socket.Connect("localhost", 9876);
                            // Send the Paratext focused reference to the correct link.
                            NetworkStream ns = socket.GetStream();
                            StreamWriter sw = new StreamWriter(ns);
                            sw.WriteLine("GET /navigation/paratext?from=" + focus + " HTTP/1.1");
                            sw.WriteLine("");
                            sw.Flush();
                            // Read the response from the local Bibledit client server.
                            String response;
                            StreamReader sr = new StreamReader(ns);
                            do
                            {
                                response = sr.ReadLine();
                            }
                            while (response != null);
                            // Close connection.
                            socket.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            // Restart the timeout.
            focusedReferenceTimer.Stop();
            focusedReferenceTimer.Start();
        }


        private void WebviewLoaderTick(object sender, EventArgs e)
        {
            // It could be that the kernel has not yet decided which port number to serve.
            // Handle that situation here.
            if (String.IsNullOrEmpty(portNumber)) 
                return;

            // Navigate to the port nuber that the Bibledit core is serving.
            webView.CoreWebView2.Navigate("http://localhost:" + portNumber);

            // No next cycle anymore.
            dispatcherTimer.Stop();
        }


        void NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs args)
        {
            //String uri = args.Uri;
        }



        void NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs args)
        {
        }


        void SourceChanged(object sender, CoreWebView2SourceChangedEventArgs args)
        {
        }


        async void InitializeAsync()
        {
            // Await EnsureCoreWebView2Async, because the initialization of CoreWebView2 is asynchronous.
            await webView.EnsureCoreWebView2Async(null);
            // After the webview has been initialized, start a timer to start the process of navigating to the local server.
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(WebviewLoaderTick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            dispatcherTimer.Start();
        }

    }
}
