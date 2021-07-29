using System;
using System.IO;

using UnityEngine;

namespace ExternalUnityRendering.PathManagement
{
    /// <summary>
    /// Class that manages directory creation and validation.
    /// </summary>
    public class DirectoryManager
    {
        /// <summary>
        /// The path to the directory pointed by the <see cref="DirectoryManager"/>.
        /// </summary>
        private string _directory = Application.persistentDataPath;

        /// <summary>
        /// The path of the directory that this instance manages. It will not assign invalid values.
        /// Defaults to the application persistent data path.
        /// </summary>
        public string Path
        {
            get
            {
                return _directory;
            }
            set
            {
                SavePath(value, false);
            }
        }

        /// <summary>
        /// Validate and assign <paramref name="path"/> to <see cref="_directory"/> if valid.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="createNew"></param>
        private void SavePath(string path, bool createNew)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                string fullPath = System.IO.Path.GetFullPath(path);

                if (File.Exists(fullPath) || (Directory.Exists(fullPath) && createNew))
                {
                    int i = 1;
                    do
                    {
                        // rename as dir (1), dir (2) and so on and so forth
                        fullPath = $"{System.IO.Path.GetFullPath(path)} ({i++})";
                    } while (Directory.Exists(fullPath));
                }

                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                _directory = fullPath;
                return;
            }
            catch (ArgumentException ae)
            {
                Debug.LogError($"The directory \"{path}\" contains invalid characters.\n{ae}");
            }
            catch (System.Security.SecurityException se)
            {
                Debug.LogError($"You do not have the permissions to access \"{path}\".\n{se}");
            }
            catch (PathTooLongException ptle)
            {
                Debug.LogError($"The path \"{ path }\" is too long.\n{ptle}");
                // Retry with extended filename for windows
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                try
                {
                    string winPath = $@"\\?\{System.IO.Path.GetFullPath(path).Replace('/', '\\')}";

                    if (File.Exists(winPath) || (Directory.Exists(winPath) && createNew))
                    {
                        int i = 1;
                        do
                        {
                            // rename as dir (1), dir (2) and so on and so forth
                            winPath = $"{System.IO.Path.GetFullPath(path)} ({i++})";
                        } while (Directory.Exists(winPath));
                    }

                    if (!Directory.Exists(winPath))
                    {
                        Directory.CreateDirectory(winPath);
                    }
                    _directory = winPath;
                    Debug.Log($"Created folder using windows extended path syntax: {winPath}");
                    return;
                }
                // If failed in any way, just assign path as persistent data path.
                catch { }
#endif
            }
            catch (IOException ioe)
            {
                Debug.LogError($"The directory \"{ path }\" could not be created.\n{ioe}");
            }

            Debug.LogWarning($"Using the {nameof(Application.persistentDataPath)}: " +
                $"\"{Application.persistentDataPath}\"");
            _directory = Application.persistentDataPath;
        }

        /// <summary>
        /// Create a directory given a path.
        /// </summary>
        /// <param name="path">Path to the directory.</param>
        /// <param name="createNew">Whether the folder should be unique.</param>
        public DirectoryManager(string path, bool createNew = false)
        {
            SavePath(path, createNew);
        }

        // Empty, as _path is by default the datapath.
        /// <summary>
        /// Create a manager for the application persistent data path.
        /// </summary>
        public DirectoryManager() { }

        /// <summary>
        /// Create a subdirectory in <paramref name="directory"/> named
        /// <paramref name="directoryName"/>.
        /// </summary>
        /// <param name="directory">The main directory in which to create the subfolder.</param>
        /// <param name="directoryName">The name of the subdirectory.</param>
        /// <param name="createNew">Whether the folder should be unique.</param>
        public DirectoryManager(DirectoryManager directory, string directoryName,
            bool createNew = false)
            : this(System.IO.Path.Combine(directory.Path, directoryName), createNew) { }

        /// <summary>
        /// Create a subdirectory in <paramref name="parentDirectory"/> named
        /// <paramref name="directoryName"/>.
        /// </summary>
        /// <param name="parentDirectory">The main directory in which to create the subfolder.</param>
        /// <param name="directoryName">The name of the subdirectory.</param>
        /// <param name="createNew">Whether the folder should be unique.</param>
        public DirectoryManager(string parentDirectory, string directoryName,
            bool createNew = false)
            : this(System.IO.Path.Combine(parentDirectory, directoryName), createNew) { }
    }
}
