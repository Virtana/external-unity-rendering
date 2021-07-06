#define PHYSICS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExternalUnityRendering;
using System;


public class Init : MonoBehaviour
{
    // TODO Initialize list
    // Check if exporter/importer exists
    // add if not
    // if importer just exist
    // if exporter then search commandline options for timing
    // then quit
    // may need to add a way to check for physics terminating then
    // check for importer being done then exit (throw signal or something??)

#if PHYSICS
    private class Time
    {
        public long Milliseconds { get; private set; }

        public Time(string time)
        {
            if (time.Length < 2)
            {
                Debug.LogError("Invalid Time Input");
                Application.Quit();
            }

            // get last character
            char timeModifier = time[time.Length - 1];

            if (char.IsDigit(timeModifier))
            {
                timeModifier = ' ';
            } else
            {
                time = time.Remove(time.Length - 1);
            }

            switch (timeModifier)
            {
                case ' ':
                    Milliseconds = 1;
                    break;
                case 's':
                    Milliseconds = 1000;
                    break;
                case 'm':
                    Milliseconds = 60 * 1000;
                    break;
            }

            if (!long.TryParse(time, out long number))
            {
                Debug.LogError("Failed to parse time.");
                Application.Quit(1);
            }
            else
            {
                Milliseconds *= number;
            }
        }
    }
#endif

    [RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
#if PHYSICS
        ExportScene exporter = FindObjectOfType<ExportScene>();
        if (exporter == null)
        {
            GameObject obj = new GameObject
            {
                name = System.Guid.NewGuid().ToString()
            };

            exporter = obj.AddComponent<ExportScene>();
        }


#elif RENDERER

#else
        Debug.LogError("Renderer or Physics is not defined. Will not do anything. Exiting...");
        Application.Quit(1);
#endif
    }
}
