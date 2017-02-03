using System;
using System.Diagnostics;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using System.IO;
using System.IO.IsolatedStorage;


namespace Bibledit
{

  public partial class Form1 : Form
  {

    string windowstate = "windowstate.txt";
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
      // Set screen size and state as was saved by the previous session.
      IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForAssembly();
      try
      {
        using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(this.windowstate, FileMode.Open, storage))
        using (StreamReader reader = new StreamReader(stream))
        {

          // Read restore bounds value from file
          string value;
          value = reader.ReadLine();
          if (value == "Maximized")
          {
            this.WindowState = FormWindowState.Maximized;
          }
          else
          {
            value = reader.ReadLine();
            if (value != "") this.Left = Int32.Parse(value);
            value = reader.ReadLine();
            if (value != "") this.Top = Int32.Parse(value);
            value = reader.ReadLine();
            if (value != "") this.Width = Int32.Parse(value);
            value = reader.ReadLine();
            if (value != "") this.Height = Int32.Parse(value);
          }
        }
      }
      catch (FileNotFoundException)
      {
        // Handle when file is not found in isolated storage, which is when:
        // * This is the first application session.
        // * The file has been deleted.
      }


      // Kill any previous servers. This frees the port to connect to.
      foreach (var process in Process.GetProcessesByName("server"))
      {
        process.Kill();
      }
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


    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
      // Save window state for the next time this window is opened.
      IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForAssembly();
      using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(this.windowstate, FileMode.Create, storage))
      using (StreamWriter writer = new StreamWriter(stream))
      {
        // Write window state to file.
        writer.WriteLine(this.WindowState.ToString());
        writer.WriteLine(this.Location.X.ToString());
        writer.WriteLine(this.Location.Y.ToString());
        writer.WriteLine(this.Size.Width.ToString());
        writer.WriteLine(this.Size.Height.ToString());
      }


      LibBibledit.EnableRaisingEvents = false;
      LibBibledit.CloseMainWindow();
      LibBibledit.Kill();
      LibBibledit.WaitForExit();
      LibBibledit.Close();
    }


    private void ProcessExited(object sender, System.EventArgs e)
    {
      // When the Bibledit library exits or crashea, restart it straightaway.
      LibBibledit.Start();
    }

  }
}
