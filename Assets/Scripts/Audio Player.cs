using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    public AudioClip introBGM;     
    public AudioClip ghostNormalBGM;

    private AudioSource audioSource;
    private bool hasSwitched = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
    
        audioSource.clip = introBGM;
        audioSource.loop = false; 
        audioSource.Play();

 
        Invoke(nameof(SwitchToLoopBGM), 3f);
    }

    void Update()
    {

        if (!hasSwitched && !audioSource.isPlaying)
        {
            SwitchToLoopBGM();
        }
    }

    void SwitchToLoopBGM()
    {
        if (hasSwitched) return;
        hasSwitched = true;

        audioSource.clip = ghostNormalBGM;
        audioSource.loop = true; 
        audioSource.Play();
    }
}
