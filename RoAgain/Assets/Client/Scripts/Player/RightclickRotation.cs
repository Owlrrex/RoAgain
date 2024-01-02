using OwlLogging;
using UnityEngine;

namespace Client
{
	public class RightclickRotation : MonoBehaviour
	{

		public float horiRotateSpeed = 120;
		public float vertRotateSpeed = 60;

		[SerializeField]
		private GameObject _cameraAnchor;
        [SerializeField]
        private GameObject _mainCamera;

		public float upperViewLimit = 70;
		public float lowerViewLimit = 20;

		public float zoomSpeed = 300;
		public float zoomNearLimit = 10;
		public float zoomFarLimit = 30;

		// Use this for initialization
		void Start()
		{
			OwlLogger.PrefabNullCheckAndLog(_cameraAnchor, "cameraAnchor", this, GameComponent.Input);
			OwlLogger.PrefabNullCheckAndLog(_mainCamera, "mainCamera", this, GameComponent.Input);
		}

		// Update is called once per frame
		void Update()
		{
			if (Input.GetMouseButton(1))
			{
				_cameraAnchor.transform.localEulerAngles += new Vector3(0, horiRotateSpeed * Input.GetAxis("Mouse X") * Time.deltaTime, 0);

                if (Input.GetKey(KeyCode.LeftShift))
				{
					float rotationAllowed = 1;
					float inputY = Input.GetAxis("Mouse Y");
					if (inputY > 0 && _mainCamera.transform.eulerAngles.x < lowerViewLimit)
					{
						rotationAllowed = 0;
					}
					else if (inputY < 0 && _mainCamera.transform.eulerAngles.x > upperViewLimit)
					{
						rotationAllowed = 0;
					}

					float verticalRot = -vertRotateSpeed * Input.GetAxis("Mouse Y") * Time.deltaTime * rotationAllowed;
					_cameraAnchor.transform.localEulerAngles += new Vector3(verticalRot, 0, 0);
				}
			}

			if (Input.GetAxis("Mouse ScrollWheel") != 0
				&& !PlayerUI.Instance.IsHoveringUI(Input.mousePosition))
			{
				float distance = (_mainCamera.transform.position - _cameraAnchor.transform.position).magnitude;
				float zoomAllowed = 1;
				if (distance > zoomFarLimit && Input.GetAxis("Mouse ScrollWheel") < 0)
				{
					zoomAllowed = 0;
				}
				else if (distance < zoomNearLimit && Input.GetAxis("Mouse ScrollWheel") > 0)
				{
					zoomAllowed = 0;
				}
				_mainCamera.transform.Translate(new Vector3(0, 0, Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * zoomSpeed * zoomAllowed));

			}

		}
	}
}
