using UnityEngine;
using UnityEngine.Playables;

public class CameraFlyTrigger : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool oneShot = true;

    private bool used;
    private PlayerControlLock currentLock;

    private void OnEnable()
    {
        if (!director) return;
        director.played += OnDirectorPlayed;
        director.stopped += OnDirectorStopped;
        director.paused += OnDirectorStopped;
    }

    private void OnDisable()
    {
        if (!director) return;
        director.played -= OnDirectorPlayed;
        director.stopped -= OnDirectorStopped;
        director.paused -= OnDirectorStopped;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (oneShot && used) return;
        if (!other.CompareTag(playerTag)) return;
        if (!director) return;

        used = true;

        currentLock = other.GetComponentInParent<PlayerControlLock>();
        if (currentLock) currentLock.SetLocked(true);

        director.Stop();
        director.time = 0;
        director.Evaluate();
        director.Play();
    }

    private void OnDirectorPlayed(PlayableDirector d)
    {
        if (currentLock && !currentLock.IsLocked)
            currentLock.SetLocked(true);
    }

    private void OnDirectorStopped(PlayableDirector d)
    {
        if (currentLock)
        {
            currentLock.SetLocked(false);
            currentLock = null;
        }
    }
}
