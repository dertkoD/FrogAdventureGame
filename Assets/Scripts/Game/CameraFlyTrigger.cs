using UnityEngine;
using UnityEngine.Playables;

public class CameraFlyTrigger : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool oneShot = true;

    private bool used;

    private void OnTriggerEnter(Collider other)
    {
        if (oneShot && used) return;
        if (!other.CompareTag(playerTag)) return;
        if (!director) return;

        used = true;

        director.Stop();    
        director.time = 0;
        director.Evaluate();
        director.Play();
    }
}
