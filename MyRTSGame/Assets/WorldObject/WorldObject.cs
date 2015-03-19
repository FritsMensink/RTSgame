using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using RTS;

public class WorldObject : MonoBehaviour {

	private static int CurrentObjectId = 0;
	private int ObjectId = 0;

	public string objectName;
	public Texture2D buildImage;
	public int powerUsage,cost, sellValue, hitPoints, maxHitPoints, visiblerange;

	protected Player player;
	protected string[] actions = {};
	//selecte van world objects
	protected Bounds selectionBounds;
	protected Rect playingArea = new Rect(0.0f, 0.0f, 0.0f, 0.0f);

	//voor de UesrInput.cs
	public Vector2 ScreenPos;
	public bool OnScreen = false;
	protected bool currentlySelected = false;

	//opslaan oude materialen bijvoorbeeld bij het bouwen.
	private List< Material > oldMaterials = new List< Material >();

	//health bars
	protected GUIStyle healthStyle = new GUIStyle();
	protected float healthPercentage = 1.0f;

	//worldobject
	protected bool moving, rotating;
	protected Vector3 destination;

	//zelf nadenk vermogen variablen
	//tijd tussen een beslissing is elke halve seconde
	private float timeSinceLastDecision = 0.0f, timeBetweenDecisions = 0.1f;
	public float detectionRange = 10.0f;
	protected List< WorldObject > nearbyObjects;

	//aanvallen van worldobjects
	protected WorldObject target = null;
	protected bool attacking = false;
	public float weaponRange = 10.0f;
	protected bool movingIntoPosition = false;
	protected bool aiming = false;
	public float weaponRechargeTime = 1.0f;
	private float currentWeaponChargeTime;
	public float weaponAimSpeed = 1.0f;

	//fogofwar
	public EditFogOfWarTex EditFogOfWarTex;
	protected Vector3 curPos;
	protected Vector3 lastPos;
	//selection
	public Rect selectBox;

	//audio
	public AudioClip attackSound, selectSound, useWeaponSound;
	public float attackVolume = 1.0f, selectVolume = 1.0f, useWeaponVolume = 1.0f;
	
	protected AudioElement audioElement;

	protected virtual void Awake() {

		selectionBounds = ResourceManager.InvalidBounds;
		CalculateBounds ();
		EditFogOfWarTex = (EditFogOfWarTex)GameObject.Find ("FogOfWar").GetComponent (typeof(EditFogOfWarTex));
		ObjectId = GenerateNewObjectId ();

	}
	// Use this for initialization
	protected virtual void Start () {
		SetPlayer ();

		if (player) {
			SetTeamColor ();
		}

		InitialiseAudio();
	}

	// Update is called once per frame
	protected virtual void Update () {

		//selectionBounds = ResourceManager.InvalidBounds;
		//CalculateBounds ();
		//selectBox = WorkManager.CalculateSelectionBox(selectionBounds, playingArea);
		//zorg er voor dat het wapen geladen word en er mogelijk aangevallen kan worden.
		currentWeaponChargeTime += Time.deltaTime;

		if (ShouldMakeDecision ()) {
			DecideWhatToDo ();
		}

		if (attacking && !movingIntoPosition && !aiming) {
			PerformAttack ();
		}

		//als de unit niet geselecteerd is, haal dan de units scherm positie op
		if (!currentlySelected) {
			//haal de scherm positie op
			ScreenPos = Camera.main.WorldToScreenPoint (this.transform.position);
			//
			//als de unit binnen het scherm zit
			if (UserInput.UnitWithinScreenSpace (ScreenPos)) {
				//en niet al toegevoegd is aan UnitsOnScreen, voeg hem dan er aan toe.
				if (!OnScreen) {
					UserInput.WorldObjectsOnScreen.Add (this.gameObject);
					OnScreen = true;
				}
			}
			//de unit bevind zich niet op het spelers scherm
			else {
				//verwijder hem dan uit de lijst met units op het scherm.
				if (OnScreen) {
					UserInput.RemoveFromOnScreenUnits (this.gameObject);
				}
			}
		}
	}

	protected virtual void OnGUI() {

		if (!ResourceManager.MenuOpen && !ResourceManager.MenuOpen) {
			if (currentlySelected) {
				DrawSelection ();
			} 
		}

		if (target != null && !ResourceManager.MenuOpen) {
			target.DrawHealthBar();
		}
	}

