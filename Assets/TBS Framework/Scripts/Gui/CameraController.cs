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
        public float MinZoom = 20;
        public float MaxZoom = 60;
        public Vector2 MinPosition;
        public Vector2 MaxPosition;

        void Update()
        {
            HandleMovement();
            HandleZoom();
        }

        private void HandleMovement()
        {
            Vector3 newPosition = transform.position;

            if (Input.GetKey("d"))
            {
                newPosition += transform.right * Time.deltaTime * ScrollSpeed;
            }
            else if (Input.GetKey("a"))
            {
                newPosition += transform.right * Time.deltaTime * -ScrollSpeed;
            }
            if (Input.GetKey("w"))
            {
                newPosition += transform.up * Time.deltaTime * ScrollSpeed;
            }
            else if (Input.GetKey("s"))
            {
                newPosition += transform.up * Time.deltaTime * -ScrollSpeed;
            }

            // Clamp the new position within the defined bounds
            newPosition.x = Mathf.Clamp(newPosition.x, MinPosition.x, MaxPosition.x);
            newPosition.y = Mathf.Clamp(newPosition.y, MinPosition.y, MaxPosition.y);

            transform.position = newPosition;
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0.0f)
            {
                Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView - scroll * ZoomSpeed, MinZoom, MaxZoom);
            }

            if (Input.GetKey("q"))
            {
                Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView - ZoomSpeed * Time.deltaTime, MinZoom, MaxZoom);
            }
            else if (Input.GetKey("e"))
            {
                Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView + ZoomSpeed * Time.deltaTime, MinZoom, MaxZoom);
            }
        }

        public void MoveToTarget(Transform target)
        {
            Vector3 targetPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = targetPosition;
        }
    }
}