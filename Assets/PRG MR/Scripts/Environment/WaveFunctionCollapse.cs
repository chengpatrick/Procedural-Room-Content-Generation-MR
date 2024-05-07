using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Bson;
using Oculus.Platform;
using UnityEditor;
using UnityEngine;

public class WaveFunctionCollapse : MonoBehaviour
{
    /*
     * Wave Function Collapse (WFC) takes in a 2D region of range
     * Outputs a 2D map of locations of virtual objects
     * 
     * Rules of WFC is stated as below:
     * 0 - Empty cell
     * 1 - Small object
     * 2 - Medium object
     * 3 - Large object
     * 4 - Corner of room
     * 
     * Neighbor rules:
     * 0 : 1, 2, 3
     * 1 : 0
     * 2 : 0
     * 3 : 0
     * 
     * For Large objects, all the surrounding neighbors will be None.
     * 
     * In a full map, 20% - 40% will be filled with virtual objects
     * In all virtual objects, 20% will be large objects, 40% will be medium objects
     *40% will be small objects
     * 
     * Alg:
     * 1. Find random cell to start with, choose random state outside of None
     * 2. Find cell with lowest entropy(choices), collapse that cell, repeat
     * 3. If all cell entropoy is equal, choose random cell to execute
     * 
     */

    [Flags]
    private enum WfcMap
    {
        None = 1 << 0, // 1
        Small = 1 << 1, // 2
        Medium = 1 << 2, // 4
        Large = 1 << 3, // 8
        Corner = 1 << 4 // 16
    };

    // number of possible states a cell can have
    public int stateNum = 4;

    [SerializeField] int MapWidth = 10;
    [SerializeField] int MapHeight = 10;

    [Tooltip("Percentage of aprox space that is filled with objs (0.0 - 0.7)")]
    [SerializeField] float FillPercentage = 0.3f;

    [Tooltip("Percentage of aprox amount of large objects in space (0.0 - 1,0)(large + medium + small = 1)")]
    [SerializeField] float LargePercentage = 0.2f;

    [Tooltip("Percentage of aprox amount of medium objects in space (0.0 - 1,0)(large + medium + small = 1)")]
    [SerializeField] float MediumPercentage = 0.4f;

    [Tooltip("Percentage of aprox amount of small objects in space (0.0 - 1,0)(large + medium + small = 1)")]
    [SerializeField] float SmallPercentage = 0.4f;

    // number of cells that are finalized
    private int collapsedCells = 0;

    private int largeObjCount;
    private int mediumObjCount;
    private int smallObjCount;

    [SerializeField] GameObject NonePrefab;
    [SerializeField] GameObject SmallPrefab;
    [SerializeField] GameObject MediumPrefab;
    [SerializeField] GameObject LargePrefab;
    [SerializeField] GameObject CornerPrefab;

    [SerializeField] RoomBoundingBox RBB;
    [SerializeField] bool UseRBB;

    private WfcMap[,] map;
    private WfcMap[] mapStates;

    // width and height of map
    public int n, m;

    // bounding corners of room
    private Vector3 minX;
    private Vector3 maxX;
    private Vector3 minZ;
    private Vector3 maxZ;


    public void StartWFC()
    {
        CleanUpMap();
        RBB.StartFill();

        Debug.Log("Starting WFC...");

        mapStates = new WfcMap[stateNum];
        mapStates = (WfcMap[])Enum.GetValues(typeof(WfcMap));

        SetupGrid(MapWidth, MapHeight);
        CalculateSceneObjCounts();

        GetCornerBoundsFromRBB();
        WFC();
    }

    public void CleanUpMap()
    {
        // Find prev wfc objects in scene
        GameObject[] allGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

        foreach (GameObject go in allGameObjects)
        {
            if (go.name.StartsWith("WFCParent"))
            {
                DestroyImmediate(go);
                break;
            }
        }

        // reset var
        collapsedCells = 0;

        ClearConsole();
    }

