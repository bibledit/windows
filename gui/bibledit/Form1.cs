using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Net;
using CefSharp;
using CefSharp.WinForms;


namespace Bibledit
{

  public partial class Form1 : Form
  {

    //[DllImport("bibleditlibrarywrapper.dll")]
    //public static extern string bibledit_wrapper_get_version_number();
    Process LibBibledit;


    public ChromiumWebBrowser browser;


    public void InitBrowser()
    {
      Cef.Initialize(new CefSettings());
      browser = new ChromiumWebBrowser("http://localhost:9876");
      this.Controls.Add(browser);
      browser.Dock = DockStyle.Fill;
    }


    public Form1()
    {
      InitializeComponent();
      InitBrowser();
    }


    private void Form1_Load(object sender, EventArgs e)
    {
      feedback("");
      // Kill any previous servers. This frees the port to connect to.
      foreach (var process in Process.GetProcessesByName("server"))
      {
        process.Kill();
      }
      try
      {
        //feedback(bibledit_wrapper_get_version_number ());
        LibBibledit = new Process();
        LibBibledit.StartInfo.WorkingDirectory = System.IO.Path.Combine(Application.StartupPath);
        LibBibledit.StartInfo.FileName = "server.exe";
        LibBibledit.StartInfo.Arguments = "";
        LibBibledit.StartInfo.CreateNoWindow = true;
        LibBibledit.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        LibBibledit.EnableRaisingEvents = true;
        LibBibledit.Exited += new EventHandler(ProcessExited);
        LibBibledit.Start();
        // Set the server to run on one processor only. 
        // That gives a huge boost to the speed of the Cygwin library.
        // The difference in speed is clear: It runs times faster.
        // When a background task runs in Bibledit, the GUI takes a long time to respond without this processor affinity.
        // http://zachsaw.blogspot.nl/2012/10/multithreading-under-cygwin.html
        // http://stackoverflow.com/questions/2510593/how-can-i-set-processor-affinity-in-net
        // What works well too: PsExec.exe -a 1 server.exe
        // After the C++ code was compiled through Visual Studio, the processor limit is no longer relevant.
        // LibBibledit.ProcessorAffinity = (IntPtr)1;
      }
      catch (Exception exception)
      {
        feedback(exception.Message);
      }
    }


    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
      try
      {
        LibBibledit.EnableRaisingEvents = false;
        LibBibledit.CloseMainWindow();
        LibBibledit.Kill();
        LibBibledit.WaitForExit();
        LibBibledit.Close();
      }
      catch (Exception exception)
      {
        feedback(exception.Message);
      }
    }


    private void feedback(String message)
    {
      Console.WriteLine (message);
    }


    private void ProcessExited(object sender, System.EventArgs e)
    {
      // When the Bibledit library exits or crashea, restart it straightaway.
      LibBibledit.Start();
    }

  }
}
