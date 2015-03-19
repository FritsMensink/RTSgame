using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RTS;
public class AI : Player {



	// Use this for initialization
	protected override void Start(){
		base.Start ();
		AddUnit ("Worker",Vector3.zero, Vector3.zero ,new Quaternion(0,0,0,1));
	}
	
	// Update is called once per frame
	protected override void Update () {
		base.Update ();
	}
	
}
