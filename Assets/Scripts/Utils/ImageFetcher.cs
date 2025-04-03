using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Threading.Tasks;

namespace PicassoAR.Utils {
    public class ImageFetcher : MonoBehaviour
    {
        [Header("Server Configuration")]
        [SerializeField, Tooltip("Link to fetch the image from")]
        public string serverUrl = "http://192.168.1.100:8080/images/example.jpg"; // TODO: replace with the actual server url
        public RawImage displayImage; 

        /// <summary>
        /// Fetches an image from the server and returns it as a Texture2D.
        /// </summary>
        /// <returns>Texture2D if successful, or null on failure.</returns>
        public async Task<Texture2D> FetchImageTexture()
        {
            Debug.Log($"Fetching image from {serverUrl}");

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(serverUrl))
            {
                 // Send the request asynchronously
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield(); // Wait for the request to complete
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error fetching image: {request.error}");
                    return null;
                }

                // Get and return the texture from the response
                return DownloadHandlerTexture.GetContent(request);
            }
        }
    }
}
