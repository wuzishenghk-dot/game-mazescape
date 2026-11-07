using UnityEngine;

namespace MazeGame
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smoothSpeed = 0.15f;
        public Vector2 minBounds;
        public Vector2 maxBounds;
        public bool useBounds = false;

        private Vector3 _velocity = Vector3.zero;

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, TargetPosition(), ref _velocity, smoothSpeed);
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            _velocity = Vector3.zero;
            transform.position = TargetPosition();
        }

        private Vector3 TargetPosition()
        {

            Vector3 targetPos = new Vector3(target.position.x, target.position.y, -10f);

            if (useBounds)
            {
                Camera cam = GetComponent<Camera>();
                float vertExtent = cam != null ? cam.orthographicSize : 5f;
                float horzExtent = vertExtent * (cam != null ? cam.aspect : (float)Screen.width / Screen.height);

                float clampedX = Mathf.Clamp(targetPos.x, minBounds.x + horzExtent, maxBounds.x - horzExtent);
                float clampedY = Mathf.Clamp(targetPos.y, minBounds.y + vertExtent, maxBounds.y - vertExtent);

                // If bounds are smaller than view, center instead
                if (minBounds.x + horzExtent > maxBounds.x - horzExtent)
                    clampedX = (minBounds.x + maxBounds.x) * 0.5f;
                if (minBounds.y + vertExtent > maxBounds.y - vertExtent)
                    clampedY = (minBounds.y + maxBounds.y) * 0.5f;

                targetPos.x = clampedX;
                targetPos.y = clampedY;
            }

            return targetPos;
        }

        public void SetBounds(float minX, float minY, float maxX, float maxY)
        {
            minBounds = new Vector2(minX, minY);
            maxBounds = new Vector2(maxX, maxY);
            useBounds = true;
        }
    }
}
