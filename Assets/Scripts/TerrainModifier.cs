using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;

public class TerrainModifier : MonoBehaviour
{
    #region Terrain Modifier Terrain Setting SerializeField
    [Header("Terrain Setting"), Space(10)]
    [SerializeField] private Terrain MainTerrain = null;
    [SerializeField] private Vector3Int TerrainScale = default;
    [SerializeField] private float DefaultTerrainHeight = 0;

    private float[,] terrainDepths = null;
    #endregion

    #region Terrain Modifier Scale Setting SerializeField
    [Header("Scale Setting"), Space(10)]
    [SerializeField] private int DefaultScaleLimit;
    [SerializeField] private int MinScaleLimit = 2;
    [SerializeField] private float HeightScale = 2f;
    [SerializeField] private Transform[] SideTransform = null;

    private int realModifierScaleX = 0;
    private int realModifierScaleZ = 0;
    private int modifierScaleX = 0;
    private int modifierScaleZ = 0;

    public int ModifierScaleX 
    { 
        get => modifierScaleX;
        set
        {
            modifierScaleX = Mathf.Clamp(value, MinScaleLimit, int.MaxValue);
            realModifierScaleX = modifierScaleX * 2;
        }
    }

    public int ModifierScaleZ
    {
        get => modifierScaleZ;
        set
        {
            modifierScaleZ = Mathf.Clamp(value, MinScaleLimit, int.MaxValue);
            realModifierScaleZ = modifierScaleZ * 2;
        }
    }

    public string ActiveType { get; private set; }
    #endregion

    #region Transform Modifier Grid Visual Setting SerializeField
    [Header("Visual Setting"), Space(10)]
    [SerializeField] private Transform GridRotateSpace = null;
    [SerializeField] private Transform GridBody = null;
    [SerializeField] private Transform GridVisual = null;
    [SerializeField] private Transform GridBoundary = null;
    [SerializeField] private Material Material_Boundary_Def = null;
    [SerializeField] private Material Material_Boundary_Rot = null;
    [SerializeField] private Renderer VisualRenderer = null;
    [SerializeField] private BoxCollider VisualCollider = null;

    private bool isActive = false;
    private bool isRotate = false;
    private byte generateType = 0;

    private Vector3 landSpaceOffset = default;
    private Quaternion landSpace = default;

    private Camera mainCamera = null;
    #endregion

    #region Transform Modifier Brush Setting SerializeField
    [Header("Brush Setting"), Space(10)]
    [SerializeField] private Texture2D[] BrushArray = null;
    [SerializeField] private int BrushWidth = 10;
    [SerializeField] private int BrushHeight = 10;

    private Texture2D preBrush = null;
    private Texture2D preRotBrush = null;
    #endregion

    #region Terrain Modifier Initialize Functions
    public void InitializeModifier()
    {
        mainCamera = Camera.main;
        ModifierScaleX = DefaultScaleLimit;
        ModifierScaleZ = DefaultScaleLimit;
        ActiveType = "None";

        isActive = false;
        InitializeGrid();
    }

    public void InitializeTerrain()
    {
        // X : WORLD SPACE Z, Z : WORLD SPACE X,
        // Because, terrain depth refers Terrain Texture
        terrainDepths = new float[TerrainScale.x, TerrainScale.z];

        float depth = DefaultTerrainHeight / TerrainScale.y;
        for (int z = 0; z < TerrainScale.z; ++z)
        {
            for (int x = 0; x < TerrainScale.x; ++x)
            {
                terrainDepths[x, z] = depth;
            }
        }

        TerrainData data = MainTerrain.terrainData;
        data.baseMapResolution = TerrainScale.z / 2;
        data.heightmapResolution = TerrainScale.z + 1;
        data.alphamapResolution = TerrainScale.z;
        data.size = TerrainScale;
        MainTerrain.terrainData = data;

        MainTerrain.terrainData.SetHeights(0, 0, terrainDepths);
    }

