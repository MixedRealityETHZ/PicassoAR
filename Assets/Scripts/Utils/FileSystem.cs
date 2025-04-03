using System.IO;
using UnityEngine;

namespace PicassoAR.Utils {
    public class FileSystemUtils
    {
        public static Texture2D LoadImageFromPath(string path)
    {
        // Check if the file exists
        if (!File.Exists(path))
        {
            Debug.LogError($"File not found at path: {path}");
            return null;
        }

        try
        {
            // Read all bytes from the file
            byte[] fileData = File.ReadAllBytes(path);
            Debug.Log("read bytes successful");

            // Create a new Texture2D object
            Texture2D texture = new Texture2D(2, 2); // Initial size, will be resized automatically?
            if (texture.LoadImage(fileData))
            {
                Debug.Log($"Successfully loaded image from path: {path}");
                return texture;
            }
            else
            {
                Debug.LogError("Failed to load image into Texture2D.");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"An error occurred while loading the image: {e.Message}");
            return null;
        }
    }
    }
}