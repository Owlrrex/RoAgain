#if UNITY_EDITOR
using Client;
using OwlLogging;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(GridComponent))]
public class GridComponentEditor : Editor
{
    private static Vector3[] _raycastPointBuffer = new Vector3[5];

    public override void OnInspectorGUI()
    {
        GridComponent myGrid = (GridComponent)target;

        base.OnInspectorGUI();

        float.TryParse(EditorGUILayout.TextField("NavMesh Search Radius", GridComponent.CellWalkableCheckRadius.ToString()), out GridComponent.CellWalkableCheckRadius);

        if(GUILayout.Button("Create this Map File"))
        {
            BuildMapFilesForGrid(myGrid);
        }

        if (GUILayout.Button("Create All Map Files"))
        {
            BuildAllMapFiles();
        }

        if(GUILayout.Button("Clear Gizmos"))
        {
            myGrid.GizmoDots.Clear();
        }
    }

    private void BuildAllMapFiles()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        foreach(GameObject root in scene.GetRootGameObjects())
        {
            GridComponent[] grids = root.GetComponentsInChildren<GridComponent>();
            foreach(GridComponent grid in grids)
            {
                BuildMapFilesForGrid(grid);
            }
        }
    }

    private void BuildMapFilesForGrid(GridComponent grid)
    {
        if (string.IsNullOrWhiteSpace(grid.EditorMapId)
        || grid.EditorBounds.x <= 0
        || grid.EditorBounds.y <= 0)
        {
            OwlLogger.LogError("Invalid inputs!", GameComponent.Editor);
            return;
        }

        GridCellData[] gridCells = BuildCellGridForGrid(grid);

        // Now we just need to serialize the Grid to file.
        // Do we feed it into a GridData object first, so that bounds can be serialized along with it
        GridData dataToSerialize = new();
        if(!dataToSerialize.Initialize(gridCells, grid.EditorBounds))
        {
            OwlLogger.Log("Initialization of GridData with generated cells failed!", GameComponent.Editor);
            return;
        }

        byte[] rawBytes = dataToSerialize.Serialize();
        if(rawBytes == null || rawBytes.Length == 0)
        {
            OwlLogger.LogError("Serializing GridData to bytes failed!", GameComponent.Editor);
            return;
        }

        string filename = Path.Combine(Application.dataPath, "MapFiles", $"{grid.EditorMapId}.gatu");
        using FileStream fs = new(filename, FileMode.Create, FileAccess.Write);
        fs.Write(rawBytes, 0, rawBytes.Length);
    }

    private GridCellData[] BuildCellGridForGrid(GridComponent grid)
    {
        grid.GizmoDots.Clear();

        GridData coordConverter = new GridData();
        coordConverter.Initialize(grid.EditorBounds); // this allocates another array, but it's hidden so we can't use it.

        GridCellData[]  gridCells = new GridCellData[grid.EditorBounds.x * grid.EditorBounds.y];

        for (int x = 0; x < grid.EditorBounds.x; x++)
        {
            for (int y = 0; y < grid.EditorBounds.y; y++)
            {
                GridCellData data = new();
                float validateResult = ValidateCell(x, y, grid);
                if (validateResult != -1)
                {
                    data.CellHeight = validateResult;
                }
                else
                {
                    data.CellHeight = GridCellData.CELL_HEIGHT_VOID;
                }
                gridCells[coordConverter.CoordsToIndex(new(x+1, y+1))] = data;
            }
        }

        return gridCells;
    }

    private float ValidateCell(int x, int y, GridComponent grid)
    {
        float raycastDistance = 2000f;
        float raycastStartY = 1000f;
        int layers = LayerMask.GetMask(new string[] { "ClickableTerrain" });
        float cellXOffsetLocal = grid.CellSize * x;
        float cellZOffsetLocal = grid.CellSize * y;

        Array.Clear(_raycastPointBuffer, 0, _raycastPointBuffer.Length);
        // calculate cell center x/z, this has to be first in the array
        _raycastPointBuffer[0] = new Vector3(0.5f, 0, 0.5f);
        // calculate auxiliary raycast x/z
        _raycastPointBuffer[1] = new Vector3(0.25f, 0, 0.75f);
        _raycastPointBuffer[2] = new Vector3(0.75f, 0, 0.75f);
        _raycastPointBuffer[3] = new Vector3(0.25f, 0, 0.25f);
        _raycastPointBuffer[4] = new Vector3(0.75f, 0, 0.25f);
        // offset all of them by cell indices & other constant ones
        for (int i = 0; i < _raycastPointBuffer.Length; i++)
        {
            // Scale to cell size
            _raycastPointBuffer[i] *= grid.CellSize;
            // Offset for cell position
            _raycastPointBuffer[i].x += cellXOffsetLocal;
            _raycastPointBuffer[i].z += cellZOffsetLocal;
            // Move to Ray starting height
            _raycastPointBuffer[i].y = raycastStartY;
        }

        // transform into world space
        for(int i = 0; i < _raycastPointBuffer.Length; i++)
        {
            _raycastPointBuffer[i] = grid.transform.TransformPoint(_raycastPointBuffer[i]);
        }

        // perform raycasts & navmesh checks, aggregate results
        int hitCount = 0;
        float cellMidHeight = float.NaN;
        float cellAverageHeight = 0;

        for (int i = 0; i < _raycastPointBuffer.Length; i++)
        {
            if (Physics.Raycast(_raycastPointBuffer[i], -grid.transform.up, out RaycastHit rayHit, raycastDistance, layers))
            {
                bool navMeshFound = NavMesh.SamplePosition(rayHit.point, out NavMeshHit nmHit, 1.4f, NavMesh.AllAreas);
                if(navMeshFound)
                {
                    // Compare only horizontal distances, to ignore distance introduced by tile's slope
                    Vector3 rayHitLocal = grid.transform.InverseTransformPoint(rayHit.point);
                    Vector3 nmHitLocal = grid.transform.InverseTransformPoint(nmHit.position);

                    Vector3 flattenedRayHit = rayHitLocal;
                    flattenedRayHit.y = 0;
                    Vector3 flattenedNmHit = nmHitLocal;
                    flattenedNmHit.y = 0;
                    if (Vector3.Distance(flattenedRayHit, flattenedNmHit) <= GridComponent.CellWalkableCheckRadius)
                    {
                        hitCount++;

                        if (i == 0)
                        {
                            cellMidHeight = rayHitLocal.y;
                        }
                        cellAverageHeight += rayHitLocal.y;
                        
                        grid.GizmoDots.Add(rayHit.point);
                    }
                }
            }
        }
        cellAverageHeight /= _raycastPointBuffer.Length;

        Array.Clear(_raycastPointBuffer, 0, _raycastPointBuffer.Length);

        // decide
        bool isCellValid = hitCount >= 3;
        if (isCellValid)
        {
            if(float.IsNaN(cellMidHeight)) // Mid of cell was not walkable - use average height
            {
                return cellAverageHeight;
            }
            else
            {
                return cellMidHeight;
            }
        }
        else return -1;
    }
}
#endif
