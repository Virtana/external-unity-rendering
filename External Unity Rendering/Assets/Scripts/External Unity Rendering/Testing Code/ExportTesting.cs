using System.Collections;
using System.Linq;

using UnityEngine;

namespace ExternalUnityRendering.TestingCode
{
    class ExportTesting : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(ExportLoop());
        }

        IEnumerator ExportLoop()
        {
            Rigidbody[] rigidbodies =
                FindObjectsOfType<Collider>()
                .Where((collider) =>
                {
                    return collider.gameObject.activeInHierarchy
                    && !collider.gameObject.TryGetComponent(out MeshCollider _);
                })
                .Select((collider) =>
                {
                    if (!collider.gameObject.TryGetComponent(out Rigidbody rb))
                    {
                        rb = collider.gameObject.AddComponent<Rigidbody>();
                    }

                    rb.mass = 10;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    return rb;
                })
                .ToArray();

            while (Application.isPlaying)
            {
                foreach (Rigidbody rb in rigidbodies)
                {
                    rb.AddForceAtPosition(
                        new Vector3(
                            Random.Range(-20, 20),
                            Random.Range(-20, 20),
                            Random.Range(-20, 20)),
                        rb.ClosestPointOnBounds(
                            new Vector3(
                                Random.Range(-20, 20),
                                Random.Range(-20, 20),
                                Random.Range(-20, 20))),
                        ForceMode.Impulse);
                }

                yield return new WaitForSeconds(0.5f);
            }

        }
    }
}