	private void DrawDefaultVisibleHealthBar() {
				
		//als het wereldobject niet van een speler is (niet van ai of speler)
		//word de playing area niet gezet voor het object waardoor
		//de healthbar niet getekend wordt zonder dat het object geselecteerd is.
		if (player) {
			this.playingArea = player.hud.GetPlayingArea ();
				
			Rect selectBox = WorkManager.CalculateSelectionBox (selectionBounds, playingArea);
			//Draw the selection box around the currently selected object, within the bounds of the playing area
			GUI.BeginGroup (playingArea);
			CalculateCurrentHealth (0.35f, 0.65f);
			DrawHealthBar (selectBox, "");
			GUI.EndGroup ();
		}
	}

	public Rect GetSelectionBox() {
		return WorkManager.CalculateSelectionBox(selectionBounds, playingArea);
	}

	public void DrawSelection() {
		if (player) {
			this.playingArea = player.hud.GetPlayingArea ();

			GUI.skin = ResourceManager.SelectionBoxSkin;

			//als het wereldobject niet van een speler is (niet van ai of speler)
			//word de playing area niet gezet voor het object waardoor
			//de healthbar niet getekend wordt zonder dat het object geselecteerd is.

			Rect selectBox = WorkManager.CalculateSelectionBox (selectionBounds, playingArea);
			//Draw the selection box around the currently selected object, within the bounds of the playing area
			GUI.BeginGroup (playingArea);
			DrawSelectionBox (selectBox);
			DrawHealthBar (selectBox, "");
			GUI.EndGroup ();
		}
	}

	protected virtual void DrawSelectionBox(Rect selectBox) {
		GUI.Box(selectBox, "");
	}

	public void SetPlayer() {
		player = transform.root.GetComponentInChildren< Player >();
	}

	public virtual void SetSelection(bool selected, Rect playingArea) {
		currentlySelected = selected;
		if (selected) {
			this.playingArea = playingArea;

			if(audioElement != null) audioElement.Play(selectSound);
		}
	}

	public bool GetCurrentlySelected() {
		return currentlySelected;
	}
	
	public string[] GetActions() {
		return actions;
	}

	public virtual void PerformAction(string actionToPerfom) {
		//de subclasses van het wereld object moeten bepalen wat er moet gaan gebeuren.
	}

	public void CalculateBounds() {
		selectionBounds = new Bounds(transform.position, Vector3.zero);
		foreach(Renderer r in GetComponentsInChildren< Renderer >()) {
			selectionBounds.Encapsulate(r.bounds);
		}
	}

	public virtual void SetHoverState(GameObject hoverObject) {
		//only handle input if owned by a human player and currently selected
		if(player && player.humanControlled && currentlySelected) {
			//something other than the ground is being hovered over
			if(hoverObject.name != "Terrain") {
				Player owner = hoverObject.transform.root.GetComponent< Player >();
				Unit unit = hoverObject.transform.GetComponent< Unit >();
				Building building = hoverObject.transform.GetComponent< Building >();
				if(owner) { //the object is owned by a player
					if(owner.username == player.username) { player.hud.SetCursorState(CursorState.Select); }
					//CanAttack van bijbehorende klaasse word uitgevoerd.
					else if(CanAttack())  { player.hud.SetCursorState(CursorState.Attack); }
					else  { player.hud.SetCursorState(CursorState.Select); }
				} else if(unit || building && CanAttack())  { player.hud.SetCursorState(CursorState.Attack); }
				else { player.hud.SetCursorState(CursorState.Select); }
			}
		}
	}

	public virtual bool CanAttack() {
		//default behaviour needs to be overidden by children
		return false;
	}

	public bool IsOwnedBy(Player owner) {
		if(player && player.Equals(owner)) {
			return true;
		} else {
			return false;
		}
	}
	