    public void InitializeBrush()
    {
        preBrush = ResizeBrush(0);
        preRotBrush = ResizeBrush(1);
    }
    #endregion

    #region Terrain Generate Functions
    private void SetDefaultTerrainHeights(int sx, int sz, int scaleX, int scaleZ, float height)
    {
        float[,] heights = new float[scaleZ, scaleX];

        float realHeights = height / TerrainScale.y;
        for (int z = 0; z < scaleZ; ++z)
        {
            for (int x = 0; x < scaleX; ++x)
            {
                heights[z, x] = Mathf.Max(realHeights, terrainDepths[sz + z, sx + x]);
                terrainDepths[sz + z, sx + x] = heights[z, x];
            }
        }

        MainTerrain.terrainData.SetHeights(sx, sz, heights);
    }

    private void SetRotateTerrainHeights(int sx, int sz, int ex, int ez, float height)
    {
        int scaleX = ex - sx + 1;
        int scaleZ = ez - sz + 1;
        float[,] heights = new float[scaleZ, scaleX];

        float realHeights = height / TerrainScale.y;
        for (int z = 0; z < scaleZ; ++z)
        {
            for (int x = 0; x < scaleX; ++x)
            {
                if (Physics.Raycast(new Vector3(sx + x, 100, sz + z), Vector3.down, out RaycastHit hit, 1000, -1) && hit.transform.CompareTag("Decal"))
                {
                    heights[z, x] = Mathf.Max(realHeights, terrainDepths[sz + z, sx + x]);
                    terrainDepths[sz + z, sx + x] = heights[z, x];
                }
                else
                {
                    heights[z, x] = terrainDepths[sz + z, sx + x];
                }
            }
        }

        MainTerrain.terrainData.SetHeights(sx, sz, heights);
    }

    private void SetTerrainHeights(int sx, int sz, float[,] heights)
    {
        MainTerrain.terrainData.SetHeights(sx, sz, heights);
    }
    #endregion

    #region Terrain Modifier Functions
    public bool ModifyTerrainBlock(Action initAction)
    {
        if (!isActive)
        {
            isActive = true;
            ActiveType = "Modify Terrain Block";
            StartCoroutine(ModifyTerrainCoroutine(initAction));
            return true;
        }
        return false;
    }

    private void InitializeGrid()
    {
        generateType = 0;
        GridBody.localScale = new Vector3(2, 10, 2);
        GridBody.localPosition = Vector3.zero;
        GridVisual.localPosition = Vector3.zero;
        GridBoundary.localPosition = Vector3.zero;
        VisualCollider.size = Vector3.one;

        transform.rotation = Quaternion.identity;
        transform.position = Vector3.zero;

        RotateModifier(false);
        VisualRenderer.material = Material_Boundary_Def;
    }

    private void RotateModifier(bool isRotate)
    {
        if (isRotate)
        {
            transform.SetParent(GridRotateSpace);
            transform.localRotation = Quaternion.identity;
            VisualRenderer.material = Material_Boundary_Rot;

            landSpaceOffset = new Vector3(1, 0, 1);
            landSpace = Quaternion.Euler(0, -45, 0);    // 45도 꺽인 Transform 좌표를 다시 되돌리는 작업 
        }
        else
        {
            transform.SetParent(null);
            transform.localRotation = Quaternion.identity;
            VisualRenderer.material = Material_Boundary_Def;

            landSpaceOffset = new Vector3(.5f, 0, .5f);
            landSpace = Quaternion.identity;
        }
        this.isRotate = isRotate;
    }

    private IEnumerator ModifyTerrainCoroutine(Action initAction)
    {
        initAction.Invoke();

        Vector3 nextPosition = Vector3.zero;
        Vector3 startPosition = Vector3.zero;
        Vector3 gridRotation = Vector3.zero;
        Vector3Int gridScale = new Vector3Int(0, Mathf.RoundToInt(GridBody.localScale.y), 0);

        int layer = -1 + 32;
        while (isActive)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                InitializeGrid();
                ActiveType = "None";
                initAction.Invoke();
                break;
            }

