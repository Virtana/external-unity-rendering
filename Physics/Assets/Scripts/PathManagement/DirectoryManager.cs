using System;
using System.IO;
using UnityEngine;

namespace ExternalUnityRendering.PathManagement
{
    // TODO see notes in FileManager.cs. Consider same for this.
    class DirectoryManager
    {
        private DirectoryInfo _directory;

        public DirectoryInfo Directory
        {
            get
            {
                _directory.Refresh();
                return _directory;
            }
        }

        public string Path
        {
            get
            {
                return _directory.FullName;
            }
            set
            {
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(value);
                    if (!dir.Exists)
                    {
                        dir.Create();
                    }
                    _directory = dir;
                }
                catch (ArgumentNullException ane)
                {
                    Debug.LogError("Cannot set directory path to null.\n"
                        + ane.ToString());
                }
                catch (ArgumentException ae)
                {
                    Debug.LogError($"The directory <{ value }> contains invalid "
                        + $"characters.\n{ ae }");
                }
                catch (System.Security.SecurityException se)
                {
                    Debug.LogError("You do not have the permissions to access "
                        + $"<{ value }>.\n{ se }");
                }
                catch (PathTooLongException ptle)
                {
                    // TODO: Implement retry with extended filename for windows
                    Debug.LogError($"The path <{ value }> is too long.\n"
                        + ptle.ToString());
                }
                catch (IOException ioe)
                {
                    Debug.LogError($"The directory <{ value }> could not be created.\n"
                        + ioe.ToString());
                }
                
                // if directory failed to be assigned, then try a new one.
                if (_directory == null)
                {
                    // NOTE if this failed before, e.g. on parameterless cpnstructor
                    // then its just trying again, but that would be a larger unity
                    // issue
                    _directory = new DirectoryInfo(Application.persistentDataPath);
                    Debug.LogWarning(
                        $"Defaulting to { Application.persistentDataPath}.");
                }
            }
        }

        public DirectoryManager(string path)
        {
            Path = path;
        }

        public DirectoryManager()
            : this(Application.persistentDataPath) { }
            
        public DirectoryManager(DirectoryManager directory, string directoryName)
            : this(System.IO.Path.Combine(directory.Path, directoryName)) { }
    }
}
