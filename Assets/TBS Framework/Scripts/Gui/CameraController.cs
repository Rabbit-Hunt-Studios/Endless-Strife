using UnityEngine;

namespace TbsFramework.Gui
{
    /// <summary>
    /// Simple movable camera implementation.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public float ScrollSpeed = 15;
        public float ScrollEdge = 0.01f;
        public float ZoomSpeed = 10;
        public float MinZoom = 5;
        public float MaxZoom = 20;

        void Update()
        {
            HandleMovement();
            HandleZoom();
        }

        private void HandleMovement()
        {
            if (Input.GetKey("d") || Input.mousePosition.x >= Screen.width * (1 - ScrollEdge))
            {
                transform.Translate(transform.right * Time.deltaTime * ScrollSpeed, Space.World);
            }
            else if (Input.GetKey("a") || Input.mousePosition.x <= Screen.width * ScrollEdge)
            {
                transform.Translate(transform.right * Time.deltaTime * -ScrollSpeed, Space.World);
            }
            if (Input.GetKey("w") || Input.mousePosition.y >= Screen.height * (1 - ScrollEdge))
            {
                transform.Translate(transform.up * Time.deltaTime * ScrollSpeed, Space.World);
            }
            else if (Input.GetKey("s") || Input.mousePosition.y <= Screen.height * ScrollEdge)
            {
                transform.Translate(transform.up * Time.deltaTime * -ScrollSpeed, Space.World);
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0.0f)
            {
                Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - scroll * ZoomSpeed, MinZoom, MaxZoom);
            }

            if (Input.GetKey("q"))
            {
                Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - ZoomSpeed * Time.deltaTime, MinZoom, MaxZoom);
            }
            else if (Input.GetKey("e"))
            {
                Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize + ZoomSpeed * Time.deltaTime, MinZoom, MaxZoom);
            }
        }
    }
}