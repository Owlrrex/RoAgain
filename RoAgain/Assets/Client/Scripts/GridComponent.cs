using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Client
{
    public class GridComponent : MonoBehaviour
    {
        public GridData Data { get; private set; }

        public int CellSize = 1;

        public GameObject FinalPathIndicator;

#if UNITY_EDITOR
        public List<Vector3> GizmoDots { get; private set; } = new();

        public string EditorMapId;
        public Vector2Int EditorBounds;

        public static float CellWalkableCheckRadius = 0.1f;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            foreach (Vector3 dot in GizmoDots)
            {
                Gizmos.DrawWireSphere(dot, 0.1f);
            }
        }
#endif

        // Start is called before the first frame update
        void Start()
        {
            if (CellSize <= 0)
            {
                OwlLogger.LogError("Grid CellSize must be a positive number!", GameComponent.Grid);
                CellSize = 1;
            }

            // tmp
            //Initialize(DebugBounds);
            //GridData.Path path = GridData.FindPath(new(10, 10), new(5, 1));
            //foreach (Vector2Int finalPathCoord in path.AllCells)
            //{
            //    Instantiate(FinalPathIndicator, CoordsToWorldPosition(finalPathCoord), Quaternion.identity);
            //}
        }

        public int Initialize(string mapId)
        {
            if(Data == null)
            {
                Data = new();
            }

            Data = GridData.LoadMapFromFiles(mapId);
            if(Data == null)
            {
                OwlLogger.LogError($"Initialization of GridComponent for map {mapId} failed!", GameComponent.Grid);
                return -1;
            }
            return 0;
        }

        public Vector2Int FreePosToGridCoords(Vector3 freePos)
        {
            Vector3 localFreePos = transform.worldToLocalMatrix.MultiplyPoint(freePos);
            Vector2Int candidateCoords = Vector2Int.zero;
            candidateCoords.x = Mathf.CeilToInt(localFreePos.x / CellSize);
            candidateCoords.y = Mathf.CeilToInt(localFreePos.z / CellSize);

            if (!Data.AreCoordinatesValid(candidateCoords))
            {
                return GridData.INVALID_COORDS;
            }

            if (Data.GetDataAtCoords(candidateCoords).IsVoidCell())
            {
                return GridData.INVALID_COORDS;
            }

            return candidateCoords;
        }

        public Vector3 CoordsToWorldPosition(Vector2Int coords)
        {
            if (!Data.AreCoordinatesValid(coords))
                return Vector3.negativeInfinity;

            float halfCell = CellSize / 2.0f;

            Vector3 position = Vector3.zero;
            position.x = CellSize * (coords.x - 1) + halfCell;
            position.z = CellSize * (coords.y - 1) + halfCell;
            position.y = Data.GetDataAtCoords(coords).CellHeight;

            return transform.localToWorldMatrix.MultiplyPoint(position);
        }

        public Vector3 SnapPositionToGrid(Vector3 freePos)
        {
            Vector2Int coords = FreePosToGridCoords(freePos);
            if (coords == GridData.INVALID_COORDS)
            {
                return Vector3.negativeInfinity;
            }

            Vector3 snappedPos = CoordsToWorldPosition(coords);
            if (snappedPos == Vector3.negativeInfinity)
            {
                return Vector3.negativeInfinity;
            }

            return snappedPos;
        }
    }
}

