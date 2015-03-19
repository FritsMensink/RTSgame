using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RTS;
public class AI : Player {

	public Vector3 spawnpoint= new Vector3(14,-6,23);

	// Use this for initialization
	protected override void Start(){
		base.Start ();
		//AddUnit ("Worker",spawnpoint,spawnpoint,new Quaternion(0,0,0,1));
	}
	
	// Update is called once per frame
	protected override void Update () {
		base.Update ();
		spamUnits ();
	}


	void spamUnits(){
		//CreateBuilding()
		//getbuilding
		//building create 5 units
	}
}
