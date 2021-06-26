﻿using System;
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
        /// Internal reference to the file that this object manages.
        /// </summary>
        private DirectoryInfo _directory;

        /// <summary>
        /// Whether this directory must be unique. Will automatically rename if necessary.
        /// </summary>
        private readonly bool _createNewDirectory = false;

        /// <summary>
        /// The path of the directory that this instance manages. It will not
        /// assign invalid values. Defaults to the application persistent data path.
        /// </summary>
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
                    } else if (_createNewDirectory)
                    {
                        int i = 1;
                        do
                        {
                            // rename as dir (1), dir (2) and so on and so forth
                            dir = new DirectoryInfo($"{ value } ({ i++ })");
                        } while (dir.Exists);
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

                if ((_directory == null) && value == Application.dataPath)
                {
                    // If this failed, big problem, but that is a unity problem
                    Debug.LogError("Failed to reference persistent data path!");
                }
                else if (_directory == null)
                {
                    // if directory failed to be assigned, then try a new one.
                    _directory = new DirectoryInfo(Application.persistentDataPath);
                    Debug.LogWarning(
                        $"Defaulting to { Application.persistentDataPath}.");
                }
            }
        }

        /// <summary>
        /// Create a directory given a path.
        /// </summary>
        /// <param name="path">Path to the directory.</param>
        /// <param name="createNew">Whether the folder should be unique.</param>
        public DirectoryManager(string path, bool createNew = false)
        {
            _createNewDirectory = createNew;
            Path = path;
        }

        /// <summary>
        /// Create a manager for the application persistent data path
        /// </summary>
        public DirectoryManager()
            : this(Application.persistentDataPath) { }

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
    }
}
