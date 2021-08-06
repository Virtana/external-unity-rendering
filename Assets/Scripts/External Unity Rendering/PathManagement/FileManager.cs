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
        private string _filePath = null;

        /// <summary>
        /// The path of the file that this instance manages. It will not assign invalid paths.
        /// </summary>
        public string Path
        {
            get
            {
                return _filePath;
            }
            set
            {
                SavePath(value, false);
            }
        }

        private void SavePath(string path, bool createNew)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                string fullPath = System.IO.Path.GetFullPath(path);
                string dirName = System.IO.Path.GetDirectoryName(fullPath);

                if (Directory.Exists(fullPath) || (File.Exists(fullPath) && createNew))
                {
                    int i = 1;
                    string nameWithoutExtension =
                        System.IO.Path.GetFileNameWithoutExtension(fullPath);
                    string extension = System.IO.Path.GetExtension(fullPath);

                    do
                    {
                        // rename as file (1).ext, file (2).ext and so on and so forth
                        fullPath =
                            System.IO.Path.GetFullPath(System.IO.Path.Combine(dirName,
                                $"{nameWithoutExtension} ({i++}){extension}"));
                    } while (File.Exists(fullPath));
                }

                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }

                if (!File.Exists(fullPath))
                {
                    File.Create(fullPath).Close();
                }

                _filePath = fullPath;
            }
            catch (ArgumentException ae)
            {
                Debug.LogError($"The file path \"{path}\" is empty or contains invalid " +
                    $"characters.\n{ae}");
            }
            catch (System.Security.SecurityException se)
            {
                Debug.LogError("You do not have the permissions to access "
                    + $"\"{ path }\".\n{ se }");
            }
            catch (UnauthorizedAccessException uae)
            {
                Debug.LogError($"Access to \"{ path }\" is denied.\n{ uae }");
            }
            catch (PathTooLongException ptle)
            {
                Debug.LogError($"The path \"{ path }\" is too long.\n{ ptle }");
                // Retry with extended filename for windows
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                try
                {
                    string winPath = $@"\\?\{System.IO.Path.GetFullPath(path).Replace('/', '\\')}";
                    System.IO.Path.GetFullPath(winPath);
                    string dirName = System.IO.Path.GetDirectoryName(winPath);
                    if (Directory.Exists(winPath) || (File.Exists(winPath) && createNew))
                    {
                        int i = 1;
                        string nameWithoutExtension =
                            System.IO.Path.GetFileNameWithoutExtension(winPath);
                        string extension = System.IO.Path.GetExtension(winPath);

                        do
                        {
                            // rename as file (1).ext, file (2).ext and so on and so forth
                            winPath =
                                System.IO.Path.GetFullPath(System.IO.Path.Combine(dirName,
                                    $"{ nameWithoutExtension} ({i++}){extension}"));
                        } while (File.Exists(winPath));
                    }

                    if (!Directory.Exists(dirName))
                    {
                        Directory.CreateDirectory(dirName);
                    }

                    if (!File.Exists(winPath))
                    {
                        File.Create(winPath).Close();
                    }

                    _filePath = winPath;
                    Debug.Log($"Created folder using windows extended path syntax: {winPath}");
                    return;
                }
                // If failed in any way, do nothing.
                catch { }
#endif
            }
            catch (NotSupportedException nse)
            {
                Debug.LogError($"The path \"{path}\" contains a colon.\n{nse}");
            }
            catch (IOException ioe)
            {
                Debug.LogError($"The file \"{path}\" could not be created.\n{ioe}");
            }
        }

        /// <summary>
        /// Generate a file that should be unique. To be used as a fallback for files that must exist.
        /// </summary>
        public FileManager()
            : this(System.IO.Path.Combine(Application.persistentDataPath,
                System.IO.Path.GetRandomFileName()), false) { }

        /// <summary>
        /// Creates a file given a path as a string.
        /// </summary>
        /// <param name="path">The path of the file to be created.</param>
        /// <param name="createNew">Whether the file must be newly created.</param>
        public FileManager(string path, bool createNew = false)
        {
            SavePath(path, createNew);
        }

        /// <summary>
        /// Create a file given a filename and directoryManager.
        /// </summary>
        /// <param name="directory">The directory in which the file must be created.</param>
        /// <param name="name">The name of the file.</param>
        /// <param name="createNew">Whether the file must be newly created.</param>
        public FileManager(DirectoryManager directory, string name, bool createNew = false)
            : this(System.IO.Path.Combine(directory.Path, name), createNew) { }

        /// <summary>
        /// Create a file given a string folder and filename.
        /// </summary>
        /// <param name="folder">The folder in which the file should be made. If
        /// invalid defaults to the persistent data path.</param>
        /// <param name="name">The name of the file.</param>
        /// <param name="createNew">Whether the file must be newly created.</param>
        public FileManager(string folder, string name, bool createNew = false)
            : this(new DirectoryManager(folder), name, createNew) { }

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
                using (FileStream stream = File.Open(_filePath, mode, FileAccess.Write,
                    FileShare.Read))
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
        /// Write string into file and return its success.
        /// </summary>
        /// <param name="data">The string to be written to file.</param>
        /// <param name="append">False if to overwrite the file's contents,
        /// true to append <paramref name="data"/> to the end of the file.</param>
        /// <returns>True if the data was written sucessfully.</returns>
        public async Task<bool> WriteToFileAsync(string data, bool append = false)
        {
            FileMode mode = append ? FileMode.Append : FileMode.Truncate;

            try
            {
                using (FileStream stream = File.Open(_filePath, mode, FileAccess.Write,
                    FileShare.Read))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(data);
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
                using (FileStream stream = File.OpenWrite(_filePath))
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

        /// <summary>
        /// Write <paramref name="data"/> to file as bytes.
        /// </summary>
        /// <param name="data">THe byte data to write.</param>
        public async void WriteToFileAsync(byte[] data)
        {
            if (data == null)
            {
                Debug.LogError("Cannot write a null array to disk.");
                return;
            }

            try
            {
                using (FileStream stream = File.OpenWrite(_filePath))
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
                using (FileStream stream = File.OpenRead(_filePath))
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

            return data.ToString();
        }

        /// <summary>
        /// Asynchronously reads from the file and returns the data as a string.
        /// </summary>
        /// <returns>A <see cref="Task"/> that resolves when the data has been read. </returns>
        public async Task<string> ReadFileAsync()
        {
            StringBuilder data = new StringBuilder();
            try
            {
                using (FileStream stream = File.OpenRead(_filePath))
                using (StreamReader reader = new StreamReader(stream))
                {
                    data.Append(await reader.ReadToEndAsync());
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

            return data.ToString();
        }
    }
}
