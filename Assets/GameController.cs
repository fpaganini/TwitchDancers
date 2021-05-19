using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameController : MonoBehaviour
{

    public GameObject DancerPrephab;
    // Start is called before the first frame update
    void Start()
    {
        TwitchConnection.instance.Inialize();
        StartCoroutine(VerificaFila());
    }

    public IEnumerator VerificaFila() {
        while (true)
        {
            yield return new WaitForSeconds(1);
            var item = TwitchConnection.GetFirst();
            if (item != null)
            {
                var obj = Instantiate(DancerPrephab, new Vector3(0f, 0f, -5f), Quaternion.identity);
                ActorController act = obj.GetComponent<ActorController>();
                act.avatarUrl = item.avatar;
                act.Inicializa();
                while(!act.Ended)
                {
                    yield return new WaitForSeconds(1);
                }
                GameObject.Destroy(obj);
                yield return new WaitForSeconds(10);
                
            }
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
