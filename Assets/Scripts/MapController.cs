using UnityEngine;

public class MapController : MonoBehaviour
{
    void Start()
    {
        // Example: Switch to Satellite on Start
        SetMapType("satellite");
    }

    public void SetMapType(string mapType)
    {
        switch (mapType.ToLower())
        {
            case "satellite":
                OnlineMaps.instance.mapType = "google.satellite";
                break;
            case "terrain":
                OnlineMaps.instance.mapType = "google.terrain";
                break;
            case "roadmap":
                OnlineMaps.instance.mapType = "google.relief";
                break;            
            default:
                Debug.LogWarning("Unknown map type.");
                break;

        }

        // Refresh the map to apply changes
        OnlineMaps.instance.Redraw();
    }
}
