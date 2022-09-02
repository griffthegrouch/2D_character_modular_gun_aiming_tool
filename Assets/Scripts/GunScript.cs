using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunScript : MonoBehaviour
{
    // different types of guns that would be fun
    // missle launcher
    // laser gun/rifle
    // 

    //one handed bool

    //semi auto, full auto, burst

    public GameObject _ammo;

    //semi auto, full auto, burst
    //0-none, 1-semi, 2-full auto, 3-burst
    public int _firingMode = 2;
    public int _clipSize = 45;

    public float _reloadTime = 0.5f;

    //bullet speed/power
    //bullet size

    public float _delayTime = 0.0f;
    //burst delay



    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public (GameObject, int, int, float, float) GetVars(){
        //function returns all info about the gun
        return (_ammo, _firingMode, _clipSize, _reloadTime, _delayTime);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}