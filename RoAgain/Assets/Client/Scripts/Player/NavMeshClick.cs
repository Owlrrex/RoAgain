using OwlLogging;
using UnityEngine;
using UnityEngine.AI;

namespace Client
{
    public class NavMeshClick : MonoBehaviour
    {
        public static NavMeshClick Instance;

        private bool _hasQueriedThisFrame;
        private Vector2Int _mouseCoordCache;

        [SerializeField]
        private GameObject _clickIndicator;
        [SerializeField]
        private GameObject _navIndicator;

        [SerializeField]
        private LayerMask _clickableLayers;

        private const bool _showHoverIndicator = false;
        private GameObject _indicatorInstance;
        private GameObject _clickInstance;

        public Camera Camera;

        // Start is called before the first frame update
        void Start()
        {
            if (Instance != null)
            {
                OwlLogger.LogError($"Duplicate NavMeshClick instance!", GameComponent.Input);
                Destroy(this);
                return;
            }

            Instance = this;

            if(OwlLogger.PrefabNullCheckAndLog(_clickIndicator, "clickIndicator", this, GameComponent.Input))
            {
                Destroy(gameObject);
            }

            if (OwlLogger.PrefabNullCheckAndLog(_navIndicator, "navIndicator", this, GameComponent.Input))
            {
                Destroy(gameObject);
            }

            _indicatorInstance = Instantiate(_navIndicator, gameObject.transform);
            _indicatorInstance.SetActive(false);

            if(_showHoverIndicator)
                _clickInstance = Instantiate(_clickIndicator, gameObject.transform);
        }

        // Update is called once per frame
        void Update()
        {
            _hasQueriedThisFrame = false;

            //hovering indicator, temporary here
            if (_indicatorInstance == null)
                return;

            if (ClientMain.Instance.MapModule.Grid == null)
                return;

            if (Camera == null)
                return;

            Vector2Int mouseGridPos = GetMouseGridCoords();
            if (mouseGridPos == GridData.INVALID_COORDS)
            {
                _indicatorInstance.SetActive(false);
                return;
            }

            Vector3 gridCellPos = ClientMain.Instance.MapModule.Grid.CoordsToWorldPosition(mouseGridPos);

            if (gridCellPos.Equals(Vector3.negativeInfinity))
            {
                _indicatorInstance.SetActive(false);
                return;
            }

            _indicatorInstance.transform.position = gridCellPos;
            _indicatorInstance.SetActive(true);
        }

        private void PerformQuery()
        {
            if (ClientMain.Instance != null && ClientMain.Instance.MapModule?.Grid == null)
            {
                OwlLogger.LogError("NavMeshClick doesn't have a current Grid!", GameComponent.Grid);
                return;
            }

            _hasQueriedThisFrame = true;

            _mouseCoordCache = GridData.INVALID_COORDS;
            
            Vector3 mousePos = Input.mousePosition;

            if (PlayerUI.Instance.IsHoveringUI(mousePos))
                return;


            Ray ray = Camera.ScreenPointToRay(mousePos);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100, _clickableLayers))
                return;

            if(hit.collider.gameObject.layer == LayerMask.NameToLayer("ClickableTerrain"))
            {
                if(_showHoverIndicator)
                    _clickInstance.transform.position = hit.point;

                if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 1, NavMesh.AllAreas))
                    return;

                _mouseCoordCache = ClientMain.Instance.MapModule.Grid.FreePosToGridCoords(navHit.position);
            }
            else if(hit.collider.gameObject.layer == LayerMask.NameToLayer("ClickableObject"))
            {
                // Get Objects entity component (of some sort): GridEntityMover? BattleEntityModelComponent? A BattleEntityModelComponent-equivalent for general GridEntities?
                // Get component's entity coordinates, set _mouseCoordCache
                // Interact with component
            }
            
        }

        public Vector2Int GetMouseGridCoords()
        {
            if (!_hasQueriedThisFrame)
                PerformQuery();

            return _mouseCoordCache;
        }
    }
}
