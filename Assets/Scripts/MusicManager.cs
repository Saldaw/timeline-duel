using UnityEngine;

public class MusicManager : MonoBehaviour {
  private static MusicManager instance;
  private AudioSource audioSource;

  void Awake() {
    if (instance != null && instance != this) {
      Destroy(gameObject);
      return;
    }

    instance = this;
    DontDestroyOnLoad(gameObject);

    audioSource = GetComponent<AudioSource>();
    if (!audioSource.isPlaying)
      audioSource.Play();
  }

  public void SetVolume(float volume) {
    audioSource.volume = volume;
  }

  public void StopMusic() {
    audioSource.Stop();
  }

  public void PlayMusic() {
    if (!audioSource.isPlaying)
      audioSource.Play();
  }
}
