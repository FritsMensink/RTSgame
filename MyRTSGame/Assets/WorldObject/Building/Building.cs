using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RTS;
using System;

public class Building : WorldObject {

	public float maxBuildProgress;
	protected Queue< string > buildQueue;
	private float currentBuildProgress = 0.0f;
	private Vector3 spawnPoint;
	protected Vector3 rallyPoint;
	public Texture2D rallyPointImage;
	public Texture2D sellImage;

	public bool needsBuilding = false;
		
	public AudioClip finishedJobSound;
	public float finishedJobVolume = 1.0f;

	protected override void Awake ()
	{
		base.Awake ();

		buildQueue = new Queue< string >();

		 SetSpawnPoint();
	}
	// Use this for initialization
	protected override void Start () {
		base.Start ();
	}
	
	// Update is called once per frame
	protected override void Update () {
		base.Update ();

		ProcessBuildQueue ();
	
	}

	protected override void OnGUI() {
		base.OnGUI ();
		if (needsBuilding) {
			DrawBuildProgress ();
		}
	}

	protected void CreateUnit(string unitName) {
		buildQueue.Enqueue(unitName);
	}

	protected void ProcessBuildQueue() {
		if(buildQueue.Count > 0) {
			currentBuildProgress += Time.deltaTime * ResourceManager.BuildSpeed;
			if(currentBuildProgress > maxBuildProgress) {
				if(player) {
					
					if(audioElement != null) audioElement.Play(finishedJobSound);
					player.AddUnit(buildQueue.Dequeue(), spawnPoint, rallyPoint, transform.rotation);
				}
				currentBuildProgress = 0.0f;
			}
		}
	}

	public string[] getBuildQueueValues() {
		string[] values = new string[buildQueue.Count];
		int pos = 0;
		foreach(string unit in buildQueue) values[pos++] = unit;
		return values;
	}
	
	public float getBuildPercentage() {
		return currentBuildProgress / maxBuildProgress;
	}

	protected override bool ShouldMakeDecision () {
		return false;
	}

	public override void SetSelection(bool selected, Rect playingArea) {
		base.SetSelection(selected, playingArea);
		if(player) {
			RallyPoint flag = player.GetComponentInChildren< RallyPoint >();
			if(selected) {
				if(flag && player.humanControlled && spawnPoint != ResourceManager.InvalidPosition && rallyPoint != ResourceManager.InvalidPosition) {
					flag.transform.localPosition = rallyPoint;
					flag.transform.forward = transform.forward;
					flag.Enable();
				}
			} else {
				if(flag && player.humanControlled) { flag.Disable(); }
			}
		}
	}

	public bool hasSpawnPoint() {
		return spawnPoint != ResourceManager.InvalidPosition && rallyPoint != ResourceManager.InvalidPosition && hasRallyPoint();
	}

	public override void SetHoverState(GameObject hoverObject) {
		base.SetHoverState(hoverObject);
		//only handle input if owned by a human player and currently selected
		if(player && player.humanControlled && currentlySelected) {
			if(hoverObject.name == "Terrain") {
				if(player.hud.GetPreviousCursorState() == CursorState.RallyPoint) player.hud.SetCursorState(CursorState.RallyPoint);
			}
		}
	}

	public override void MouseClick(GameObject hitObject, Vector3 hitPoint, Player controller) {
		base.MouseClick(hitObject, hitPoint, controller);
		//only handle iput if owned by a human player and currently selected
		if(player && player.humanControlled && currentlySelected) {
			if(hitObject.name == "Terrain") {
				if((player.hud.GetCursorState() == CursorState.RallyPoint || player.hud.GetPreviousCursorState() == CursorState.RallyPoint) && hitPoint != ResourceManager.InvalidPosition) {
					SetRallyPoint(hitPoint);
				}
			}
		}
	}

	public void SetRallyPoint(Vector3 position) {
		if (hasRallyPoint()) {
			rallyPoint = position;
			//rallyPoint.x = rallyPoint.x - 5;
			if (player && player.humanControlled && currentlySelected) {
				RallyPoint flag = player.GetComponentInChildren< RallyPoint > ();
				if (flag) {
					flag.transform.localPosition = rallyPoint;
				}
			}
		}
	}

	public virtual bool hasRallyPoint() {
		return false;
	}

	public void Sell() {
		if (player) {
			player.AddResource (ResourceType.Money, sellValue);
			player.AddResource (ResourceType.Power, powerUsage);
		}
		if (currentlySelected) {
			SetSelection (false, playingArea);
			UserInput.CurrentlySelectedWorldObjects = new ArrayList();
			Destroy (this.gameObject);
		}
	}

	public void StartConstruction() {
		CalculateBounds();
		needsBuilding = true;
		hitPoints = 1;
	}

	private void DrawBuildProgress() {
		GUI.skin = ResourceManager.SelectionBoxSkin;
		Rect selectBox = WorkManager.CalculateSelectionBox(selectionBounds, playingArea);
		//Draw the selection box around the currently selected object, within the bounds of the main draw area
		GUI.BeginGroup(playingArea);
		CalculateCurrentHealth(0.5f, 0.99f);
		DrawHealthBar(selectBox, "Building ...");
		GUI.EndGroup();
	}

	public bool UnderConstruction() {
		return needsBuilding;
	}
	
	public void Construct(int amount) {
		hitPoints += amount;
		if(hitPoints >= maxHitPoints) {
			hitPoints = maxHitPoints;
			needsBuilding = false;
			RestoreMaterials();
			SetTeamColor();
		}
		SetSpawnPoint ();
	}

	private void SetSpawnPoint(){

		if (hasRallyPoint()) {
			float spawnX = selectionBounds.center.x + transform.forward.x * selectionBounds.extents.x + transform.forward.x * 2;
			float spawnZ = selectionBounds.center.z + transform.forward.z * selectionBounds.extents.z + transform.forward.z * 2;
			spawnPoint = new Vector3 (spawnX, 0.0f, spawnZ);
			//set de rally point van het gebouw of de default spawnpoint
			rallyPoint = spawnPoint;
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
}
