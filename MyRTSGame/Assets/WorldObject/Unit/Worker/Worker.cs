using UnityEngine;
using RTS;
using System.Collections.Generic;

public class Worker : Unit {
	
	public int buildSpeed;
	
	private Building currentProject;
	private bool building = false;
	private float amountBuilt = 0.0f;

	public AudioClip finishedJobSound;
	public float finishedJobVolume = 1.0f;

	/*** Game Engine methods, all can be overridden by subclass ***/
	
	protected override void Start () {
		base.Start();
		actions = new string[] {"WarFactory", "OilPump","PowerPlant"};
	}
	
	protected override void Update () {
		base.Update();

		if(!moving && !rotating) {
			if(building && currentProject && currentProject.UnderConstruction()) {
				amountBuilt += buildSpeed * Time.deltaTime;
				int amount = Mathf.FloorToInt(amountBuilt);
				if(amount > 0) {
					amountBuilt -= amount;
					currentProject.Construct(amount);
					if(!currentProject.UnderConstruction()) {
						building = false;
						if(audioElement != null) audioElement.Play(finishedJobSound);
					}
				}
			}
		}
	}
	
	/*** Public Methods ***/

	public override void MouseClick (GameObject hitObject, Vector3 hitPoint, Player controller) {
		bool doBase = true;
		//only handle input if owned by a human player and currently selected
		if(player && player.humanControlled && currentlySelected && hitObject && hitObject.name!="Terrain") {
			Building building = hitObject.transform.GetComponent< Building >();
			if(building) {
				if(building.UnderConstruction()) {
					SetBuilding(building);
					doBase = false;
				}
			}
		}
		if(doBase) base.MouseClick(hitObject, hitPoint, controller);
	}

	protected override bool ShouldMakeDecision () {
		if(building) return false;
		return base.ShouldMakeDecision();
	}

	public override void SetBuilding (Building project) {
		base.SetBuilding (project);
		currentProject = project;
		StartMove(currentProject.transform.position, currentProject.gameObject);
		building = true;
	}
	
	public override void PerformAction (string actionToPerform) {
		base.PerformAction (actionToPerform);
		CreateBuilding(actionToPerform);
	}
	
	public override void StartMove(Vector3 destination) {
		base.StartMove(destination);
		building = false;
		amountBuilt = 0.0f;
	}
	
	private void CreateBuilding(string buildingName) {
		Vector3 buildPoint = new Vector3(transform.position.x, transform.position.y, transform.position.z + 10);
		if (player) { 
			player.CreateBuilding (buildingName, buildPoint, this, playingArea);
		}
	}

	protected override void InitialiseAudio () {
		base.InitialiseAudio ();
		if(finishedJobVolume < 0.0f) finishedJobVolume = 0.0f;
		if(finishedJobVolume > 1.0f) finishedJobVolume = 1.0f;
		List< AudioClip > sounds = new List< AudioClip >();
		List< float > volumes = new List< float >();
		sounds.Add(finishedJobSound);
		volumes.Add (finishedJobVolume);
		audioElement.Add(sounds, volumes);
	}

	protected override void DecideWhatToDo () {
		base.DecideWhatToDo ();
		List< WorldObject > buildings = new List< WorldObject >();
		foreach(WorldObject nearbyObject in nearbyObjects) {
			if(nearbyObject.GetPlayer() != player) continue;
			Building nearbyBuilding = nearbyObject.GetComponent< Building> ();
			if(nearbyBuilding && nearbyBuilding.UnderConstruction()) buildings.Add(nearbyObject);
		}
		WorldObject nearestObject = WorkManager.FindNearestWorldObjectInListToPosition(buildings, transform.position);
		if(nearestObject) {
			Building closestBuilding = nearestObject.GetComponent< Building >();
			if(closestBuilding) SetBuilding(closestBuilding);
		}
	}
}