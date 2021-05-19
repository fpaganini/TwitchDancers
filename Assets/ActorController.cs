using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ActorController : MonoBehaviour
{
    private AudioSource audiosource;
    private Animator animator;

    public Transform Head;
    public string avatarUrl;

    public enum Musics
    {
        QuirkyBossa
    }

    bool isMoving = false;
    bool isDancing = false;
    public bool Ended = false;

    public Musics musicToPlay;
    // Start is called before the first frame update
    void Start()
    {
        audiosource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!Ended)
        {
            if (isMoving && textureLoaded)
            {
                transform.Translate(0, 0, 1f * Time.deltaTime, Space.Self);
                if (transform.position.z >= 0 && isDancing == false)
                {
                    isMoving = false;
                    isDancing = true;
                    animator.SetBool("Wait", true);
                    animator.SetTrigger("QuirkyBossa");

                }
                else if (transform.position.z >= 8 && isDancing == true)
                {
                    Ended = true;
                }

            }
        }
    }

    public void Inicializa() {
        StartCoroutine(GetTexture(avatarUrl));
    }

    bool textureLoaded = false;

    IEnumerator GetTexture(string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError(www.error);
        }
        else {
            Texture myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            if(Head == null)
                Head = this.gameObject.transform.Find("mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:Neck/mixamorig:Head/mixamorig:HeadTop_End/CubeHead");
            var renderer = Head.GetComponent<Renderer>();
            renderer.material.mainTexture = myTexture;
            textureLoaded = true;
        }
    }

    public void Rotate_90()
    {
        this.gameObject.transform.Rotate(0f, -90f, 0f);
    }
    public void Rotate90()
    {
        this.gameObject.transform.Rotate(0f, 90f, 0f);
    }

    public void PlayMusic()
    {
        if (musicToPlay == Musics.QuirkyBossa)
        {
            AudioLibrary.instance.MusicAudioSource.PlayOneShot(AudioLibrary.instance.QuirkyBossa);
        }
    }

    public void Moving()
    {
        isMoving = true;
    }


}
