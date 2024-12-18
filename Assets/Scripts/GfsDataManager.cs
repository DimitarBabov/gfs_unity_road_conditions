using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;
using TMPro;
using static UnityEngine.EventSystems.EventTrigger;
/// <summary>
/// Downloads requested pngs and assigns them to dictionaries
/// </summary>
public class GfsDataManager : MonoBehaviour
{
    // Public variables
    public List<string> parameters = new List<string> { "HGT-500_mb", "TMP-surface" };  // Example parameters with level included
    public float minLat = 20f;  // Min latitude
    public float maxLat = 55f;  // Max latitude
    public float minLon = 230f;  // Min longitude
    public float maxLon = 300f;  // Max longitude

    [SerializeField]
    public List<TextureEntry> texturesByParamList = new List<TextureEntry>();  // List to hold textures for inspector view

    [SerializeField]
    public List<TextureInfoEntry> texturesInfoByParamList = new List<TextureInfoEntry>();  // List to hold textures info for inspector view

    public Dictionary<string, List<Texture2D>> texturesByParam = new Dictionary<string, List<Texture2D>>();  // Dictionary to hold textures by parameter
    public Dictionary<string, List<string>> textureInfoByParam = new Dictionary<string, List<string>>();  // Dictionary to hold meta content for each texture by parameter

    public TextMeshProUGUI metaTextDisplay;

    public string serverUrl = "http://wms.lhr.rocks/get_png_meta_links";  // The server URL
    private bool isInitialized = false;

