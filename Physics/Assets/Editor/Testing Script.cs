using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SceneStateExporter;
using UnityEditor;

public class TesterGUI
{
    public static int Runs = 10;
    public static float Radius = 30;
    public static float Power = 100;
    
    [MenuItem("Exporter Testing/Explode")]
    public static void AddRigidBodiesAndExplode()
    {
        GameObject obj = new GameObject();
        ExportScene export = obj.AddComponent<ExportScene>();

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

        export.StartCoroutine(ExportContinuously(Runs));
    }

    private static IEnumerator ExportContinuously(int runs)
    {
        ExportScene export = Object.FindObjectOfType<ExportScene>();
        CustomCamera cam = Object.FindObjectOfType<CustomCamera>();

        if (export == null)
        {
            Debug.LogError("Missing ExportScene Component.");
            yield break;
        }
        else if (cam == null)
        {
            Debug.LogError("Missing CustomCamera.");
            yield break;
        }


        for (int i = 0; i < runs; i++)
        {
            export.ExportPath = string.Format(@"D:\Virtana\Planning\obj ({0}).json", i+1);
            export.ExportCurrentScene();
            cam.RenderImage(@"D:\Virtana\Planning", new Vector2Int(1920, 1080));
            yield return new WaitForSecondsRealtime(1f);
        }
    }
}