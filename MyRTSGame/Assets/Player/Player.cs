using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RTS;

public class Player : MonoBehaviour {

	public string username;
	public int startMoney, startMoneyLimit, startPower, startPowerLimit;
	private Dictionary< ResourceType, int > resources, resourceLimits;
	public bool humanControlled;
	public HUD hud;
	public AudioClip noMoney, lowPower;
	public float noMoney_volume =1f, lowPower_volume =1f;

	//Team kleur
	public Color teamColor;

	//voor het bouwen
	public Material notAllowedMaterial, allowedMaterial;

	private Building tempBuilding;
	private Unit tempCreator;
	private bool findingPlacement = false;
	protected AudioElement audioElement;
	//public WorldObject SelectedObject { get; set; }

	protected virtual void Awake() {
		resources = InitResourceList();
		resourceLimits = InitResourceList();
	}

	// Use this for initialization
	protected virtual void Start (){
		hud = GetComponentInChildren<HUD> ();
		AddStartResourceLimits();
		AddStartResources();
		InitialiseAudio();
	}
	
	// Update is called once per frame
	protected virtual void Update () {
		if(humanControlled) {
			hud.SetResourceValues(resources, resourceLimits);
		}

		if(findingPlacement) {
			tempBuilding.CalculateBounds();
			if(CanPlaceBuilding()) tempBuilding.SetTransparentMaterial(allowedMaterial, false);
			else tempBuilding.SetTransparentMaterial(notAllowedMaterial, false);
		}
	}

	private Dictionary< ResourceType, int > InitResourceList() {
		Dictionary< ResourceType, int > list = new Dictionary< ResourceType, int >();
		list.Add(ResourceType.Money, 0);
		list.Add(ResourceType.Power, 0);
		return list;
	}

	private void AddStartResourceLimits() {
		IncrementResourceLimit(ResourceType.Money, startMoneyLimit);
		IncrementResourceLimit(ResourceType.Power, startPowerLimit);
	}
	
	private void AddStartResources() {
		AddResource(ResourceType.Money, startMoney);
		AddResource(ResourceType.Power, startPower);
	}

	public void AddResource(ResourceType type, int amount) {
		resources[type] += amount;
	}
	
	public void IncrementResourceLimit(ResourceType type, int amount) {
		resourceLimits[type] += amount;
	}

	public void AddUnit(string unitName, Vector3 spawnPoint, Vector3 rallyPoint, Quaternion rotation) {
			Units units = GetComponentInChildren< Units > ();
			GameObject newUnit = (GameObject)Instantiate (ResourceManager.GetUnit (unitName), spawnPoint, rotation);
			newUnit.transform.parent = units.transform;
			Unit unitObject = newUnit.GetComponent< Unit > ();
		if (!(GetResourceAmount(ResourceType.Money)-unitObject.cost <0)) {
			AddResource (ResourceType.Money, -unitObject.cost);
			if (unitObject && spawnPoint != rallyPoint) {
				unitObject.StartMove (rallyPoint);
			}
		}else{
			audioElement.Play(noMoney);
			Destroy(newUnit);
		}
	}

	public void CreateBuilding(string buildingName, Vector3 buildPoint, Unit creator, Rect playingArea) {
		GameObject newBuilding = (GameObject)Instantiate(ResourceManager.GetBuilding(buildingName), buildPoint, new Quaternion());
		tempBuilding = newBuilding.GetComponent< Building >();
		if (tempBuilding) {
			tempCreator = creator;
			findingPlacement = true;
			tempBuilding.SetTransparentMaterial(notAllowedMaterial, true);
			tempBuilding.SetColliders(false);
			tempBuilding.SetPlayingArea(playingArea);
		} else Destroy(newBuilding);
	}

	public bool IsFindingBuildingLocation() {
		return findingPlacement;
	}
	
	public void FindBuildingLocation() {
		Vector3 newLocation = WorkManager.FindHitPoint(Input.mousePosition);
		newLocation.y = 0;
		tempBuilding.transform.position = newLocation;
	}

	public bool CanPlaceBuilding() {
		bool canPlace = true;
		
		Bounds placeBounds = tempBuilding.GetSelectionBounds();
		//shorthand for the coordinates of the center of the selection bounds
		float cx = placeBounds.center.x;
		float cy = placeBounds.center.y;
		float cz = placeBounds.center.z;
		//shorthand for the coordinates of the extents of the selection box
		float ex = placeBounds.extents.x;
		float ey = placeBounds.extents.y;
		float ez = placeBounds.extents.z;
		
		//Determine the screen coordinates for the corners of the selection bounds
		List< Vector3 > corners = new List< Vector3 >();
		corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx+ex,cy+ey,cz+ez)));
		corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx+ex,cy+ey,cz-ez)));
		corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx+ex,cy-ey,cz+ez)));
		corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx-ex,cy+ey,cz+ez)));
		corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx+ex,cy-ey,cz-ez)));
		corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx-ex,cy-ey,cz+ez)));
		corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx-ex,cy+ey,cz-ez)));
		corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx-ex,cy-ey,cz-ez)));
		
		foreach(Vector3 corner in corners) {
			GameObject hitObject = WorkManager.FindHitObject(corner);
			if(hitObject && hitObject.name != "Terrain") {
				WorldObject worldObject = hitObject.transform.GetComponent< WorldObject >();
				if(worldObject && placeBounds.Intersects(worldObject.GetSelectionBounds())) canPlace = false;
			}
		}
		return canPlace;
	}

	public void StartConstruction() {
		if (!(GetResourceAmount(ResourceType.Money) - tempBuilding.cost < 0) ) {
			if(!(GetResourceAmount(ResourceType.Power) - tempBuilding.powerUsage < 0)){
			AddResource (ResourceType.Money, -tempBuilding.cost);
			AddResource (ResourceType.Power, -tempBuilding.powerUsage);
			findingPlacement = false;
			Buildings buildings = GetComponentInChildren< Buildings > ();
			if (buildings)
				tempBuilding.transform.parent = buildings.transform;
			tempBuilding.SetPlayer ();
			tempBuilding.SetColliders (true);
			tempCreator.SetBuilding (tempBuilding);
			tempBuilding.StartConstruction ();
			}
			else{
				audioElement.Play(lowPower);
				CancelBuildingPlacement();
			}
		} else {
			audioElement.Play(noMoney);
			CancelBuildingPlacement();
		}

	}

	public void CancelBuildingPlacement() {
		findingPlacement = false;
		Destroy(tempBuilding.gameObject);
		tempBuilding = null;
		tempCreator = null;
	}

	public bool IsDead() {
		Building[] buildings = GetComponentsInChildren< Building >();
		Unit[] units = GetComponentsInChildren< Unit >();
		if(buildings != null && buildings.Length > 0) return false;
		if(units != null && units.Length > 0) return false;
		return true;
	}

	public int GetResourceAmount(ResourceType type) {
		return resources[type];
	}
	protected virtual void InitialiseAudio() {
		List< AudioClip > sounds = new List< AudioClip >();
		List< float > volumes = new List< float >();
		if(noMoney_volume < 0.0f) noMoney_volume = 0.0f;
		if(noMoney_volume > 1.0f) noMoney_volume = 1.0f;
		sounds.Add(noMoney);
		volumes.Add(noMoney_volume);
		if(lowPower_volume < 0.0f) lowPower_volume = 0.0f;
		if(lowPower_volume > 1.0f) lowPower_volume = 1.0f;
		sounds.Add(lowPower);
		volumes.Add(lowPower_volume);
		audioElement = new AudioElement(sounds, volumes, username, this.transform);
	}
}
