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
                    Debug.LogError("Cannot set file path to null.\n"
                        + ane.ToString());
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
                    Debug.LogError($"Access to <{ value }> is denied.\n { uae }");
                }
                catch (PathTooLongException ptle)
                {
                    // TODO: Implement retry with extended filename for windows
                    Debug.LogError($"The path <{ value }> is too long.\n"
                        + ptle.ToString());
                }
                catch (NotSupportedException nse)
                {
                    Debug.LogError($"The path <{ value }> contains a colon.\n"
                        + nse.ToString());
                }
                catch (IOException ioe)
                {
                    Debug.LogError($"The file <{ value }> could not be created.\n"
                        + ioe.ToString());
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
            // for Windows implementation in .net 5
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

        // TODO PROPERLY IMPLEMENT THIS INCOMPLETE DEMO FUNCTION
        // OPTIONAL add generic serialization options
        public void WriteToFile(string data, bool append = true)
        {
            FileMode mode = append ? FileMode.Append : FileMode.Truncate;
            using (FileStream stream = _file.Open(mode))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine(data);
            }
        }

        public void WriteToFile(byte[] data)
        {
            using (FileStream stream = _file.OpenWrite())
            {
                stream.Write(data, 0, data.Length);
            }
        }

        public string ReadFile()
        {
            StringBuilder data = new StringBuilder();
            using (FileStream stream = _file.OpenRead())
            using (StreamReader reader = new StreamReader(stream))
            {
                data.Append(reader.ReadToEnd());
            }

            return data.ToString();
        }
    }
}
