/*
Copyright (©) 2003-2024 Teus Benschop.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/


#include <filter/shell.h>
#include <filter/string.h>
#include <filter/url.h>
#include <database/logs.h>
#ifndef HAVE_CLIENT
#include <sys/wait.h>
#endif
#ifdef HAVE_WINDOWS
#include <tlhelp32.h>
#endif
#include <developer/logic.h>


static std::string filter_shell_escape_argument (std::string argument)
{
  argument = filter::strings::replace ("'", "\\'", argument);
  argument.insert (0, "'");
  argument.append ("'");
  return argument;
}


// Runs shell $command in folder $directory, with $parameters.
// If $output and $error are non-nullptr, that is where the output of the shell command goes.
// If they are nullptr, the output of the shell command goes to the Journal.
int filter_shell_run ([[maybe_unused]] std::string directory,
                      std::string command,
                      [[maybe_unused]] const std::vector <std::string> parameters,
                      [[maybe_unused]] std::string * output,
                      [[maybe_unused]] std::string * error)
{
#ifdef HAVE_CLIENT
  Database_Logs::log ("Did not run on client: " + command);
  return 0;
#else
  command = filter_shell_escape_argument (command);
  if (!directory.empty ()) {
    directory = filter_shell_escape_argument (directory);
    command.insert (0, "cd " + directory + "; ");
  }
  for (std::string parameter : parameters) {
    parameter = filter_shell_escape_argument (parameter);
    command.append (" " + parameter);
  }
  std::string pipe = filter_url_tempfile ();
  std::string standardout = pipe + ".out";
  std::string standarderr = pipe + ".err";
  command.append (" > " + standardout);
  command.append (" 2> " + standarderr);
  int result = system (command.c_str());
  std::string contents = filter_url_file_get_contents (standardout);
  if (output) {
    output->assign (contents);
  } else {
    Database_Logs::log (contents);
  }
  contents = filter_url_file_get_contents (standarderr);
  if (error) {
    error->assign (contents);
  } else {
    Database_Logs::log (contents);
  }
  filter_url_unlink (standardout);
  filter_url_unlink (standarderr);
  return result;
#endif
}


// Runs $command with $parameters.
// It does not run $command through the shell, but executes it straight.
int filter_shell_run (std::string command,
                      [[maybe_unused]] const char * parameter,
                      [[maybe_unused]] std::string & output)
{
#ifdef HAVE_CLIENT
  Database_Logs::log ("Did not run on client: " + command);
  return 0;
#else
  // File descriptor for file to write child's stdout to.
  std::string path = filter_url_tempfile () + ".txt";
  int fd = open (path.c_str (), O_WRONLY|O_CREAT, 0666);
  
  // Create child process as a duplicate of this process.
  pid_t pid = fork ();
  
  if (pid == 0) {
    
    // This runs in the child.
    dup2(fd, 1);
    close(fd);
    execlp (command.c_str(), parameter, nullptr);
    // The above only returns in case of an error.
    Database_Logs::log (strerror (errno));
    // Use_exit instead of exit, so there's no flushing.
    _exit (1);
    //close (fd);
    return -1;
  }
  
  // Wait till child is ready.
  wait(nullptr);
  close(fd);
  
  // Read the child's output.
  output = filter_url_file_get_contents (path);
#endif
  
  return 0;
}


// Runs $command as if it were typed on the command line.
// Does not escape anything in the $command.
// Returns the exit code of the process.
// The output of the process, both stdout and stderr, go into $out_err.
int filter_shell_run (std::string command, std::string & out_err)
{
#ifdef HAVE_IOS
  return 0;
#else
  std::string pipe = filter_url_tempfile ();
  command.append (" > " + pipe + " 2>&1");
  int result = system (command.c_str());
  out_err = filter_url_file_get_contents (pipe);
  return result;
#endif
}


// Returns true if $program is present on the system.
bool filter_shell_is_present (std::string program)
{
  // This crashes on iOS, so skip it.
#ifdef HAVE_IOS
  return false;
#else
  std::string command = "which " + program + " > /dev/null 2>&1";
  int exitcode = system (command.c_str ());
  return (exitcode == 0);
#endif
}


// Lists the running processes.
std::vector <std::string> filter_shell_active_processes ()
{
  std::vector <std::string> processes;

#ifdef HAVE_WINDOWS

  HANDLE hProcessSnap = CreateToolhelp32Snapshot (TH32CS_SNAPPROCESS, 0);
  if (hProcessSnap != INVALID_HANDLE_VALUE) {
    PROCESSENTRY32 pe32;
    pe32.dwSize = sizeof (PROCESSENTRY32);
    if (Process32First (hProcessSnap, &pe32)) {
      processes.push_back (filter::strings::wstring2string (pe32.szExeFile));
      while (Process32Next (hProcessSnap, &pe32)) {
        processes.push_back (filter::strings::wstring2string (pe32.szExeFile));
      }
      CloseHandle (hProcessSnap);
    }
  }

#else

  std::string output;
  filter_shell_run ("ps ax", output);
  processes = filter::strings::explode (output, '\n');

#endif
  
  return processes;
}


// Runs $command with $p1, $p2, etc...
// If $directory is given, the process changes the working directory to that.
// It does not run $command through the shell, but executes it through vfork,
// which is the fastest possibble way to run a child process.
int filter_shell_vfork ([[maybe_unused]] std::string & output,
                        [[maybe_unused]] std::string directory,
                        [[maybe_unused]] std::string command,
                        [[maybe_unused]] const char * p01,
                        [[maybe_unused]] const char * p02,
                        [[maybe_unused]] const char * p03,
                        [[maybe_unused]] const char * p04,
                        [[maybe_unused]] const char * p05,
                        [[maybe_unused]] const char * p06,
                        [[maybe_unused]] const char * p07,
                        [[maybe_unused]] const char * p08,
                        [[maybe_unused]] const char * p09,
                        [[maybe_unused]] const char * p10,
                        [[maybe_unused]] const char * p11,
                        [[maybe_unused]] const char * p12,
                        [[maybe_unused]] const char * p13)
{
  int status = 0;
#ifdef HAVE_CLIENT
  Database_Logs::log ("Did not run on client: " + command);
#else

  // File descriptors for files to write child's stdout and stderr to.
  std::string path = filter_url_tempfile () + ".txt";
  int fd = open (path.c_str (), O_WRONLY|O_CREAT, 0666);

  // It seems that waiting very shortly before calling vfork ()
  // enables running threads to continue running.
  std::this_thread::sleep_for (std::chrono::milliseconds (1));
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
  pid_t pid = vfork();
#pragma clang diagnostic pop
  if (pid != 0) {
    if (pid < 0) {
      Database_Logs::log ("Failed to run " + command);
    } else {
      wait (&status);
    }
  } else {

    // This runs in the child.
    dup2 (fd, 1);
    dup2 (fd, 2);
    close (fd);
    if (!directory.empty ()) {
      [[maybe_unused]] int result = chdir (directory.c_str());
    }
    execlp (command.c_str(), command.c_str(), p01, p02, p03, p04, p05, p06, p07, p08, p09, p10, p11, p12, p13, nullptr);
    // The above only returns in case of an error.
    Database_Logs::log (command + ": " + strerror (errno));
    _exit (1);
    //close (fd);
    return -1;
  }
  
  // Read the child's output.
  close (fd);
  output = filter_url_file_get_contents (path);
  filter_url_unlink (path);
  
#endif
  
  return status;
}
