using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioLibrary : MonoBehaviour
{
    public static AudioLibrary instance;
    public AudioSource MusicAudioSource;
    public AudioClip QuirkyBossa;
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        instance.MusicAudioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
