using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ModifierUI : MonoBehaviour
{
    [Header("Terrain Modifier Setting"), Space(10)]
    [SerializeField] private TerrainModifier Modifier = null;

    [Header("Modifier UI Setting"), Space(10)]
    [SerializeField] private TextMeshProUGUI PreRunningCoroutine = null;
    [SerializeField] private TMP_InputField XscaleInputField = null;
    [SerializeField] private TMP_InputField ZscaleInputField = null;

    private void Awake()
    {
        Modifier.InitializeModifier();
        Modifier.InitializeTerrain();
        Modifier.InitializeBrush();

        XscaleInputField.text = Modifier.ModifierScaleX.ToString();
        ZscaleInputField.text = Modifier.ModifierScaleZ.ToString();
    }

    public void ChangeModifierScale(bool isX)
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

    public void StartModifyingTerrainBlock()
    {
        Modifier.ModifyTerrainBlock(RefreshRunningCoroutine);
    }

    public void RefreshRunningCoroutine()
    {
        PreRunningCoroutine.text = Modifier.ActiveType;
    }
}
