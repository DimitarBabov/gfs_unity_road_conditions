using UnityEngine;

public class MapController : MonoBehaviour
{
    void Start()
    {
        // Set default map type at start
        SetMapType("terrain");

        // Subscribe to map events
        OnlineMaps.instance.OnChangePosition += OnMapChanged;
        OnlineMaps.instance.OnChangeZoom += OnMapChanged;

        // Apply constraints once at the start
        ApplyConstraints();
    }

    private void OnMapChanged()
    {
        ApplyConstraints();
    }

    private void ApplyConstraints()
    {
        // Get current position and zoom
        double lon = OnlineMaps.instance.position.x;
        double lat = OnlineMaps.instance.position.y;
        float z = OnlineMaps.instance.floatZoom;

        // Clamp longitude to [-90,90]
        lon = Mathf.Clamp((float)lon, -89f, 89f);
        // Clamp longitude to [-90,90]
        lat = Mathf.Clamp((float)lat, 1f, 359f);

        // Enforce minimum zoom of 2
        if (z < 2f) z = 2f;

        // Set the constrained position and zoom without triggering another event
        OnlineMaps.instance.SetPositionAndZoom(lon, lat, z);
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
