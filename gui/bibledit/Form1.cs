using System;
using System.Diagnostics;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices;


namespace Bibledit
{

  public partial class Form1 : Form
  {

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);


    private static IntPtr MyHookID = IntPtr.Zero;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private static LowLevelKeyboardProc MyKeyboardProcessor = HookCallback;
    private static Boolean HasFocus = false;
    private static Boolean ControlPressed = false;
    private static Boolean SearchDialogOpen = false;
    

    string windowstate = "windowstate.txt";
    Process BibleditCore;
    public static ChromiumWebBrowser browser;


    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);


    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
      using (Process curProcess = Process.GetCurrentProcess())
      using (ProcessModule curModule = curProcess.MainModule)
      {
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
      }
    }


    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
      if (HasFocus)
      {
        if (nCode >= 0)
        {
          int vkCode = Marshal.ReadInt32(lParam);
          Keys keys = (Keys)vkCode;
          String key = keys.ToString();
          if (wParam == (IntPtr)WM_KEYDOWN)
          {
            if (key.Contains("Control")) ControlPressed = true;
          }
          if (wParam == (IntPtr)WM_KEYUP)
          {
            if (key.Contains("Control")) ControlPressed = false;
          }
          if (ControlPressed && key.Equals("F") && (wParam == (IntPtr)WM_KEYDOWN))
          {
            SearchWebkit();
          }
        }
      }
      return CallNextHookEx(MyHookID, nCode, wParam, lParam);
    }


    public void InitBrowser()
    {
      Cef.Initialize(new CefSettings());
      browser = new ChromiumWebBrowser("http://localhost:9876");
      Controls.Add(browser);
      browser.Dock = DockStyle.Fill;
    }


    public Form1()
    {
      InitializeComponent();
      InitBrowser();
      MyHookID = SetHook(MyKeyboardProcessor);
    }


    ~Form1()
    {
      UnhookWindowsHookEx(MyHookID);
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

          // Read restore bounds value from file
          string value;
          value = reader.ReadLine();
          if (value == "Maximized")
          {
            WindowState = FormWindowState.Maximized;
          }
          else
          {
            value = reader.ReadLine();
            if (value != "") Left = Int32.Parse(value);
            value = reader.ReadLine();
            if (value != "") Top = Int32.Parse(value);
            value = reader.ReadLine();
            if (value != "") Width = Int32.Parse(value);
            value = reader.ReadLine();
            if (value != "") Height = Int32.Parse(value);
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
      BibleditCore.StartInfo.FileName = "server.exe";
      BibleditCore.StartInfo.Arguments = "";
      BibleditCore.StartInfo.CreateNoWindow = true;
      BibleditCore.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
      BibleditCore.EnableRaisingEvents = true;
      BibleditCore.Exited += new EventHandler(ProcessExited);
      BibleditCore.Start();
      // Set the server to run on one processor only. 
      // That gives a huge boost to the speed of the Cygwin library.
      // The difference in speed is clear: It runs times faster.
      // When a background task runs in Bibledit, the GUI takes a long time to respond without this processor affinity.
      // http://zachsaw.blogspot.nl/2012/10/multithreading-under-cygwin.html
      // http://stackoverflow.com/questions/2510593/how-can-i-set-processor-affinity-in-net
      // What works well too: PsExec.exe -a 1 server.exe
      // After the C++ code was compiled through Visual Studio, the processor limit is no longer relevant.
      // BibleditCore.ProcessorAffinity = (IntPtr)1;
    }


    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
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
      HasFocus = true;
    }


    private void Form1_Deactivate(object sender, EventArgs e)
    {
      HasFocus = false;
    }


    private static void SearchWebkit ()
    {
      if (SearchDialogOpen) return;
      SearchDialogOpen = true;

      System.Drawing.Size size = new System.Drawing.Size(200, 70);
      Form inputBox = new Form();

      inputBox.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      inputBox.ClientSize = size;
      inputBox.Text = "Search";

      System.Windows.Forms.TextBox textBox = new TextBox();
      textBox.Size = new System.Drawing.Size(size.Width - 10, 23);
      textBox.Location = new System.Drawing.Point(5, 5);
      inputBox.Controls.Add(textBox);

      Button okButton = new Button();
      okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
      okButton.Size = new System.Drawing.Size(75, 23);
      okButton.Text = "&OK";
      okButton.Location = new System.Drawing.Point(size.Width - 80 - 80, 39);
      inputBox.Controls.Add(okButton);

      Button cancelButton = new Button();
      cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      cancelButton.Size = new System.Drawing.Size(75, 23);
      cancelButton.Text = "&Cancel";
      cancelButton.Location = new System.Drawing.Point(size.Width - 80, 39);
      inputBox.Controls.Add(cancelButton);

      inputBox.FormBorderStyle = FormBorderStyle.FixedDialog;
      inputBox.MinimizeBox = false;
      inputBox.MaximizeBox = false;

      inputBox.AcceptButton = okButton;
      inputBox.CancelButton = cancelButton;

      inputBox.StartPosition = FormStartPosition.CenterParent;
      DialogResult result = inputBox.ShowDialog();
      if (result == System.Windows.Forms.DialogResult.OK)
      {
        string search = textBox.Text;
        // If the users enters an empty string, any markup is supposed to be removed from the webview.
        // This is done by searching for something that is not likely to be found.
        if (search.Length == 0) search = "b.i.b.l.e.d.i.t";
        WebBrowserExtensions.Find(browser, 1, search, true, false, false);
      }

      SearchDialogOpen = false;
    }

  }
}
