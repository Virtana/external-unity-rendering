using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ExternalUnityRendering.PathManagement
{
    // TODO: consider making Interface or class for this and Dirmanager to inherit from

    /// <summary>
    /// Class that manages file creation, reading and writing.
    /// </summary>
    public class FileManager
    {
        /// <summary>
        /// Internal reference to the file that this object manages.
        /// </summary>
        private FileInfo _file;

        /// <summary>
        /// Whether this file must be unique. Will automatically rename the
        /// </summary>
        private readonly bool _createNew = false;

        /// <summary>
        /// The path of the file that this instance manages. It will not assign invalid paths.
        /// </summary>
        public string Path
        {
            get
            {
                return _file?.FullName;
            }
            set
            {
                try
                {
                    FileInfo file = new FileInfo(value);
                    if (file.Exists)
                    {
                        if (!_createNew)
                        {
                            _file = file;
                            return;
                        }
                        int i = 1;
                        string nameWithoutExtension =
                            System.IO.Path.GetFileNameWithoutExtension(file.FullName);
                        string extension = file.Extension;
                        string filename = "";

                        do
                        {
                            // Rename in the same way windows does.
                            filename = $"{nameWithoutExtension} ({i++}){extension}";
                        } while (File.Exists(filename));

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

        /// <summary>
        /// Generate a file that should be unique. To be used as a fallback for files that must exist.
        /// </summary>
        public FileManager()
        {
            // HACK assumes no exceptions rn because if it does,
            // its likely a pathtoolong exception this is an edge case though
            // consider options
            // Create a near-guaranteed valid and unique file. See the first
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
        /// <summary>
        /// Creates a file given a path as a string.
        /// </summary>
        /// <param name="path">The path of the file to be created.</param>
        /// <param name="createNew">Whether the file must be newly created.</param>
        public FileManager(string path, bool createNew = false)
        {
            // note, this does implement the some of the same functionality another
            // constructor but I can't call it from here without some funky obfuscated
            // logic that may perform worse
            _createNew = createNew;
            // check if file path is absolute or relative, may not be optimal for windows
            if (System.IO.Path.IsPathRooted(path))
            {
                string directoryPath = System.IO.Path.GetDirectoryName(path);
                path = path.Remove(0, directoryPath.Length);
                DirectoryManager directory = new DirectoryManager(directoryPath);
                Path = System.IO.Path.Combine(directory.Path, path);
            }
            else
            {
                Path = System.IO.Path.Combine(Application.persistentDataPath, path);
            }
        }

        /// <summary>
        /// Create a file given a filename and directoryManager.
        /// </summary>
        /// <param name="directory">The directory in which the file must be created.</param>
        /// <param name="name">The name of the file.</param>
        /// <param name="createNew">Whether the file must be newly created.</param>
        public FileManager(DirectoryManager directory, string name, bool createNew = false)
        {
            _createNew = createNew;
            Path = System.IO.Path.Combine(directory.Path, name);
        }

        /// <summary>
        /// Create a file given a string folder and filename.
        /// </summary>
        /// <param name="folder">The folder in which the file should be made. If
        /// invalid defaults to the persistent data path.</param>
        /// <param name="name">The name of the file.</param>
        /// <param name="createNew">Whether the file must be newly created.</param>
        public FileManager(string folder, string name, bool createNew = false)
            : this(new DirectoryManager(folder), name, createNew) { }

        // OPTIONAL add more options based on the overloads for writer, use generics maybe?
        // OPTIONAL retry options?

        /// <summary>
        /// Write string into file and return its success.
        /// </summary>
        /// <param name="data">The string to be written to file.</param>
        /// <param name="append">False if to overwrite the file's contents,
        /// true to append <paramref name="data"/> to the end of the file.</param>
        /// <returns>True if the data was written sucessfully.</returns>
        public bool WriteToFile(string data, bool append = false)
        {
            FileMode mode = append ? FileMode.Append : FileMode.Truncate;

            try
            {
                using (FileStream stream = _file.Open(mode))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine(data);
                }
                return true;
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
            return false;
        }

        /// <summary>
        /// Write <paramref name="data"/> to file as bytes.
        /// </summary>
        /// <param name="data">THe byte data to write.</param>
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

        public async Task WriteToFileAsync(byte[] data)
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
                    await stream.WriteAsync(data, 0, data.Length);
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

        /// <summary>
        /// Reads the file and return the data as a string.
        /// </summary>
        /// <returns>Data read from the file. Returns an empty string if an
        /// error occurs.</returns>
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
                // if unity doesn't do it well ¯\_(ツ)_/¯
                Debug.LogError("Catastrophic error. Out of memory when trying to " +
                    $"read from { _file.FullName }.\n{ oome }");
                throw;
            }

            return data.ToString();
        }
    }
}
