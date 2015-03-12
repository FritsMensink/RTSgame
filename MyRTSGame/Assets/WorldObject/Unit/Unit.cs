//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RTS;
using System;

	public class Unit : WorldObject {
		
	protected bool moving, rotating;
	public float moveSpeed, rotateSpeed;

	protected Vector3 destination;
	protected Quaternion targetRotation;

	private GameObject destinationTarget;
	public AudioClip driveSound, moveSound;
	public float driveVolume = 0.5f, moveVolume = 1.0f;

		/*** Game Engine methods, all can be overridden by subclass ***/
		
	protected override void Awake() {
		base.Awake();
	}
	
	protected override void Start () {
		base.Start();
		if (player.humanControlled) {
			EditFogOfWarTex.drawCircle ((int)Math.Ceiling (transform.position.x), (int)Math.Ceiling (transform.position.z), visiblerange);
		}
	}
	
	protected override void Update () {
		base.Update();
		curPos = transform.position;
		if(curPos != lastPos) {
			selectionBounds = ResourceManager.InvalidBounds;
			CalculateBounds ();
			if(this.visiblerange!=0 && player){
				if(player.humanControlled){
					EditFogOfWarTex.drawCircle ((int)Math.Ceiling (transform.position.x), (int)Math.Ceiling (transform.position.z), visiblerange);
				}
			}
		}
		lastPos = curPos;
		if (rotating) {
			TurnToTarget ();
		} else if (moving) {
			MakeMove ();
		}
	}
	
	protected override void OnGUI() {
		base.OnGUI();
	}
	
	public override void SetHoverState(GameObject hoverObject) {
		base.SetHoverState (hoverObject);
		//only handle input if owned by a human player and currently selected
		if (player && player.humanControlled && currentlySelected) {
			
			if (hoverObject.name == "Terrain") {
				player.hud.SetCursorState (CursorState.Move);
			}
		}
	}

	public override void MouseClick(GameObject hitObject, Vector3 hitPoint, Player controller) {
		Debug.Log ("Mouse click unit!");
		base.MouseClick(hitObject, hitPoint, controller);
		//only handle input if owned by a human player and currently selected
		if(player && player.humanControlled && currentlySelected) {
			if(hitObject.name == "Terrain" && hitPoint != ResourceManager.InvalidPosition) {
				float x = hitPoint.x;
				//makes sure that the unit stays on top of the surface it is on
				float y = hitPoint.y + this.transform.position.y;
				//player.SelectedObject.transform.position.y;
				float z = hitPoint.z;
				Vector3 destination = new Vector3(x, y, z);
				StartMove(destination);
			}
		}
	}

	public virtual void StartMove(Vector3 destination) {
		if(audioElement != null) audioElement.Play (moveSound);
		this.destination = destination;
		destinationTarget = null;
		targetRotation = Quaternion.LookRotation (destination - transform.position);
		rotating = true;
		moving = false;
		attacking = false;
	}

	public virtual void StartMove(Vector3 destination, GameObject destinationTarget) {
		StartMove(destination);
		this.destinationTarget = destinationTarget;
	}

	private void MakeMove() {
		transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * moveSpeed);
		if (transform.position == destination) {
			moving = false;
			movingIntoPosition = false;
			if(audioElement != null) { audioElement.Stop(driveSound); }
		}
		CalculateBounds();
	}

	private void TurnToTarget() {
		transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed);
		CalculateBounds();
		//sometimes it gets stuck exactly 180 degrees out in the calculation and does nothing, this check fixes that
		Quaternion inverseTargetRotation = new Quaternion(-targetRotation.x, -targetRotation.y, -targetRotation.z, -targetRotation.w);
		if(transform.rotation == targetRotation || transform.rotation == inverseTargetRotation) {
			rotating = false;
			moving = true;
			if(destinationTarget) CalculateTargetDestination();
			if(audioElement != null) audioElement.Play(driveSound);
		}
	}

	public virtual void SetBuilding(Building creator) {
		//specific initialization for a unit can be specified here
	}

	protected override void InitialiseAudio () {
		base.InitialiseAudio ();
		List< AudioClip > sounds = new List< AudioClip >();
		List< float > volumes = new List< float >();
		if(driveVolume < 0.0f) driveVolume = 0.0f;
		if(driveVolume > 1.0f) driveVolume = 1.0f;
		volumes.Add(driveVolume);
		sounds.Add(driveSound);
		if(moveVolume < 0.0f) moveVolume = 0.0f;
		if(moveVolume > 1.0f) moveVolume = 1.0f;
		sounds.Add(moveSound);
		volumes.Add(moveVolume);
		audioElement.Add(sounds, volumes);
	}

	private void CalculateTargetDestination() {
		//calculate number of unit vectors from unit centre to unit edge of bounds
		Vector3 originalExtents = selectionBounds.extents;
		Vector3 normalExtents = originalExtents;
		normalExtents.Normalize();
		float numberOfExtents = originalExtents.x / normalExtents.x;
		int unitShift = Mathf.FloorToInt(numberOfExtents);
		
		//calculate number of unit vectors from target centre to target edge of bounds
		WorldObject worldObject = destinationTarget.GetComponent< WorldObject >();
		if(worldObject) originalExtents = worldObject.GetSelectionBounds().extents;
		else originalExtents = new Vector3(0.0f, 0.0f, 0.0f);
		normalExtents = originalExtents;
		normalExtents.Normalize();
		numberOfExtents = originalExtents.x / normalExtents.x;
		int targetShift = Mathf.FloorToInt(numberOfExtents);
		
		//calculate number of unit vectors between unit centre and destination centre with bounds just touching
		int shiftAmount = targetShift + unitShift;
		
		//calculate direction unit needs to travel to reach destination in straight line and normalize to unit vector
		Vector3 origin = transform.position;
		Vector3 direction = new Vector3(destination.x - origin.x, 0.0f, destination.z - origin.z);
		direction.Normalize();
		
		//destination = center of destination - number of unit vectors calculated above
		//this should give us a destination where the unit will not quite collide with the target
		//giving the illusion of moving to the edge of the target and then stopping
		for(int i = 0; i < shiftAmount; i++) destination -= direction;
		destination.y = destinationTarget.transform.position.y;
	}
}


