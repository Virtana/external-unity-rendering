using ExternalUnityRendering.CameraUtilites;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace ExternalUnityRendering.UnityEditor
{
    // NOTE rework the functionality of this.
    // also add more options
    // TODO add sub menus for write to file, transmit etc and some way to change RUNs
    public class TesterGUI
    {
        public static int Runs = 10;
        public static float Radius = 30;
        public static float Power = 100;

        [MenuItem("Exporter Testing/Explode")]
        public static void AddRigidBodiesAndExplode()
        {
            GameObject exporter = new GameObject
            {
                name = "Exporter Object"
            };
            ExportScene export = exporter.AddComponent<ExportScene>();

            if (export == null)
            {
                Debug.LogError("Missing exportscene Component.");
                return;
            }

            Vector3 explosionPos = Vector3.zero;
            Collider[] colliders = Physics.OverlapSphere(explosionPos, Radius);
            foreach (Collider hit in colliders)
            {
                if (hit.gameObject.name == "Plane" ||
                    hit.gameObject.name.StartsWith("Quad"))
                {
                    continue;
                }
                Rigidbody rb = hit.gameObject.GetComponent<Rigidbody>();

                if (rb == null)
                {
                    rb = hit.gameObject.AddComponent<Rigidbody>();
                }

                rb.drag = 0f;
                rb.mass = 10;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                rb.AddExplosionForce(Power, explosionPos, Radius, 30.0F);
                rb.AddForce(new Vector3(
                        UnityEngine.Random.Range(-20, 20),
                        UnityEngine.Random.Range(-20, 20),
                        UnityEngine.Random.Range(-20, 20)),
                    ForceMode.Impulse);
            }

            // HACK using export to run coroutine
            export.StartCoroutine(ExportContinuously(Runs));
        }

        // 
        private static IEnumerator ExportContinuously(int runs)
        {
            ExportScene export = Object.FindObjectOfType<ExportScene>();

            Camera[] cameras = Object.FindObjectsOfType<Camera>();

            if (cameras.Length == 0)
            {
                // If cam is empty, then no cameras were found.
                Debug.LogError("Missing Camera! Importer cannot render from this.");
                yield break;
            }

            foreach (Camera camera in cameras)
            {
                // add this behaviour
                if (camera.gameObject.GetComponent<CustomCamera>() == null)
                {
                    camera.gameObject.AddComponent<CustomCamera>();
                }
            }

            // TODO add multicam support
            CustomCamera[] customCameras = Object.FindObjectsOfType<CustomCamera>();

            if (export == null)
            {
                Debug.LogError("Missing ExportScene Component.");
                yield break;
            }

            // TODO add a way to select export folder before the loop
            export.ExportFolder = @"D:\Virtana\Planning";
            foreach (CustomCamera cam in customCameras)
            {
                cam.RenderPath = @"D:\Virtana\Planning";
            }

            // TODO Ensure folders are set appropriately
            for (int i = 0; i < runs; i++)
            {
                Debug.Log(export.ExportFolder);
                export.ExportCurrentScene(ExportScene.ExportType.WriteToFile);
                foreach (CustomCamera cam in customCameras)
                {
                    cam.RenderImage(new Vector2Int(1920, 1080));
                }
                yield return new WaitForSecondsRealtime(1f);
            }
        }
    }
}
