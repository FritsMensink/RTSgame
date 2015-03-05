using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RTS;

public class WorldObject : MonoBehaviour {

	public string objectName;
	public Texture2D buildImage;
	public int cost, sellValue, hitPoints, maxHitPoints;

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

	//aanvallen van worldobjects
	protected WorldObject target = null;
	protected bool attacking = false;
	public float weaponRange = 10.0f;
	protected bool movingIntoPosition = false;
	protected bool aiming = false;
	public float weaponRechargeTime = 1.0f;
	private float currentWeaponChargeTime;
	public float weaponAimSpeed = 1.0f;

	protected virtual void Awake() {

		selectionBounds = ResourceManager.InvalidBounds;
		CalculateBounds ();
	}
	// Use this for initialization
	protected virtual void Start () {
		SetPlayer ();

		if (player) {
			SetTeamColor ();
		}
	}

	// Update is called once per frame
	protected virtual void Update () {

		//zorg er voor dat het wapen geladen word en er mogelijk aangevallen kan worden.
		currentWeaponChargeTime += Time.deltaTime;
		if (attacking && !movingIntoPosition && !aiming) {
			PerformAttack ();
		}
		//als de unit niet geselecteerd is, haal dan de units scherm positie op
		if (!currentlySelected) {
			//haal de scherm positie op
			ScreenPos = Camera.main.WorldToScreenPoint (this.transform.position);

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

		if (currentlySelected) {
			DrawSelection();
		}
	}

	private void DrawSelection() {
		GUI.skin = ResourceManager.SelectionBoxSkin;
		GUI.color = Color.yellow;
		Rect selectBox = WorkManager.CalculateSelectionBox(selectionBounds, playingArea);
		//Teken de selectie box om het huidige geselecteerde object, die zich binnen het speel veld bevind.
		GUI.BeginGroup (playingArea);
		DrawSelectionBox (selectBox);
		GUI.EndGroup ();
	}

	public void SetPlayer() {
		player = transform.root.GetComponentInChildren< Player >();
	}

	protected virtual void DrawSelectionBox(Rect selectBox) {
		GUI.Box(selectBox, "");
		CalculateCurrentHealth(0.35f, 0.65f);
		DrawHealthBar(selectBox, "");
	}

	public virtual void SetSelection(bool selected, Rect playingArea) {
		currentlySelected = selected;
		if(selected) this.playingArea = playingArea;
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
			WorldObject worldObject = hitObject.transform.parent.GetComponent< WorldObject > ();
			//clicked on another selectable object
			if (worldObject) {
				Player owner = hitObject.transform.root.GetComponent< Player > ();
				if (owner) { //the object is controlled by a player
					if (player && player.humanControlled) { //this object is controlled by a human player
						//start attack if object is not owned by the same player and this object can attack, else select
						if (player.username != owner.username && CanAttack ())
							BeginAttack (worldObject);
	
					}
				}
			}
		}
	}

	protected virtual void BeginAttack(WorldObject target) {
		this.target = target;
		if (TargetInRange ()) {
			attacking = true;
			PerformAttack ();
		} else { 
			AdjustPosition ();
		}
	}

	private bool TargetInRange() {
		Vector3 targetLocation = target.transform.position;
		Vector3 direction = targetLocation - transform.position;
		if(direction.sqrMagnitude < weaponRange * weaponRange) {
			return true;
		}
		return false;
	}

	private void AdjustPosition() {
		//het enige object wat kan bewegen op dit moment is een unit
		//als het dus geen unit is annuleren we aanval
		Unit self = this as Unit;
		if(self) {
			movingIntoPosition = true;
			Vector3 attackPosition = FindNearestAttackPosition();
			self.StartMove(attackPosition);
			attacking = true;
		} else attacking = false;
	}

	private Vector3 FindNearestAttackPosition() {
		Vector3 targetLocation = target.transform.position;
		Vector3 direction = targetLocation - transform.position;
		float targetDistance = direction.magnitude;
		float distanceToTravel = targetDistance - (0.9f * weaponRange);
		return Vector3.Lerp(transform.position, targetLocation, distanceToTravel / targetDistance);
	}

	private void PerformAttack() {
		if(!target) {
			attacking = false;
			return;
		}
		if (!TargetInRange ()) {
			AdjustPosition ();
		} else if (!TargetInFrontOfWeapon ()) {
			AimAtTarget ();
		} else if (ReadyToFire ()) {
			UseWeapon ();
		}
	}

	//in deze methode gaan we er van uit dat het wapen altijd aan de voorkant van het world object zich bevind
	//specifeke gevallen kunnen deze methode overriden.
	protected virtual bool TargetInFrontOfWeapon() {
		Vector3 targetLocation = target.transform.position;
		Vector3 direction = targetLocation - transform.position;
		if(direction.normalized == transform.forward.normalized) return true;
		else return false;
	}

	private bool ReadyToFire() {
		if(currentWeaponChargeTime >= weaponRechargeTime) return true;
		return false;
	}

	protected virtual void AimAtTarget() {
		aiming = true;
		//this behaviour needs to be specified by a specific object
	}

	protected virtual void UseWeapon() {
		//reset de huidige weapon charge time naar 0 zodat het wapen moet herladen.
		currentWeaponChargeTime = 0.0f;
		//this behaviour needs to be specified by a specific object
	}

	public void TakeDamage(int damage) {
		hitPoints -= damage;
		if (hitPoints <= 0) {
			if (UserInput.GetFirstSelectedWorldObject() == this) {
			this.currentlySelected = false;
			UserInput.CurrentlySelectedWorldObjects[0] = null;
			}
			Destroy (gameObject);
		}
	}

	protected virtual void CalculateCurrentHealth(float lowSplit, float highSplit) {
		healthPercentage = (float)hitPoints / (float)maxHitPoints;
		if(healthPercentage > highSplit) healthStyle.normal.background = ResourceManager.HealthyTexture;
		else if(healthPercentage > lowSplit) healthStyle.normal.background = ResourceManager.DamagedTexture;
		else healthStyle.normal.background = ResourceManager.CriticalTexture;
	}
	
	protected void DrawHealthBar(Rect selectBox, string label) {
		healthStyle.padding.top = -20;
		healthStyle.fontStyle = FontStyle.Bold;
		GUI.Label(new Rect(selectBox.x, selectBox.y - 7, selectBox.width * healthPercentage, 5), label, healthStyle);
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
}
