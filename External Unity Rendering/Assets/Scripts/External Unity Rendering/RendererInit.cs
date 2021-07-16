using UnityEngine;

namespace ExternalUnityRendering
{
    class RendererInit : MonoBehaviour
    {
#if RENDERER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
#endif
        private static void Initialize()
        {
            if (FindObjectOfType<ImportScene>() == null)
            {
                GameObject obj = new GameObject
                {
                    name = "Scene State Importer"
                };

                obj.AddComponent<ImportScene>();
            }
        }
    }
}