            switch (generateType)
            {
                case 0:
                    Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 1000, layer))
                    {
                        startPosition = landSpace * hit.point;
                        startPosition.x = Mathf.RoundToInt(startPosition.x) / 2 * 2 + landSpaceOffset.x;
                        startPosition.z = Mathf.RoundToInt(startPosition.z) / 2 * 2 + landSpaceOffset.z;
                        transform.localPosition = startPosition;
                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        GridBody.localPosition = new Vector3(-1, 0, -1);
                        GridVisual.localPosition = new Vector3(.5f, 0, .5f);
                        GridBoundary.localPosition = new Vector3(.5f, 0, .5f);
                        generateType = 1;
                    }

                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        RotateModifier(!isRotate);
                    }
                    break;
                case 1:
                    if (Input.GetMouseButton(0))
                    {
                        ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(ray, out hit, 1000, layer))
                        {
                            nextPosition = landSpace * hit.point;
                            gridScale.x = Mathf.RoundToInt(nextPosition.x - startPosition.x);
                            gridScale.z = Mathf.RoundToInt(nextPosition.z - startPosition.z);
                            gridRotation.x = gridScale.z < 0 ? 180 : 0;
                            gridRotation.z = gridScale.x < 0 ? 180 : 0;
                            gridScale.x = Mathf.Clamp(Mathf.Abs(gridScale.x) / 2 * 2, 2, realModifierScaleX);
                            gridScale.z = Mathf.Clamp(Mathf.Abs(gridScale.z) / 2 * 2, 2, realModifierScaleZ);
                            transform.localRotation = Quaternion.Euler(gridRotation.x, 0, gridRotation.z);
                            GridBody.localScale = gridScale;
                        }
                    }
                    else
                    {
                        if (isRotate)
                        {
                            float floatSX = float.MaxValue, floatSZ = float.MaxValue;
                            float floatEX = 0, floatEZ = 0;
                            for (int i = 0; i < SideTransform.Length; ++i)
                            {
                                floatSX = Mathf.Min(SideTransform[i].position.x, floatSX);
                                floatSZ = Mathf.Min(SideTransform[i].position.z, floatSZ);
                                floatEX = Mathf.Max(SideTransform[i].position.x, floatEX);
                                floatEZ = Mathf.Max(SideTransform[i].position.z, floatEZ);
                            }
                            int intSX = Mathf.RoundToInt(floatSX), intSZ = Mathf.RoundToInt(floatSZ);
                            int intEX = Mathf.RoundToInt(floatEX), intEZ = Mathf.RoundToInt(floatEZ);

                            SetRotateTerrainHeights(intSX - 1, intSZ - 1, intEX, intEZ, HeightScale);

                            /*
                            int sx = Mathf.RoundToInt(startPosition.x + (gridRotation.z.Equals(180) ? -gridScale.x + .5f : -1.5f));
                            int sz = Mathf.RoundToInt(startPosition.z + (gridRotation.x.Equals(180) ? -gridScale.z + .5f : -1.5f));
                            int scaleX = gridScale.x / 2;
                            int scaleZ = gridScale.z / 2;
                            for (int x = 0; x < scaleX; ++x)
                            {
                                for (int z = 0; z < scaleZ; ++z)
                                {
                                    Vector3 position = Quaternion.Euler(0, 45, 0) * new Vector3(sx + 2 * x + 1, 100, sz + 2 * z + 1);
                                    if (Physics.Raycast(position, Vector3.down, out hit, 1000, layer))
                                    {
                                        GameObject clone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                        clone.transform.position = hit.point;
                                        PaintBrush(Mathf.RoundToInt(hit.point.x), Mathf.RoundToInt(hit.point.z), preRotBrush);
                                    }
                                }
                            }
                            */
                        }
                        else
                        {
                            //SetDefaultTerrainHeights(sx, sz, gridScale.x + 1, gridScale.z + 1, HeightScale);

                            int sx = Mathf.RoundToInt(startPosition.x + (gridRotation.z.Equals(180) ? -gridScale.x + .5f : -1.5f));
                            int sz = Mathf.RoundToInt(startPosition.z + (gridRotation.x.Equals(180) ? -gridScale.z + .5f : -1.5f));
                            int scaleX = gridScale.x / 2;
                            int scaleZ = gridScale.z / 2;
                            for (int x = 0; x < scaleX; ++x)
                            {
                                for (int z = 0; z < scaleZ; ++z)
                                {
                                    if (Physics.Raycast(new Vector3(sx + 2 * x + 1.5f, 100, sz + 2 * z + 1.5f), Vector3.down, out hit, 1000, layer))
                                    {
                                        GameObject clone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                        clone.transform.position = hit.point;
                                        PaintBrush(Mathf.RoundToInt(hit.point.x), Mathf.RoundToInt(hit.point.z), preBrush);
                                    }
                                }
                            }
                        }

                        Instantiate(GridBody, GridBody.position, GridBody.rotation);
                        InitializeGrid();
                    }
                    break;
            }
            yield return null;
        }

    }
    #endregion

    #region Terrain Brush Functions
    private void PaintBrush(int cx, int cz, Texture2D brush)
    {
        int width = brush.width;
        int height = brush.height;
        int sx = cx - width / 2;
        int sz = cz - height / 2;
        float[,] heights = new float[height, width];
        for (int z = 0; z < height; ++z)
        {
            for (int x = 0; x < width; ++x)
            {
                heights[z, x] = brush.GetPixel(z, x).a * HeightScale / TerrainScale.y;
                heights[z, x] = Mathf.Max(heights[z, x], terrainDepths[sz + z, sx + x]);
                terrainDepths[sz + z, sx + x] = heights[z, x];
            }
        }

        SetTerrainHeights(sx, sz, heights);
    }

    private IEnumerator PaintBrush()
    {
        bool isActive = true;
        int layer = -1 + 32;

        int brushIndex = 1;
        Texture2D brush = BrushArray[brushIndex];

        while (isActive)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000, layer))
                {
                    int width = preBrush.width;
                    int height = preBrush.height;
                    PaintBrush(Mathf.RoundToInt(hit.point.x), Mathf.RoundToInt(hit.point.z), brush);

                    int sx = Mathf.RoundToInt(hit.point.x) - width / 2;
                    int sz = Mathf.RoundToInt(hit.point.z) - height / 2;
                    GameObject clone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    clone.transform.position = new Vector3(Mathf.RoundToInt(hit.point.x) + 1, hit.point.y, Mathf.RoundToInt(hit.point.z) + 1);
                    clone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    clone.transform.position = new Vector3(sx + 1, hit.point.y, sz + 1);
                    clone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    clone.transform.position = new Vector3(sx + width, hit.point.y, sz + height);
                }
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                brush = ResizeBrush(brushIndex);
                Debug.Log("Resize Brush");
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                isActive = false;
            }

            yield return null;
        }
    }

    private Texture2D ResizeBrush(int index)
    {
        Texture2D defaultBrush = BrushArray[index];
        Texture2D newBrush = new Texture2D(BrushWidth, BrushHeight, defaultBrush.format, false);
        Color[] prePixels = defaultBrush.GetPixels(0);

        float incX = (1f / BrushWidth);
        float incY = (1f / BrushHeight);
        for (int pixel = 0; pixel < prePixels.Length; ++pixel)
            prePixels[pixel] = defaultBrush.GetPixelBilinear(incX * (pixel % BrushWidth), incY * (Mathf.Floor(pixel / BrushWidth)));

        newBrush.SetPixels(prePixels, 0);
        newBrush.Apply();
        return newBrush;
    }
    #endregion
}