    private void SetupGrid(int w, int h)
    {
        n = w;
        m = h;

        // set up map
        map = new WfcMap[n, m];
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < m; j++)
            {
                // if cell is on edge of grid, set to None State
                if (i == 0 || j == 0 || i == n - 1 || j == m - 1)
                {
                    map[i, j] = WfcMap.None;
                    collapsedCells++;
                }
                else
                    map[i, j] = WfcMap.None | WfcMap.Small | WfcMap.Medium | WfcMap.Large;
            }
        }

        map[0, 0] = WfcMap.Corner;
        map[0, m - 1] = WfcMap.Corner;
        map[n - 1, 0] = WfcMap.Corner;
        map[n - 1, m - 1] = WfcMap.Corner;
    }

    private void CalculateSceneObjCounts()
    {
        int allocation = (int)((n * m - collapsedCells) * FillPercentage);

        largeObjCount = (int)(allocation * LargePercentage);
        mediumObjCount = (int)(allocation * MediumPercentage);
        smallObjCount = (int)(allocation * SmallPercentage);

        Debug.Log("CalculateSceneObjCounts: Large obj: " + largeObjCount);
        Debug.Log("CalculateSceneObjCounts: Medium obj: " + mediumObjCount);
        Debug.Log("CalculateSceneObjCounts: Small obj: " + smallObjCount);
    }

    private void WFC()
    {
        // set random state for random start point
        int randX = UnityEngine.Random.Range(1, n - 1);
        int randY = UnityEngine.Random.Range(1, m - 1);

        WfcMap randState = GetRandomState();
        map[randX, randY] = randState;

        UpdateNeighbors(randX, randY);
        UpdateCornerNeighbors(randX, randY);

        if(randState == WfcMap.Large)
            UpdateLargeNeighbors(randX, randY);

        int x, y;
        while (true)
        {
            FindLowestEntropyCell(out x, out y);

            if (x == -1 && y == -1)
                break;

            randState = GetRandomState(GetStateList(map[x, y]));
            map[x, y] = randState;

            UpdateNeighbors(x, y);
            UpdateCornerNeighbors(x, y);

            if (randState == WfcMap.Large)
                UpdateLargeNeighbors(x, y);
        }

        PrintMap();

        // Spawn Objects in scene
        if (UseRBB)
            SpawnMapInRoom();
        else
            SpawnMap();
    }

    // Update neightbor cells around i and j
    private void UpdateNeighbors(int i, int j)
    {
        if (i - 1 >= 0)
        {
            if (!CheckCellCollapsed(i - 1, j))
            {
                UpdateCell(map[i, j], i - 1, j);
            }
        }

        if (i + 1 < n)
        {
            if (!CheckCellCollapsed(i + 1, j))
            {
                UpdateCell(map[i, j], i + 1, j);
            }
        }

        if (j - 1 >= 0)
        {
            if (!CheckCellCollapsed(i, j - 1))
            {
                UpdateCell(map[i, j], i, j - 1);
            }
        }

        if (j + 1 < m)
        {
            if (!CheckCellCollapsed(i, j + 1))
            {
                UpdateCell(map[i, j], i, j + 1);
            }
        }
    }

    // Check for corner neighbors
    private void UpdateCornerNeighbors(int i, int j)
    {
        if (i - 1 >= 0 && j - 1 >= 0)
        {
            if (!CheckCellCollapsed(i - 1, j - 1))
            {
                UpdateCornerCell(map[i, j], i - 1, j - 1);
            }
        }

        if (i + 1 < n && j - 1 >= 0)
        {
            if (!CheckCellCollapsed(i + 1, j - 1))
            {
                UpdateCornerCell(map[i, j], i + 1, j - 1);
            }
        }

        if (i - 1 >= 0 && j + 1 < m)
        {
            if (!CheckCellCollapsed(i - 1, j + 1))
            {
                UpdateCornerCell(map[i, j], i - 1, j + 1);
            }
        }

        if (i + 1 < n && j + 1 < m)
        {
            if (!CheckCellCollapsed(i + 1, j + 1))
            {
                UpdateCornerCell(map[i, j], i + 1, j + 1);
            }
        }
    }

    // Update neightbor cells around i and j if curr cell is large
    private void UpdateLargeNeighbors(int i, int j)
    {
        if (i - 2 >= 0)
        {
            if (!CheckCellCollapsed(i - 2, j))
            {
                UpdateCell(map[i, j], i - 2, j);
            }
        }

        if (i + 2 < n)
        {
            if (!CheckCellCollapsed(i + 2, j))
            {
                UpdateCell(map[i, j], i + 2, j);
            }
        }

        if (j - 2 >= 0)
        {
            if (!CheckCellCollapsed(i, j - 2))
            {
                UpdateCell(map[i, j], i, j - 2);
            }
        }

        if (j + 2 < m)
        {
            if (!CheckCellCollapsed(i, j + 2))
            {
                UpdateCell(map[i, j], i, j + 2);
            }
        }

        if (i - 2 >= 0 && j - 2 >= 0)
        {
            if (!CheckCellCollapsed(i - 2, j - 2))
            {
                UpdateCornerCell(map[i, j], i - 2, j - 2);
            }
            if (!CheckCellCollapsed(i - 1, j - 2))
            {
                UpdateCornerCell(map[i, j], i - 1, j - 2);
            }
            if (!CheckCellCollapsed(i - 2, j - 1))
            {
                UpdateCornerCell(map[i, j], i - 2, j - 1);
            }
        }

        if (i + 2 < n && j - 2 >= 0)
        {
            if (!CheckCellCollapsed(i + 2, j - 2))
            {
                UpdateCornerCell(map[i, j], i + 2, j - 2);
            }
            if (!CheckCellCollapsed(i + 1, j - 2))
            {
                UpdateCornerCell(map[i, j], i + 1, j - 2);
            }
            if (!CheckCellCollapsed(i + 2, j - 1))
            {
                UpdateCornerCell(map[i, j], i + 2, j - 1);
            }
        }

        if (i - 2 >= 0 && j + 2 < m)
        {
            if (!CheckCellCollapsed(i - 2, j + 2))
            {
                UpdateCornerCell(map[i, j], i - 2, j + 2);
            }
            if (!CheckCellCollapsed(i - 1, j + 2))
            {
                UpdateCornerCell(map[i, j], i - 1, j + 2);
            }
            if (!CheckCellCollapsed(i - 2, j + 1))
            {
                UpdateCornerCell(map[i, j], i - 2, j + 1);
            }
        }

        if (i + 2 < n && j + 2 < m)
        {
            if (!CheckCellCollapsed(i + 2, j + 2))
            {
                UpdateCornerCell(map[i, j], i + 2, j + 2);
            }
            if (!CheckCellCollapsed(i + 1, j + 2))
            {
                UpdateCornerCell(map[i, j], i + 1, j + 2);
            }
            if (!CheckCellCollapsed(i + 2, j + 1))
            {
                UpdateCornerCell(map[i, j], i + 2, j + 1);
            }
        }
    }

    // Update cell based on prev cell state rules
    private void UpdateCell(WfcMap prev, int i, int j)
    {
        if (prev is WfcMap.None)
        {
            map[i, j] &= ~WfcMap.None;
        }
        else if (prev is WfcMap.Small)
        {
            map[i, j] = WfcMap.None;
        }
        else if (prev is WfcMap.Medium)
        {
            map[i, j] = WfcMap.None;
        }
        else if (prev is WfcMap.Large)
        {
            map[i, j] = WfcMap.None;
        }
    }

    // Update cell based on prev cell state rules
    private void UpdateCornerCell(WfcMap prev, int i, int j)
    {
        if (prev is WfcMap.None)
        {
            map[i, j] &= ~WfcMap.None;
        }
        else if (prev is WfcMap.Small)
        {
            map[i, j] &= ~WfcMap.Large;
        }
        else if (prev is WfcMap.Medium)
        {
            map[i, j] &= ~WfcMap.Large;
        }
        else if (prev is WfcMap.Large)
        {
            map[i, j] = WfcMap.None;
        }
    }

    // Return a random WfcMap State
    // If list is given return random WfcMap State within list
    private WfcMap GetRandomState(List<WfcMap> list = null)
    {
        if (list == null)
        {
            if (largeObjCount-- > 0)
                return WfcMap.Large;
            else if (mediumObjCount-- > 0)
                return WfcMap.Medium;
            else if (smallObjCount-- > 0)
                return WfcMap.Small;
            else
                return WfcMap.None;
        }
        else
        {
            if (list.Contains(WfcMap.Large) && largeObjCount-- > 0)
                return WfcMap.Large;
            else if (list.Contains(WfcMap.Medium) && mediumObjCount-- > 0)
                return WfcMap.Medium;
            else if (list.Contains(WfcMap.Small) && smallObjCount-- > 0)
                return WfcMap.Small;
            else
                return WfcMap.None;
        }
    }

    private List<WfcMap> GetStateList(WfcMap states)
    {
        List<WfcMap> individualFlags = new List<WfcMap>();

        foreach (WfcMap flag in Enum.GetValues(typeof(WfcMap)))
        {
            if (states.HasFlag(flag))
            {
                individualFlags.Add(flag);
            }
        }

        return individualFlags;
    }

    // Find cell coord with smallest entropy, return (-1, -1) if none.
    private void FindLowestEntropyCell(out int x, out int y)
    {
        WfcMap min = WfcMap.None | WfcMap.Small | WfcMap.Medium | WfcMap.Large;

        x = -1;
        y = -1;

        List<int[]> openCellsList = new List<int[]>();

        // iterate to find cell with smallest entropy
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < m; j++)
            {
                if (!CheckCellCollapsed(i, j))
                {
                    openCellsList.Add(new int[] { i, j });
                }
            }
        }

        if (openCellsList.Count > 0)
        {
            int idx = UnityEngine.Random.Range(0, openCellsList.Count);

            x = openCellsList[idx][0];
            y = openCellsList[idx][1];
        }
    }

    private bool CheckCellCollapsed(int i, int j)
    {
        return map[i, j] is WfcMap.None
            or WfcMap.Small
            or WfcMap.Medium
            or WfcMap.Large
            or WfcMap.Corner;
    }

    private void GetCornerBoundsFromRBB()
    {
        minX = RBB.minX;
        maxX = RBB.maxX;
        minZ = RBB.minZ;
        maxZ = RBB.maxZ;
    }

    private void PrintMap()
    {
        String line = "";
        for (int i = 0; i < n; i++)
        {
            line = i + " Map Layout:      ";
            for (int j = 0; j < m; j++)
            {
                line += map[i, j] + " | ";
            }
            Debug.Log(line);
        }
    }

    private void SpawnMapInRoom()
    {
        int i = 0, j = 0;
        GameObject instantiateParent = new GameObject("WFCParent");

        float xDiff = (minX.x - minZ.x) / m;
        float zDiff = (maxX.z - minZ.z) / n;

        for (float a = minZ.x; a < maxX.x - 0.01f; a += (maxX.x - minZ.x) / n)
        {
            j = 0;

            for (float b = minZ.z; b < minX.z - 0.01f; b += (minX.z - minZ.z) / m)
            {
                float xPos = a;
                float zPos = b;

                if (i > 0)
                    zPos += zDiff * i;
                if (j > 0)
                    xPos += xDiff * j;

                Vector3 pos = new Vector3(xPos, 0.2f, zPos);

                GameObject instantiateObj;
                switch (map[i, j])
                {
                    case WfcMap.None:
                        instantiateObj = Instantiate(NonePrefab, pos, Quaternion.identity);
                        instantiateObj.transform.parent = instantiateParent.transform;
                        break;
                    case WfcMap.Small:
                        instantiateObj = Instantiate(SmallPrefab, pos, Quaternion.identity);
                        instantiateObj.transform.parent = instantiateParent.transform;
                        break;
                    case WfcMap.Medium:
                        instantiateObj = Instantiate(MediumPrefab, pos, Quaternion.identity);
                        instantiateObj.transform.parent = instantiateParent.transform;
                        break;
                    case WfcMap.Large:
                        instantiateObj = Instantiate(LargePrefab, pos, Quaternion.identity);
                        instantiateObj.transform.parent = instantiateParent.transform;
                        break;
                    case WfcMap.Corner:
                        instantiateObj = Instantiate(CornerPrefab, pos, Quaternion.identity);
                        instantiateObj.transform.parent = instantiateParent.transform;
                        break;
                    default:
                        break;
                }
                j++;
            }
            i++;
        }
    }

    private void SpawnMap()
    {
        GameObject instantiateParent = new GameObject("WFCParent");

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                Vector3 pos = new Vector3(i - 50, minX.y, j - 50);

                GameObject instantiateObj;
                switch (map[i, j])
                {
                    case WfcMap.None:
                        instantiateObj = Instantiate(NonePrefab, pos, Quaternion.identity);
                        instantiateObj.transform.parent = instantiateParent.transform;
                        break;
                    case WfcMap.Small:
                        instantiateObj = Instantiate(SmallPrefab, pos, Quaternion.identity);
                        instantiateObj.transform.parent = instantiateParent.transform;
                        break;
                    case WfcMap.Medium:
                        instantiateObj = Instantiate(MediumPrefab, pos, Quaternion.identity);
                        instantiateObj.transform.parent = instantiateParent.transform;
                        break;
                    case WfcMap.Large:
                        instantiateObj = Instantiate(LargePrefab, pos, Quaternion.identity);
                        instantiateObj.transform.parent = instantiateParent.transform;
                        break;
                    case WfcMap.Corner:
                        instantiateObj = Instantiate(CornerPrefab, pos, Quaternion.identity);
                        instantiateObj.transform.parent = instantiateParent.transform;
                        break;
                }
            }
        }
    }

    private static void ClearConsole()
    {
        var assembly = Assembly.GetAssembly(typeof(SceneView));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }
}
