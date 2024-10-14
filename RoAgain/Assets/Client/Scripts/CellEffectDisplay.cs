using Shared;
using OwlLogging;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class CellEffectData
    {
        public int GroupId;
        public CellEffectType Type;
        public GridShape Shape;

        public static CellEffectData FromPacket(CellEffectGroupPlacedPacket packet)
        {
            return new()
            {
                GroupId = packet.GroupId,
                Type = packet.Type,
                Shape = packet.Shape
            };
        }
    }

    public class CellEffectDisplay : MonoBehaviour
    {
        public List<CellEffectType> SupportedTypes;
        [SerializeField]
        private GameObject _model;

        private List<GameObject> _createdObjects = new();

        public int Initialize(CellEffectData data, GridComponent grid)
        {
            if (grid == null || grid.Data == null)
            {
                OwlLogger.LogError("Can't initialize CellEffectDisplay with empty grid!", GameComponent.Other);
            }

            if (!SupportedTypes.Contains(data.Type))
            {
                OwlLogger.LogError($"Can't initialize CellEffectDisplay {gameObject.name} with unsupported effect type {data.Type}", GameComponent.Other);
                return -1;
            }

            List<Coordinate> coordinates = data.Shape.GatherCoordinates(grid.Data);
            foreach (Coordinate coord in coordinates)
            {
                Vector3 cellPos = grid.CoordsToWorldPosition(coord);
                Vector3 localPos = transform.InverseTransformPoint(cellPos);
                _createdObjects.Add(Instantiate(_model, localPos, Quaternion.identity, transform));
            }

            return 0;
        }

        public void Shutdown()
        {
            foreach (GameObject obj in _createdObjects)
            {
                Destroy(obj);
            }
            _createdObjects.Clear();
        }
    }
}
