using UnityEngine;

public class Avacon25 : MonoBehaviour
{
    [SerializeField] private AKLD_SOTemplate Avacon;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Avacon.AUDIOO1(this.gameObject);
        Avacon.AUDIOO2(this.gameObject);
       

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