	//Deze methode handeld de basis muis click op een wereldObjet.
	//Verdere details hangen af van de subclasse die een andere/meer implementatie kan specificeren.
	//er kan alleen aangevallen worden door een speler als hij niet zelf de unit controlled die hij wil aanvallen
	//en het andere object wat hij wil aanvallen ook onder een speler valt.
	public virtual void MouseClick(GameObject hitObject, Vector3 hitPoint, Player controller){
		//Ga alleen iets doen met de muisclick als er op dit moment iets geselecteerd is.
		if (currentlySelected && hitObject && hitObject.name != "Terrain") {

			WorldObject worldObject = hitObject.transform.GetComponent< WorldObject > ();
			target = worldObject;
			//clicked on another selectable object
			if (worldObject) {
				Player owner = hitObject.transform.root.GetComponent< Player > ();
				if (owner) { //the object is controlled by a player or ai
					if (player && player.humanControlled) { //this object is controlled by a human player
						//start attack if object is not owned by the same player and this object can attack, else select
						if (player.username != owner.username && CanAttack ()) {
							BeginAttack (worldObject);
						}
					}
				}
			}
		}
	}

	public void TakeDamage(int damage) {
		hitPoints -= damage;
		if (hitPoints <= 0) {
			Destroy (gameObject);
		}
	}

	protected void BeginAttack(WorldObject target) {
		if (audioElement != null) {
			audioElement.Play (attackSound);
		}
		this.target = target;

		if (moving) {
			destination = transform.position;
		}

		if (TargetInRange ()) {
			attacking = true;
			PerformAttack ();
		} else {
			AdjustPosition ();
		}
	}
	
	protected void PerformAttack() {
		if(!target) {
			attacking = false;
			return;
		}
		if (!TargetInRange ()) {
			AdjustPosition ();
		}
		else if (!TargetInFrontOfWeapon ()) {
			AimAtTarget ();
		} else if (ReadyToFire ()) {
			UseWeapon ();
		}
	}
	
	protected bool TargetInRange() {
		Vector3 targetLocation = target.transform.position;
		Vector3 direction = targetLocation - transform.position;
		if(direction.sqrMagnitude < weaponRange * weaponRange) {
			return true;
		}
		return false;
	}

	protected virtual void UseWeapon() {
		if(audioElement != null && Time.timeScale > 0) audioElement.Play(useWeaponSound);
		currentWeaponChargeTime = 0.0f;
		//this behaviour needs to be specified by a specific object
	}

	protected virtual void AimAtTarget() {
		aiming = true;
		//this behaviour needs to be specified by a specific object
	}
	
	protected bool TargetInFrontOfWeapon() {
		Vector3 targetLocation = target.transform.position;
		Vector3 direction = targetLocation - transform.position;
        float angle = Vector3.Angle(direction, transform.forward);

		if (angle < 5.0 && angle > -5.0) {
			return true;
		}

		//if(direction.normalized == transform.forward.normalized) return true;
		else {
			return false;
		}
	}
	
	private Vector3 FindNearestAttackPosition() {
		Vector3 targetLocation = target.transform.position;
		Vector3 direction = targetLocation - transform.position;
		float targetDistance = direction.magnitude;
		float distanceToTravel = targetDistance - (0.9f * weaponRange);
		return Vector3.Lerp(transform.position, targetLocation, distanceToTravel / targetDistance);
	}
	
	protected void AdjustPosition() {
		Unit self = this as Unit;
		if(self) {
			movingIntoPosition = true;
			Vector3 attackPosition = FindNearestAttackPosition();
			self.StartMove(attackPosition);
			attacking = true;
		} else attacking = false;
	}
	
	protected bool ReadyToFire() {
		if(currentWeaponChargeTime >= weaponRechargeTime) return true;
		return false;
	}

	protected virtual void CalculateCurrentHealth(float lowSplit, float highSplit) {
		healthPercentage = (float)hitPoints / (float)maxHitPoints;
		if(healthPercentage > highSplit) healthStyle.normal.background = ResourceManager.HealthyTexture;
		else if(healthPercentage > lowSplit) healthStyle.normal.background = ResourceManager.DamagedTexture;
		else healthStyle.normal.background = ResourceManager.CriticalTexture;
	}
	
	public void DrawHealthBar(Rect selectBox, string label) {
		CalculateCurrentHealth(0.35f, 0.65f);
		healthStyle.padding.top = -20;
		healthStyle.fontStyle = FontStyle.Bold;
		GUI.Label(new Rect(selectBox.x, selectBox.y - 7, selectBox.width * healthPercentage, 5), label, healthStyle);
	}

