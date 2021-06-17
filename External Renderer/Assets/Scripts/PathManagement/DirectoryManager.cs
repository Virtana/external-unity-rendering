using System;
using System.IO;
using UnityEngine;

namespace ExternalUnityRendering.PathManagement
{
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
                        + $"characters.\n{ ae.ToString() }");
                }
                catch (System.Security.SecurityException se)
                {
                    Debug.LogError("You do not have the permissions to access "
                        + $"<{ value }>.\n{ se.ToString() }");
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
                finally
                {
                    if (_directory.FullName == Application.persistentDataPath)
                    {
                        Debug.LogWarning(
                            $"Defaulting to { Application.persistentDataPath}.");
                    }
                }
            }
        }

        public DirectoryManager(string path)
        {
            Path = path;
        }

        public DirectoryManager()
            : this(Application.persistentDataPath) { }
    }
}
