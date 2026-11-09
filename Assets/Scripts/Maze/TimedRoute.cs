using UnityEngine;
namespace MazeGame
{
    public class TimedRoute : MonoBehaviour
    {
        private float _remaining=8f;
        private void Update(){_remaining-=Time.deltaTime;if(_remaining<=0)Destroy(gameObject);}
    }
}
