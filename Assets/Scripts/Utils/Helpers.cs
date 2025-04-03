using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PicassoAR.Utils
{
    public class Helpers 
    {
        /// <summary>
        /// Finds a child component of type T with a name that contains the specified string.
        /// </summary>
        /// <typeparam name="T">The type of the component to find. Must inherit from Component.</typeparam>
        /// <param name="gameObject">The GameObject to search within.</param>
        /// <param name="name">The name or part of the name to search for.</param>
        /// <returns>The component of type T with the matching name, or null if not found.</returns>
        public static T GetChildComponentByName<T>(GameObject gameObject, string name) where T : Component
        {
            if (gameObject == null)
            {
                Debug.LogError("GameObject is null.");
                return null;
            }

            // Get all components of type T in the children of the GameObject
            T[] components = gameObject.GetComponentsInChildren<T>();

            // Find the first component whose name contains the specified string
            T foundComponent = components.ToList().Find(x => x.name.Contains(name));

            if (foundComponent == null)
            {
                Debug.LogError($"No component of type {typeof(T).Name} with name containing '{name}' found.");
            }

            return foundComponent;
        }

        public static void UpdateAndApplyImageTexture(ref RawImage renderer, Texture2D texture)
        {
            // update the image rendered on the screen once the image texture updates
            renderer.material.mainTexture = texture;
            renderer.texture = texture;
            texture.Apply();
            Debug.Log("applied texture update");
        }


        public class Ref<T>
        {
            private T backing;
            public T Value {get{return backing;}}
            public Ref(T reference)
            {
                backing = reference;
            }
        }
    }
}