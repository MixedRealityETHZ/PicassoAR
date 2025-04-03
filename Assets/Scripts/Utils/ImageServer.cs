using System.Collections;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace PicassoAR.Utils
{
    public class ImageServer : MonoBehaviour
    {
        [Header("Server Configuration")]
        [SerializeField, Tooltip("Link to fetch the image from")]
        public string serverUrl = "https://127.0.0.1:8080/"; // replace with the actual server url
        private string listenPath = "comm_usr";
        private string sendPath = "comm_ml2";
        
        public IEnumerator SendImageToServer(Texture2D texture, string imageName = "image.png")
        {   
            // wait until the last image has been sent
            // TODO: define a set of types that can be sent
            byte[] imageData = texture.EncodeToPNG();
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", imageData, imageName, "image/png");

            Debug.Log("sending data to: " + serverUrl + sendPath);
            UnityWebRequest request = UnityWebRequest.Post(serverUrl + sendPath, form);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {   
                Debug.Log("Send image success!");
            }
            else
            {
                // Debug.LogError($"Error while sending the image to the server: {request.error}");
            }
        }


        public IEnumerator FetchProcessedImage(string imgName, System.Action<Texture2D> onComplete)
        {
            // called immediately after sending the image to the server
            string url = $"{serverUrl}{listenPath}?img={UnityWebRequest.EscapeURL(imgName)}";
            Debug.Log("url " + url);
            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse the gallery JSON (SimpleJSON or System.Text.Json)
                var galleryData = request.downloadHandler.text; 
                Debug.Log("Got response: " + galleryData);

                byte[] processedData = request.downloadHandler.data;
                Texture2D processedTexture = new Texture2D(2, 2); // will be replaced by the actual image texture afterwardss
                processedTexture.LoadImage(processedData);
                onComplete?.Invoke(processedTexture);
            }
            else
            {
                Debug.LogError($"Error fetching gallery: {request.error}");
                yield return null;
            }
        }


        public IEnumerator FetchGallery(string pathType)
        {
            string url = $"https://localhost:8080/images/{pathType}/gallery";
            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse the gallery JSON (you can use SimpleJSON or System.Text.Json)
                var galleryData = request.downloadHandler.text; 
            }
            else
            {
                Debug.LogError($"Error fetching gallery: {request.error}");
            }
        }
    }

}