using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ParamUIUIController : MonoBehaviour
{
    [Tooltip("Reference to the ForecastController that has paramColors.")]
    public ForecastController forecastController;

    [Tooltip("Parent container (e.g., a VerticalLayoutGroup) where parameter rows will be added.")]
    public Transform parentContainer;

    [Tooltip("Prefab for a single parameter UI row, containing a Text and an Image.")]
    public GameObject paramUIRowPrefab;

    void Start()
    {
        UpdateParamUI();
    }

    public void UpdateParamUI()
    {
        // Clear existing children if any
        foreach (Transform child in parentContainer)
        {
            Destroy(child.gameObject);
        }

        // Loop through parameters in ForecastController
        foreach (ParamColor pc in forecastController.paramColors)
        {
            // Instantiate a UI row
            GameObject row = Instantiate(paramUIRowPrefab, parentContainer);

            // Find Text and Image components
            TextMeshProUGUI nameText = row.transform.Find("ParamNameText").GetComponent<TextMeshProUGUI>();
            Image colorImage = row.transform.Find("ColorImage").GetComponent<Image>();

            // Assign parameter name and highlight color
            nameText.text = pc.parameterName;
            colorImage.color = pc.highlightColor;
        }
    }
}
