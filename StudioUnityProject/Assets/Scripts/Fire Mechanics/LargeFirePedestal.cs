using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LargeFirePedestal : MonoBehaviour
{
    public GameObject PlayerObject;
    public AmmoController AmmoController;

    private void Start()
    {
        //set player object
        PlayerObject = GameObject.FindGameObjectWithTag("Player");
        //get ammocontroller from player prefab
        AmmoController = PlayerObject.GetComponentInChildren<AmmoController>();
    }

    //interact with pedestal triggers refill ammo script
    public void UsePedestal(){
        AmmoController.FireAmmo = AmmoController.RefillLargePedestal(AmmoController.FireAmmo);
    }
}
