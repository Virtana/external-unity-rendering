using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ExternalUnityRendering.PathManagement
{

    // TODO: consider making Interface or class for this and Dirmanager to inherit from
    class FileManager
    {
        private FileInfo _file;

        public FileInfo File
        {
            get
            {
                _file.Refresh();
                return _file;
            }
        }

        // TODO: consider whether this should throw an exception
        // HACK: if initialization fails, File is null
        public string Path
        {
            get
            {
                // HACK using null conditional until decision is made
                return _file?.FullName;
            }
            set
            {
                try
                {
                    FileInfo file = new FileInfo(value);
                    if (file.Exists)
                    {
                        int i = 1;
                        string dir = file.Directory.FullName;
                        string name = file.Name;
                        string extension = file.Extension;
                        string filename = "";

                        do
                        {
                            // Rename in the same way windows does.
                            filename = System.IO.Path.Combine(
                                        dir, $"{ name } ({ i++ }){ extension }");
                        } while (System.IO.File.Exists(filename));

                        file = new FileInfo(filename);
                    }
                    file.Create().Close();
                    _file = file;
                }
                catch (ArgumentNullException ane)
                {
                    Debug.LogError($"Cannot set file path to null.\n{ ane }");
                }
                catch (ArgumentException ae)
                {
                    Debug.LogError($"The folder <{ value }> is empty or contains "
                        + $"invalid characters.\n{ ae }");
                }
                catch (System.Security.SecurityException se)
                {
                    Debug.LogError("You do not have the permissions to access "
                        + $"<{ value }>.\n{ se }");
                }
                catch (UnauthorizedAccessException uae)
                {
                    Debug.LogError($"Access to <{ value }> is denied.\n{ uae }");
                }
                catch (PathTooLongException ptle)
                {
                    // TODO: Implement retry with extended filename for windows
                    Debug.LogError($"The path <{ value }> is too long.\n{ ptle }");
                }
                catch (NotSupportedException nse)
                {
                    Debug.LogError($"The path <{ value }> contains a colon.\n{ nse }");
                }
                catch (IOException ioe)
                {
                    Debug.LogError($"The file <{ value }> could not be created.\n{ ioe }");
                }
            }
        }

        public FileManager()
        {
            // Create a near-guaranteed unique file. See the first
            // comment on https://stackoverflow.com/a/11938280 
            FileInfo file = new FileInfo(
                System.IO.Path.Combine(Application.persistentDataPath,
                (
                    DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff-UTCzz")
                    + $" { Guid.NewGuid() }.dat"
                )));

            file.Create().Close();
            _file = file;
        }

        // HACK Tries to detect if path is relative (a filename) or absolute
        public FileManager(string path) 
        { 
            // HACK May not be best implementation for windows, can't find .net source 
            // for Windows implementation in .net 5. Maybe not needed?
            if (System.IO.Path.IsPathRooted(path))
            {
                Path = path;
            } else
            {
                Path = System.IO.Path.Combine(Application.persistentDataPath, path);
            }
        }

        public FileManager(string folder, string name)
                    : this(new DirectoryManager(folder), name) { }

        public FileManager(DirectoryManager directory, string name) 
        {
            Path = System.IO.Path.Combine(directory.Path, name);
        }

        // OPTIONAL add generic serialization options
        // OPTIONAL retry options?
        public void WriteToFile(string data, bool append = true)
        {
            FileMode mode = append ? FileMode.Append : FileMode.Truncate;

            try
            {
                using (FileStream stream = _file.Open(mode))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine(data);
                }
                return;
            }
            catch (FileNotFoundException fnfe)
            {
                Debug.LogError($"The file has not been found.\n{ fnfe }");
            }
            catch (DirectoryNotFoundException dnfe)
            {
                // Constructor enforces a valid directory, so this error must be due
                // to some sort of lock or deletion
                Debug.LogError($"The file directory may have been deleted.\n{ dnfe }");
            }
            catch (System.Security.SecurityException se)
            {
                Debug.LogError($"The caller does not have the required permission.\n{ se }");
            }
            catch (UnauthorizedAccessException uae)
            {
                Debug.LogError($"Name is read-only.\n{ uae }");
            }
            catch (ObjectDisposedException ode)
            {
                Debug.LogError("The StreamWriter buffer may be full, and current writer " +
                    $"is closed.\n{ ode }");
            }
            catch (NotSupportedException nse)
            {
                Debug.LogError("The StreamWriter buffer may full, and the contents of the " +
                    "buffer cannot be written to the underlying fixed size stream because " +
                    $"the StreamWriter is at the end the stream.\n{ nse }");
            }
            catch (IOException ioe)
            {
                Debug.LogError("The file is already opened by another process or an I/O " +
                    $"error has occurred while writing to file.\n{ ioe }");
            }
        }

        public void WriteToFile(byte[] data)
        {
            if (data == null)
            {
                Debug.LogError("Cannot write a null array to disk.");
                return;
            }

            try
            {
                using (FileStream stream = _file.OpenWrite())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            catch (UnauthorizedAccessException uae)
            {
                Debug.LogError($"Name is read-only.\n{ uae }");
            }
            catch (DirectoryNotFoundException dnfe)
            {
                // Constructor enforces a valid directory, so this error must be due
                // to some sort of lock or deletion
                Debug.LogError($"The file directory may have been deleted.\n{ dnfe }");
            }
            catch (ArgumentOutOfRangeException aoore)
            {
                // normally would trigger when the second and/or third params to
                // stream.Write are negative, but that **should** not be possible. 
                // Multithreading issue??
                Debug.LogError($"Could not write data in array to stream.\n{ aoore }");
            }
            catch (ArgumentException ae)
            {
                // same as above but with generally invalid values e.g outside range of array
                Debug.LogError($"Could not write data in array to stream.\n{ ae }");
            }
            catch (ObjectDisposedException ode)
            {
                Debug.LogError($"The stream is closed.\n{ ode }");
            }
            catch (NotSupportedException nse)
            {
                Debug.LogError("The StreamWriter buffer may full, and the contents of the " +
                    "buffer cannot be written to the underlying fixed size stream because " +
                    $"the StreamWriter is at the end the stream.\n{ nse }");
            }
            catch (IOException ioe)
            {
                Debug.LogError("An I/O error occurred or another thread may have caused " +
                    "an unexpected change in the position of the operating system's file " +
                    $"handle.\n{ ioe }");
            }
        }

        public string ReadFile()
        {
            StringBuilder data = new StringBuilder();
            try
            {
                using (FileStream stream = _file.OpenRead())
                using (StreamReader reader = new StreamReader(stream))
                {
                    data.Append(reader.ReadToEnd());
                }
            }
            catch (UnauthorizedAccessException uae)
            {
                Debug.LogError($"Name is read-only.\n{ uae }");
            }
            catch (DirectoryNotFoundException dnfe)
            {
                // Constructor enforces a valid directory, so this error must be due
                // to some sort of lock or deletion
                Debug.LogError($"The file directory may have been deleted.\n{ dnfe }");
            }
            catch (ArgumentException ae)
            {
                Debug.LogError($"The stream does not support reading.\n{ ae }");
            }
            catch (IOException ioe)
            {
                Debug.LogError("The file is already open or an I/O error has occured." +
                    $"\n{ ioe }");
            }
            catch (OutOfMemoryException oome)
            {
                // log but don't handle. Unity **should** handle this appropriately
                // The following link (split in two)
                // https://docs.microsoft.com/en-us/dotnet/api/system.outofmemoryexception?
                // view=net-5.0#:~:text=This%20type%20of%20OutOfMemoryException,example%20does.
                // says that Environment.FailFast() should be called, but unity should do that
                Debug.LogError("Catastrophic error. Out of memory when trying to " +
                    $"read from { _file.FullName }.\n{ oome }");
                throw; 
            }

            return data.ToString();
        }
    }
}