    // Singleton instance
    public static GfsDataManager Instance { get; private set; }
    public bool IsInitialized() { return isInitialized; }
    private void Awake()
    {
        // Ensure there's only one instance of XRpngContainer
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    IEnumerator Start()
    {

        metaTextDisplay.text = " ";

        if (texturesByParamList != null && texturesByParamList.Count() > 0)
        {
            metaTextDisplay.text = "Loading GFS data ...";
            // Check if textures are already loaded in the inspector
            foreach (TextureEntry entry in texturesByParamList)
            {
                if (entry.textures != null && entry.textures.Count > 0)
                {
                    string paramKey = entry.param;
                    if (!string.IsNullOrEmpty(paramKey))
                    {
                        texturesByParam[paramKey] = entry.textures;
                        ExtractTextureInfoFromName(paramKey);                        
                    }
                }
            }

            isInitialized = true;
        }
        else
        {
            isInitialized = false;
            metaTextDisplay.text = "Loading GFS data ...";
            yield return StartCoroutine(InitializeTextures());
            isInitialized = true;
            Debug.Log("is init = TRUE");
        }
    }

    private IEnumerator InitializeTextures()
    {
        foreach (string paramLevel in parameters)
        {
            if (!texturesByParam.ContainsKey(paramLevel))
            {
                string[] paramLevelSplit = paramLevel.Split('-');
                string param = paramLevelSplit[0];
                string level = paramLevelSplit[1];
                // Start the process to request PNGs and download textures for each parameter
                yield return StartCoroutine(RequestPngsAndDownloadTextures(param, level));
            }
        }
    }

    private void ExtractTextureInfoFromName(string paramLevel)
    {
        if (!texturesByParam.ContainsKey(paramLevel)) return;

        List<string> textureInfoList = new List<string>();

        foreach (Texture2D texture in texturesByParam[paramLevel])
        {
            if (texture != null)
            {
                string name = texture.name;
                string metaInfo = GenerateTextureInfoFromName(name);
                textureInfoList.Add(metaInfo);
                Debug.Log($"Texture info extracted for {name}: {metaInfo}");
            }
            else
            {
                Debug.LogWarning($"Texture is null for parameter: {paramLevel}");
            }
        }

        textureInfoByParam[paramLevel] = textureInfoList;
    }

    private string GenerateTextureInfoFromName(string textureName)
    {
        // Log the texture name to help diagnose the issue
        Debug.Log($"Parsing texture name: {textureName}");

        // Find the position of "gfs" to correctly split the param and level
        int gfsIndex = textureName.IndexOf("_gfs");
        if (gfsIndex == -1)
        {
            return "Invalid texture name format: 'gfs' not found.";
        }

        // Extract the parameter (before the first "_") and level (between first "_" and "_gfs")
        string param = textureName.Substring(0, textureName.IndexOf('_'));  // TMP, HGT, etc.
        string level = textureName.Substring(textureName.IndexOf('_') + 1, gfsIndex - textureName.IndexOf('_') - 1);  // Everything between param and gfs

        // Split the remaining part (after "_gfs") to get date, cycle, and hour
        string[] remainingParts = textureName.Substring(gfsIndex + 5).Split('_');

        if (remainingParts.Length < 3)
        {
            return "Invalid texture name format after 'gfs'.";
        }

        // Extract the date, cycle, and hour
        string date = remainingParts[0];  // Expected format is 20241008 (YYYYMMDD)
        string cycle = remainingParts[1]; // Expected format is 12 (HH)
        string hour = remainingParts[2].Substring(1); // f096 -> 096

        // Log extracted values for date, cycle, and hour for debugging
        Debug.Log($"Extracted date: {date}, cycle: {cycle}, hour: {hour}");

        // Validate that the date string is 8 characters long
        if (date.Length != 8)
        {
            return $"Invalid date format in texture name: {textureName}";
        }

        // Validate that the cycle and hour are not empty and have the expected length
        if (cycle.Length != 2 || hour.Length != 3)
        {
            return $"Invalid cycle or hour format in texture name: {textureName}";
        }

        // Format the textureInfo string
        return $"Parameter: {param}\nLevel: {level}\nDate: {date.Substring(0, 4)}-{date.Substring(4, 2)}-{date.Substring(6, 2)}\nCycle: {cycle}\nHour: {hour}";
    }

    private IEnumerator RequestPngsAndDownloadTextures(string param, string level)
    {
        // Construct the request URL with parameters
        string requestUrl = $"{serverUrl}?param={param}&level={level}";
        Debug.Log(requestUrl);

        // Send the request to the Flask server to generate PNGs and meta files
        using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching PNGs: {webRequest.error}");
                yield break;
            }
            //Debug.Log("XXXX "+webRequest.result);
            // Parse the JSON response
            PngMetaResponse response = JsonUtility.FromJson<PngMetaResponse>(webRequest.downloadHandler.text);
            Debug.Log("XXXX " + webRequest.downloadHandler.text);
            if (response.results.Length == 0)
            {
                Debug.LogError("No matching PNGs found.");
                yield break;
            }

            // Sort the textures by forecast hour (f0xx)
            List<TextureMetaPair> textureMetaPairs = new List<TextureMetaPair>();

            for (int i = 0; i < response.results.Length; i++)
            {
                string pngUrl = response.results[i].png_download_url;

                // Extract the forecast hour from the filename (e.g., "f024" from "HGT_500_mb_gfs_20240926_06_f024")
                string forecastHour = ExtractForecastHour(pngUrl);

                // Download the PNG texture
                TextureMetaPair pair = new TextureMetaPair
                {
                    forecastHour = forecastHour,
                    pngUrl = pngUrl
                };

                yield return StartCoroutine(DownloadTexture(pair));

                // Manually set the texture name using the file name from the URL
                pair.texture.name = System.IO.Path.GetFileNameWithoutExtension(pngUrl);

                // Extract texture info from the texture name instead of downloading the meta file
                yield return StartCoroutine(DownloadMetaFile(pair));

                textureMetaPairs.Add(pair);
            }

            // Sort the pairs based on the forecast hour
            textureMetaPairs = textureMetaPairs.OrderBy(pair => int.Parse(pair.forecastHour.Substring(1))).ToList();

            // Now populate the sorted textures and textureInfo arrays for the specific parameter
            List<Texture2D> texturesList = new List<Texture2D>();
            List<string> textureInfoList = new List<string>();

            foreach (TextureMetaPair pair in textureMetaPairs)
            {
                texturesList.Add(pair.texture);
                textureInfoList.Add(pair.metaContent);
            }

            texturesByParam[$"{param}-{level}"] = texturesList;
            textureInfoByParam[$"{param}-{level}"] = textureInfoList;
           // texturesInfoByParamList.Add(new TextureEntry { param = $"{param}-{level}", textures = new List<Texture2D>() });
           // texturesInfoByParamList.Add(new TextureInfoEntry { param = $"{param}-{level}", texturesInfo = new List<string>() });

            // Add to inspector-viewable list
            texturesByParamList.Add(new TextureEntry { param = $"{param}-{level}", textures = texturesList });
            texturesInfoByParamList.Add(new TextureInfoEntry { param = $"{param}-{level}", texturesInfo = textureInfoList });

            Debug.Log($"Textures for parameter {param}-{level} successfully downloaded and sorted by forecast hour.");
            metaTextDisplay.text = $"Loading {param}-{level} GFS data completed!";
        }
        
        
    }

    private IEnumerator DownloadTexture(TextureMetaPair pair)
    {
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(pair.pngUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error downloading PNG: {webRequest.error}");
                yield break;
            }
           
            pair.texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
           
            Debug.Log($"Texture for {pair.forecastHour} downloaded.");
        }
    }

    // Extract forecast hour (e.g., "f024") from the URL or filename
    private string ExtractForecastHour(string url)
    {
        string filename = System.IO.Path.GetFileNameWithoutExtension(url);
        string[] parts = filename.Split('_');
        foreach (string part in parts)
        {
            if (part.StartsWith("f") && part.Length == 4)
            {
                return part;  // Return the part that starts with "f" (e.g., "f024")
            }
        }
        return "f000";  // Default to "f000" if not found
    }

    private IEnumerator DownloadMetaFile(TextureMetaPair pair)
    {
        string metaUrl = pair.pngUrl.Replace(".png", ".info");
        using (UnityWebRequest webRequest = UnityWebRequest.Get(metaUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error downloading meta file: {webRequest.error}");
                yield break;
            }

            pair.metaContent = webRequest.downloadHandler.text;
            Debug.Log($"Meta file for {pair.forecastHour} downloaded: {pair.metaContent}");
            
        }
    }
    // Helper class to store texture and meta data along with the forecast hour
    private class TextureMetaPair
    {
        public string forecastHour;  // Forecast hour (e.g., "f024")
        public string pngUrl;  // URL to the PNG file
        public Texture2D texture;  // The downloaded texture
        public string metaContent;  // The extracted meta content based on the texture name
    }

    // Class to deserialize the JSON response from the server
    [System.Serializable]
    public class PngMetaResponse
    {
        public Result[] results;
    }

    [System.Serializable]
    public class Result
    {
        public string png_download_url;
        public string meta_download_url;
    }

    [System.Serializable]
    public class TextureEntry
    {
        public string param;
        public List<Texture2D> textures;
    }
    [System.Serializable]
    public class TextureInfoEntry
    {
        public string param;
        public List<string> texturesInfo;
    }
}
