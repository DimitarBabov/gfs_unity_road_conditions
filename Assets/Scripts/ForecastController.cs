using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ParamColor
{
    public string parameterName;
    public Color highlightColor = Color.white;
}

public class ForecastController : MonoBehaviour
{
    [Tooltip("Material that uses the 6-parameter shader.")]
    public Material targetMaterial;

    [Tooltip("How often to change the forecast texture (in seconds).")]
    public float updateInterval = 1f;

    [Tooltip("List of parameters to display. Up to 6. Order defines priority.")]
    public List<string> selectedParameters = new List<string>();

    [Tooltip("A blank texture assigned to parameters with no data to avoid mid-gray unassigned look.")]
    public Texture2D blankTexture;

    [Tooltip("List of parameter-highlight color pairs set in the Inspector.")]
    public List<ParamColor> paramColors = new List<ParamColor>();

    private float timer = 0f;
    private int currentForecastIndex = 0;

    private Dictionary<string, List<Texture2D>> paramTextures = new Dictionary<string, List<Texture2D>>();
    private bool dataReady = false;

    private const int MaxParams = 6;

    void Start()
    {
        StartCoroutine(WaitForGfsDataManager());
    }

    IEnumerator WaitForGfsDataManager()
    {
        while (GfsDataManager.Instance == null || !GfsDataManager.Instance.IsInitialized())
        {
            yield return null;
        }

        InitializeParameterTextures();
        SetCurrentForecastTextures();
        dataReady = true;
    }

    void Update()
    {
        if (!dataReady || selectedParameters.Count == 0) return;

        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            AdvanceForecast();
        }
    }

    private void InitializeParameterTextures()
    {
        paramTextures.Clear();

        foreach (var p in selectedParameters)
        {
            if (GfsDataManager.Instance.texturesByParam.ContainsKey(p))
            {
                paramTextures[p] = GfsDataManager.Instance.texturesByParam[p];
            }
            else
            {
                paramTextures[p] = new List<Texture2D>();
            }
        }

        ApplyHighlightColors();
    }

    private void AdvanceForecast()
    {
        currentForecastIndex++;
        SetCurrentForecastTextures();
    }

    private void SetCurrentForecastTextures()
    {
        for (int i = 0; i < MaxParams; i++)
        {
            if (i < selectedParameters.Count)
            {
                string param = selectedParameters[i];
                List<Texture2D> texList = paramTextures.ContainsKey(param) ? paramTextures[param] : null;

                Texture2D chosenTex = blankTexture; // default to blank
                if (texList != null && texList.Count > 0)
                {
                    int forecastIndex = currentForecastIndex % texList.Count;
                    chosenTex = texList[forecastIndex] ?? blankTexture;
                }

                SetParamMaskTexture(i + 1, chosenTex);
            }
            else
            {
                // No param in this slot, assign blank texture
                SetParamMaskTexture(i + 1, blankTexture);
            }
        }
    }

    private void SetParamMaskTexture(int paramIndex, Texture2D tex)
    {
        string propName = "_Param" + paramIndex + "MaskTex";
        targetMaterial.SetTexture(propName, tex);
    }

    private void ApplyHighlightColors()
    {
        // We'll assign colors based on selected parameters.
        for (int i = 0; i < MaxParams; i++)
        {
            Color c = Color.white; // default if not specified
            if (i < selectedParameters.Count)
            {
                string param = selectedParameters[i];
                // Find the ParamColor entry for this param
                ParamColor pc = paramColors.Find(x => x.parameterName == param);
                if (pc != null)
                {
                    c = pc.highlightColor;
                }
            }
            else
            {
                c = new Color(1, 1, 1, 0);
            }

            string colorPropName = "_Param" + (i + 1) + "HighlightColor";
            targetMaterial.SetColor(colorPropName, c);
        }
    }

    public void RefreshParameters()
    {
        currentForecastIndex = 0;
        InitializeParameterTextures();
        SetCurrentForecastTextures();
    }

    /// <summary>
    /// Change the selected parameters at runtime and refresh.
    /// </summary>
    public void SetSelectedParameters(List<string> newParams)
    {
        selectedParameters = newParams;
        RefreshParameters();
    }

    /// <summary>
    /// If you still want to set colors at runtime, you can modify paramColors
    /// and call RefreshParameters() afterwards.
    /// </summary>
    public void SetParamHighlightColorRuntime(string param, Color color)
    {
        var pc = paramColors.Find(x => x.parameterName == param);
        if (pc == null)
        {
            pc = new ParamColor { parameterName = param, highlightColor = color };
            paramColors.Add(pc);
        }
        else
        {
            pc.highlightColor = color;
        }
        RefreshParameters();
    }
}
