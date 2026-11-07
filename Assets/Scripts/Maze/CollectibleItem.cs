using UnityEngine;

namespace MazeGame
{
    public class CollectibleItem : MonoBehaviour
    {
        public bool isExit = false;
        private Vector3 _startPos;
        private float _timeOffset;

        private void Start()
        {
            _startPos = transform.position;
            _timeOffset = Random.Range(0f, 10f);
        }

        private void Update()
        {
            float t = Time.time * 3f + _timeOffset;
            if (isExit)
            {
                // Pulsing size and rotation for portal
                float pulse = 1f + Mathf.Sin(t * 1.5f) * 0.12f;
                transform.localScale = new Vector3(pulse, pulse, 1f);
                transform.Rotate(0, 0, 45f * Time.deltaTime);
            }
            else
            {
                // Bobbing and spinning for coins
                float bob = Mathf.Sin(t) * 0.08f;
                transform.position = _startPos + new Vector3(0, bob, 0);

                // Horizontal scale squeeze to simulate 3D coin spin
                float coinSpin = Mathf.Abs(Mathf.Cos(t * 1.2f));
                transform.localScale = new Vector3(Mathf.Max(0.15f, coinSpin), 1f, 1f);
            }
        }
    }
}