	public void DrawHealthBar() {
		if (player) {
			this.playingArea = player.hud.GetPlayingArea ();

			Rect selectBox = GetSelectionBox ();
			CalculateCurrentHealth (0.35f, 0.65f);
			healthStyle.padding.top = -20;
			healthStyle.fontStyle = FontStyle.Bold;
			GUI.Label (new Rect (selectBox.x, selectBox.y + 35, selectBox.width * healthPercentage, 5), "", healthStyle);
		}
	}

	public Bounds GetSelectionBounds() {
		return selectionBounds;
	}

	public void SetColliders(bool enabled) {
		Collider[] colliders = GetComponentsInChildren< Collider >();
		foreach(Collider collider in colliders) collider.enabled = enabled;
	}
	
	public void SetTransparentMaterial(Material material, bool storeExistingMaterial) {
		if(storeExistingMaterial) oldMaterials.Clear();
		Renderer[] renderers = GetComponentsInChildren< Renderer >();
		foreach(Renderer renderer in renderers) {
			if(storeExistingMaterial) oldMaterials.Add(renderer.material);
			renderer.material = material;
		}
	}
	
	public void RestoreMaterials() {
		Renderer[] renderers = GetComponentsInChildren< Renderer >();
		if(oldMaterials.Count == renderers.Length) {
			for(int i = 0; i < renderers.Length; i++) {
				renderers[i].material = oldMaterials[i];
			}
		}
	}
	
	public void SetPlayingArea(Rect playingArea) {
		this.playingArea = playingArea;
	}

	protected void SetTeamColor() {
		TeamColor[] teamColors = GetComponentsInChildren< TeamColor >();
		foreach(TeamColor teamColor in teamColors) teamColor.renderer.material.color = player.teamColor;
	}

	protected virtual void InitialiseAudio() {
		List< AudioClip > sounds = new List< AudioClip >();
		List< float > volumes = new List< float >();
		if(attackVolume < 0.0f) attackVolume = 0.0f;
		if(attackVolume > 1.0f) attackVolume = 1.0f;
		sounds.Add(attackSound);
		volumes.Add(attackVolume);
		if(selectVolume < 0.0f) selectVolume = 0.0f;
		if(selectVolume > 1.0f) selectVolume = 1.0f;
		sounds.Add(selectSound);
		volumes.Add(selectVolume);
		if(useWeaponVolume < 0.0f) useWeaponVolume = 0.0f;
		if(useWeaponVolume > 1.0f) useWeaponVolume = 1.0f;
		sounds.Add(useWeaponSound);
		volumes.Add(useWeaponVolume);
		audioElement = new AudioElement(sounds, volumes, objectName, this.transform);
	}

	public string GetPlayerUsername() {
		if (player) {
			return player.username;
		}
		return "";
	}

	/**
 * A child class should only determine other conditions under which a decision should
 * not be made. This could be 'harvesting' for a harvester, for example. Alternatively,
 * an object that never has to make decisions could just return false.
 */
	protected virtual bool ShouldMakeDecision() {
		if(!attacking && !movingIntoPosition && !aiming) {
			//we are not doing anything at the moment
			if(timeSinceLastDecision > timeBetweenDecisions) {
				timeSinceLastDecision = 0.0f;
				return true;
			}
			timeSinceLastDecision += Time.deltaTime;
		}
		return false;
	}
	
	protected virtual void DecideWhatToDo() {
		//determine what should be done by the world object at the current point in time
		Vector3 currentPosition = transform.position;
		nearbyObjects = WorkManager.FindNearbyObjects(currentPosition, detectionRange);
		try {
		if(CanAttack()) {
			List< WorldObject > enemyObjects = new List< WorldObject >();
			foreach(WorldObject nearbyObject in nearbyObjects) {
				
				if (nearbyObject.player != null ) {
				if(nearbyObject.GetPlayer().username != player.username)  
				{ enemyObjects.Add(nearbyObject); 
				}
					} 

			}
			WorldObject closestObject = WorkManager.FindNearestWorldObjectInListToPosition(enemyObjects, currentPosition);
			if(closestObject) BeginAttack(closestObject);
			} } catch (Exception) { }
	}

	public Player GetPlayer() {
		return player;
	}

	private static  int GenerateNewObjectId() {
		CurrentObjectId = CurrentObjectId + 1;
		return CurrentObjectId;
	}

	public int GetWorldObjectId() {
		return ObjectId;
	}
}
