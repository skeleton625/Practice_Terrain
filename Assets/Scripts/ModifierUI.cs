using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ModifierUI : MonoBehaviour
{
    [Header("Terrain Modifier Setting"), Space(10)]
    [SerializeField] private TerrainModifier Modifier = null;
    [SerializeField] private TextMeshProUGUI PreRunningCoroutine = null;

    [Header("Default Modifier UI Setting"), Space(10)]
    [SerializeField] private TMP_InputField XscaleInputField = null;
    [SerializeField] private TMP_InputField ZscaleInputField = null;
    [SerializeField] private Toggle MixBrushToggle = null;

    [Header("Brush Modifier UI Setting"), Space(10)]
    [SerializeField] private Image[] BrushBackgrounds = null;
    [SerializeField] private Color SelectColor = default;
    [SerializeField] private Color DefaultColor = default;
    [SerializeField] private TMP_InputField BrushWidth = null;
    [SerializeField] private TMP_InputField BrushHeight = null;

    private void Awake()
    {
        Modifier.InitializeModifier();
        Modifier.InitializeTerrain();
        Modifier.InitializeBrush();

        XscaleInputField.text = Modifier.ModifierScaleX.ToString();
        ZscaleInputField.text = Modifier.ModifierScaleZ.ToString();

        BrushWidth.text = Modifier.BrushWidth.ToString();
        BrushHeight.text = Modifier.BrushHeight.ToString();

        for (int i = 0; i < BrushBackgrounds.Length; ++i)
            BrushBackgrounds[i].color = DefaultColor;
        BrushBackgrounds[Modifier.PreBrushIndex].color = SelectColor;
    }

    public void StartDefaultModifier()
    {
        Modifier.StartDefaultModifying(RefreshRunningCoroutine);
    }

    public void ChangeDefaultModifyingScale(bool isX)
    {
        if (isX)
        {
            Modifier.ModifierScaleX = int.Parse(XscaleInputField.text);
            XscaleInputField.text = Modifier.ModifierScaleX.ToString();
        }
        else
        {
            Modifier.ModifierScaleZ = int.Parse(ZscaleInputField.text);
            ZscaleInputField.text = Modifier.ModifierScaleZ.ToString();
        }
    }

    public void ChangeMixBrush()
    {
        Modifier.MixBrush = MixBrushToggle.isOn;
    }

    public void StartImageModifier()
    {
        Modifier.StartBrushModifying(RefreshRunningCoroutine);
    }

    public void ChangeBrushImage(int index)
    {
        BrushBackgrounds[Modifier.PreBrushIndex].color = DefaultColor;
        BrushBackgrounds[index].color = SelectColor;
        Modifier.PreBrushIndex = index;
    }

    public void ChangeBrushSize(bool isWidth)
    {
        if (isWidth)
        {
            Modifier.BrushWidth = int.Parse(BrushWidth.text);
            BrushWidth.text = Modifier.BrushWidth.ToString();
        }
        else
        {
            Modifier.BrushHeight = int.Parse(BrushHeight.text);
            BrushHeight.text = Modifier.BrushHeight.ToString();
        }
    }

    private void RefreshRunningCoroutine()
    {
        PreRunningCoroutine.text = Modifier.GetActiveType();
    }
}
