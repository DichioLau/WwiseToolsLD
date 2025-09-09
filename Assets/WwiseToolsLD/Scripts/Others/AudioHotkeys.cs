using UnityEngine;

public class AudioHotkeys : MonoBehaviour
{
    [SerializeField] private AKLD_SOTemplate audioTest;


    private void Start()
    {
        audioTest.Layer1(gameObject);
        audioTest.MusicValue(gameObject, 75f);
       
    }

    void Update()
    {
        //Event

        if (Input.GetKeyDown(KeyCode.Alpha1))
            audioTest.EventNumber1(gameObject);


        if (Input.GetKeyDown(KeyCode.Alpha2))
            audioTest.EventNumber2(gameObject);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            audioTest.EventNumber3(gameObject);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            audioTest.EventNumber4(gameObject);

        if (Input.GetKeyDown(KeyCode.M))
            audioTest.Music(this.gameObject);



        //STATE
        if (Input.GetKeyDown(KeyCode.Q))
            audioTest.Layer1(gameObject);

        if (Input.GetKeyDown(KeyCode.W))
            audioTest.Layer2(gameObject);

        if (Input.GetKeyDown(KeyCode.E))
            audioTest.Layer3(gameObject);

        if (Input.GetKeyDown(KeyCode.R))
            audioTest.Layer4(gameObject);


        //RTPC

        if (Input.GetKeyDown(KeyCode.A))
            audioTest.MusicValue(this.gameObject, 100);

        if (Input.GetKeyDown(KeyCode.S))
            audioTest.MusicValue(this.gameObject, 50);


        if (Input.GetKeyDown(KeyCode.D))
        {
            audioTest.MusicValue(this.gameObject, 10);
            Debug.Log("volumen a 10");
        }
    }
}
