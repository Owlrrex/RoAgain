using OwlLogging;
using Shared;
using UnityEngine;
using UnityEngine.AI;

namespace Client
{
    // Does this class also manage the player's position on the Grid (= Occupancy)? Probably not - but then who does?
    public class PlayerMovement : MonoBehaviour
    {
        private NavMeshAgent _nmAgent;
        private GridEntityMover _gridMover;
        private Camera _ownCamera;

        // Start is called before the first frame update
        void Start()
        {
            _nmAgent = GetComponent<NavMeshAgent>();
            if (OwlLogger.PrefabNullCheckAndLog(_nmAgent, "nmAgent", this, GameComponent.Other))
            {
                Destroy(gameObject);
                return;
            }

            _gridMover = GetComponent<GridEntityMover>();
            if(_gridMover == null)
            {
                OwlLogger.LogError($"Player has no GridMover - destroying!", GameComponent.Other);
                Destroy(gameObject);
                return;
            }

            _ownCamera = GetComponentInChildren<Camera>();
            
        }

        // Update is called once per frame
        void Update()
        {
            NavMeshClick.Instance.Camera = _ownCamera;

            if (Input.GetMouseButtonDown(0))
            {
                if(CanClickToMove())
                {
                    if (NavMeshClick.Instance == null)
                    {
                        OwlLogger.LogError("PlayerMovement has no NavMeshInput - cannot click-move!", GameComponent.Input);
                    }
                    else
                    {
                        Coordinate mouseCoords = NavMeshClick.Instance.GetMouseGridCoords();
                        if (mouseCoords == GridData.INVALID_COORDS)
                            return;

                        MovementRequestPacket movementRequestPacket = new()
                        {
                            TargetCoordinates = mouseCoords
                        };
                        ClientMain.Instance.ConnectionToServer.Send(movementRequestPacket);
                    }
                }
            }
            else if (Input.GetMouseButton(0))
            {
                // TODO Hold-To-Move code here
            }
        }

        public bool CanClickToMove()
        {
            return !PlayerUI.Instance.IsHoveringUI(Input.mousePosition)
                && !PlayerUI.Instance.HoveredLayers.HasLayer("BlocksClickToMove");
        }
    }
}
