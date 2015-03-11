using UnityEngine;
using System.Collections;
using RTS;

public class UserInput : MonoBehaviour {


	//public static Rect selectionRectangle = new Rect(0,0,0,0);
	//public Texture2D selectionHighLight = null;
	#region Class Variables
	
	RaycastHit hit;
	private Player player;
	
	public static Vector3 RightClickPoint;
	public static ArrayList CurrentlySelectedWorldObjects = new ArrayList(); // of gameGameObject
	public static ArrayList WorldObjectsOnScreen = new ArrayList (); // of Gameobject
	public static ArrayList WorldObjectsInDrag = new ArrayList(); // of gameobject
	
	private bool FinishDragOnThisFrame;
	private bool StartedDrag;
	public Texture2D MouseDragTexture;
	public GameObject Target;
	private static Vector3 mouseDownPoint;
	private static Vector3 currentMousePoint; // In World Space
	public bool UserIsDragging;
	private static float TimeLimitBeforeDeclareDrag = 1.0f;
	private static float TimeLeftBeforeDeclareDrag;
	private static Vector2 MouseDragStart;
	private static float clickDragZone = 1.3f;
	
	//GUI
	
	private float boxWidth;
	private float boxHeight;
	private float boxTop;
	private float boxLeft;
	private static Vector2 boxStart;
	private static Vector2 boxFinish;
	
	#endregion

	// Use this for initialization
	void Start () {
		//ga naar het root object (player) en zoek naar het Player component dat er al aan hangt (refereerd).
		player = transform.root.GetComponent<Player> ();
	}			

	private void MoveCamera() {

		float xpos = Input.mousePosition.x;
		float ypos = Input.mousePosition.y;
		Vector3 movement = new Vector3(0, 0, 0);

		bool mouseScroll = false;

		//Horizontale camera beweging
		if(xpos >= 0 && xpos < ResourceManager.ScrollWidth) {
			movement.x -= ResourceManager.ScrollSpeed;
			player.hud.SetCursorState(CursorState.PanLeft);
			mouseScroll = true;
		} else if(xpos <= Screen.width && xpos > Screen.width - ResourceManager.ScrollWidth - 10) {
			movement.x += ResourceManager.ScrollSpeed;
			player.hud.SetCursorState(CursorState.PanRight);
			mouseScroll = true;
		}

		//verticale camera beweging
		if(ypos >= 0 && ypos < ResourceManager.ScrollWidth) {
			movement.z -= ResourceManager.ScrollSpeed;
			player.hud.SetCursorState(CursorState.PanDown);
			mouseScroll = true;
		} else if(ypos <= Screen.height && ypos > Screen.height - ResourceManager.ScrollWidth + 5) {
			movement.z += ResourceManager.ScrollSpeed;
			player.hud.SetCursorState(CursorState.PanUp);
			mouseScroll = true;
		}

		//om de camera naar voren te laten beweging in de richting waar hij naar toe wijst.
		//door de verticale kanteling te negeren.
		//de verticale beweging world terug op 0 gezet om er voor te zorgen dat de camera mooi ronddraaid.
		movement = Camera.main.transform.TransformDirection (movement);
		movement.y = 0;

		//van de grond afbewegen
		movement.y -= ResourceManager.ScrollSpeed * Input.GetAxis("Mouse ScrollWheel");

		//Berkenen vna de gewenste camera positie gebaseerd op de input
		Vector3 origin = Camera.main.transform.position;
		Vector3 destination = origin;
		destination.x += movement.x;
		destination.y += movement.y;
		destination.z += movement.z;

		//Controlleer of de camera beweging binnen de voor op gestelde limiet bevind.
		if(destination.y > ResourceManager.MaxCameraHeight) {
			destination.y = ResourceManager.MaxCameraHeight;
		} else if(destination.y < ResourceManager.MinCameraHeight) {
			destination.y = ResourceManager.MinCameraHeight;
		}

		//Als er verandering in de camera postiie is gedetecteerd, voor dan de benodige update uit.
		if(destination != origin) {
			Camera.main.transform.position = Vector3.MoveTowards(origin, destination, Time.deltaTime * ResourceManager.ScrollSpeed);
		}

		if(!mouseScroll) {
			player.hud.SetCursorState(CursorState.Select);
		}
	}

	private void RotateCamera() {
		Vector3 origin = Camera.main.transform.eulerAngles;
		Vector3 destination = origin;
		
		//detect rotation amount if ALT is being held and the Right mouse button is down
		if((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetMouseButton(1)) {
			//destination.x -= Input.GetAxis("Mouse Y") * ResourceManager.RotateAmount;
			destination.y += Input.GetAxis("Mouse X") * ResourceManager.RotateAmount;
		}
		
		//if a change in position is detected perform the necessary update
		if(destination != origin) {
			Camera.main.transform.eulerAngles = Vector3.MoveTowards(origin, destination, Time.deltaTime * ResourceManager.RotateSpeed);
		}
	}

	//update word 1x aangeroepen per frame.
	void Update()
	{

		if (player.humanControlled) {

			if (Input.GetKeyDown(KeyCode.Escape)) {
				OpenPauseMenu ();
		}
			MoveCamera ();
			RotateCamera ();

			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		
			if (Physics.Raycast (ray, out hit, Mathf.Infinity)) {
				currentMousePoint = hit.point;
			
				// Store where clicked
				if (Input.GetMouseButtonDown (0) && player.hud.MouseInBounds ()) {
					if(player.hud.MouseInBounds() && !Input.GetKey(KeyCode.LeftAlt) && GetFirstSelectedWorldObject() != null) {
						if(player.IsFindingBuildingLocation()) {
							player.CancelBuildingPlacement();
						} else {
							GetFirstSelectedWorldObject().SetSelection(false, player.hud.GetPlayingArea());
							CurrentlySelectedWorldObjects.Remove(GetFirstSelectedWorldObject());
						}
					}
					mouseDownPoint = hit.point;
					TimeLeftBeforeDeclareDrag = TimeLimitBeforeDeclareDrag;
					MouseDragStart = Input.mousePosition;
					StartedDrag = true;
				
				} else if (Input.GetMouseButton (0) && player.hud.MouseInBounds ()) {
					// if the user is not draggin lets do the test
					if (!UserIsDragging) {
						// Test to see if the user is draggin
						TimeLeftBeforeDeclareDrag -= Time.deltaTime;
						if (UserDraggingByPosition (MouseDragStart, Input.mousePosition))
						{
							UserIsDragging = true;
						}
					}
				} else if (Input.GetMouseButtonUp (0) && player.hud.MouseInBounds ()) {
					if (UserIsDragging) {
						FinishDragOnThisFrame = true;
						UserIsDragging = false;
					}
				
				}
			
				// Mouse click
				if (!UserIsDragging && player.hud.MouseInBounds ()) {
				
					if (Input.GetMouseButtonUp (1)) {
						if (CurrentlySelectedWorldObjects.Count > 0) {
							if(player.IsFindingBuildingLocation()) {
								if(player.CanPlaceBuilding()) {

									player.StartConstruction();
								}
							} 
							else {
								foreach (GameObject currentlySelectedWorldObject in CurrentlySelectedWorldObjects) {
									if (currentlySelectedWorldObject != null && hit.collider.gameObject != null)
								currentlySelectedWorldObject.GetComponent<WorldObject> ().MouseClick (hit.collider.gameObject, hit.point, player);
								}
							}
						}
					}	
					// Is it terrain?  
					//print (hit.collider.name);
					if (hit.collider.name == "Terrain") {
						// right clicking creates target model if so spawn pointer
						if (Input.GetMouseButtonDown (1)) {  
							//GameObject TargetObj = Instantiate (Target, hit.point, Quaternion.identity) as GameObject;
							//TargetObj.name = "Target Instantiate";
							//RightClickPoint = hit.point;
						} else if (Input.GetMouseButtonUp (1)) {
					
						} else if (Input.GetMouseButtonUp (0) && DidUserClickLeftMouse (mouseDownPoint)) {

							if (!ShiftKeysDown ()){
								DeselectGameobjectsIfSelected ();
							}
								
						}
					} // end of terrain
				else {
										
						//hiting other things
						if (Input.GetMouseButtonUp (0) && DidUserClickLeftMouse (mouseDownPoint)) {
							// Is the user hitting unit? Checks for the Unit Script
							if (hit.collider.gameObject.GetComponent<WorldObject> ()) {
								//are we selecting a different object?
								if (!UnitAlreadyInCurrentlySelectedUnits (hit.collider.gameObject)) {
									// if shift key not down remove rest of units
									if (!ShiftKeysDown ()) {
										DeselectGameobjectsIfSelected ();
									}
									//GameObject SelectedObj = hit.collider.transform.FindChild ("Selector").gameObject;
									//SelectedObj.SetActive(true);
									// add unit to current units
									CurrentlySelectedWorldObjects.Add (hit.collider.gameObject);
								
									//change the unit selected value to true
									hit.collider.gameObject.GetComponent<WorldObject> ().SetSelection (true, player.hud.GetPlayingArea ());
								} else {
									// unit is already in the currently select units!
									//remove unit!
									if (ShiftKeysDown ()){
										RemoveUnitFromCurrentlySelectedUnits (hit.collider.gameObject);
										hit.collider.gameObject.GetComponent<WorldObject> ().SetSelection (false, player.hud.GetPlayingArea ());
									}
									else {
										DeselectGameobjectsIfSelected ();
										//GameObject SelectedObj = hit.collider.transform.FindChild ("Selector").gameObject;
										//SelectedObj.SetActive (true);
										CurrentlySelectedWorldObjects.Add (hit.collider.gameObject);
										hit.collider.gameObject.GetComponent<WorldObject> ().SetSelection (true, player.hud.GetPlayingArea ());
									}
								}
							
							} else {
								//if this object is not unit
								if (!ShiftKeysDown ()) {
									DeselectGameobjectsIfSelected ();
									//GameObject SelectedObj = hit.collider.transform.FindChild ("Selector").gameObject;
									//SelectedObj.SetActive (false);
								}
							}
						}
					}
				} else {
//					if (Input.GetMouseButtonUp (0) && DidUserClickLeftMouse (mouseDownPoint) && player.hud.MouseInBounds())
					if (!ShiftKeysDown () && player.hud.MouseInBounds ()) {
					DeselectGameobjectsIfSelected ();
					}
//			}//end of raycast
//		} // end of dragging
//			if(!ShiftKeysDown() && StartedDrag && UserIsDragging && player.hud.MouseInBounds())
//		{
//			DeselectGameobjectsIfSelected();
//			StartedDrag = false;
//		}
				
					if (UserIsDragging) {
						//GUI Variables
						boxWidth = Camera.main.WorldToScreenPoint (mouseDownPoint).x - Camera.main.WorldToScreenPoint (currentMousePoint).x;
						boxHeight = Camera.main.WorldToScreenPoint (mouseDownPoint).y - Camera.main.WorldToScreenPoint (currentMousePoint).y;
						boxLeft = Input.mousePosition.x;
						boxTop = (Screen.height - Input.mousePosition.y) - boxHeight;

						if (FloatToBool (boxWidth))
						if (FloatToBool (boxHeight))
							boxStart = new Vector2 (Input.mousePosition.x, Input.mousePosition.y + boxHeight);
						else
							boxStart = new Vector2 (Input.mousePosition.x, Input.mousePosition.y);
						else
				if (FloatToBool (boxWidth))
						if (FloatToBool (boxHeight))
							boxStart = new Vector2 (Input.mousePosition.x + boxWidth, Input.mousePosition.y + boxHeight);
						else
							boxStart = new Vector2 (Input.mousePosition.x + boxWidth, Input.mousePosition.y);
			
			
			
						boxFinish = new Vector2 (
					boxStart.x + Unsinged (boxWidth),
					boxStart.y - Unsinged (boxHeight)
						);
			
					}
				}
			}// end of update function
		}
	}

	void LateUpdate()
	{
		WorldObjectsInDrag.Clear ();
		
		//if user is dragging, or finished onm this fram, AND there are units to select on the screen
		if ((UserIsDragging || FinishDragOnThisFrame) && WorldObjectsOnScreen.Count > 0) {
			//loop through thouse uunits on screen
			for (int i = 0; i < WorldObjectsOnScreen.Count; i++) {
				GameObject UnitObj = WorldObjectsOnScreen [i] as GameObject;
				if (UnitObj) {
					WorldObject WorldObjectScript = UnitObj.GetComponent<WorldObject> ();
					//GameObject SelectedObj = UnitObj.transform.FindChild ("Selector").gameObject;
				
					//if not allready in the dragged units
					if (!UnitAlreadyInDraggedUnits (UnitObj)) {
						if (UnitInsideDrag (WorldObjectScript.ScreenPos)) {
							//SelectedObj.SetActive (true);
							WorldObjectsInDrag.Add (UnitObj);
							//test in sleepend vakje
							UnitObj.renderer.material.color = Color.red;
						}
					
					//unit is not in the drag!
					else {
							//test niet meer in slepend vakje en ook niet geselecteerd
							UnitObj.renderer.material.color = Color.white;
							//remove the selected graphic, if units is not allready in currently selected units
							//if (!UnitAlreadyInCurrentlySelectedUnits (UnitObj) && !ShiftKeysDown()) { 
							//SelectedObj.SetActive (false);

							//}
						}
					}
				}
			}
		}
		if (FinishDragOnThisFrame) {
			FinishDragOnThisFrame = false;
			PutDraggedUnitsInCurrentlySelectedUnits ();
		}

		MouseHover ();
	}
	
	void OnGUI()
	{
		
		//box width, hieght, top, left
		if(UserIsDragging)
		{
			GUI.color = new Color (1, 1, 1, 0.5f);
			GUI.DrawTexture (new Rect(boxLeft, boxTop, boxWidth, boxHeight), MouseDragTexture);
		//	GUI.Box(new Rect(boxLeft,
		//	                 boxTop,
		//	                 boxWidth,
		//	                 boxHeight), "", MouseDragTexture);
		}
		
	}

	private void MouseHover() {
		if(player.hud.MouseInBounds()) {

			if(player.IsFindingBuildingLocation()) {
				player.FindBuildingLocation();
			} else {

			GameObject hoverObject = WorkManager.FindHitObject(Input.mousePosition);

			if(hoverObject) {
				if(CurrentlySelectedWorldObjects.Count > 0 && GetFirstSelectedWorldObject() != null) 
				{
					GameObject firstSelectedUnit = CurrentlySelectedWorldObjects[0] as GameObject;
					if (firstSelectedUnit.GetComponent<WorldObject>().GetCurrentlySelected()) {
						firstSelectedUnit.GetComponent<WorldObject>().SetHoverState(hoverObject);
					}
				}
				else if(hoverObject.name != "Terrain") {
					Player owner = hoverObject.transform.root.GetComponent< Player >();
					if(owner) {
						Unit unit = hoverObject.transform.parent.GetComponent< Unit >();
						Building building = hoverObject.transform.parent.GetComponent< Building >();
						if(owner.username == player.username && (unit || building)) player.hud.SetCursorState(CursorState.Select);
						}
					}
				}
			}
		}
	}

	#region Helper functions

	//float naar bool
	public static bool FloatToBool(float Float)
	{
		if(Float < 0F) return false; else return true;
	}
	
	//maak negatief
	public static float Unsinged(float val)
	{
		if (val < 0f) val *= -1;
		return val;
	}

	// shift key being held down 
	public static bool ShiftKeysDown()
	{
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			return true;
		else
			return false;
	}

//	private GameObject FindHitObject() {
//		//om uit te vinden welk object geraakt is, wordt er gebruik gemaakt van een paar Unity methodes
//		//De ray is een lijn die loop van waar punt waar de speler klikt en de main camera.
//		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
//		RaycastHit hit;
//		//Physics.Raycast() volgt de lijn en kijkt welk object er het eerst geraakt wordt.
//		//Als het een object find plaats hij hem in de variable hit.
//		if (Physics.Raycast (ray, out hit)) {
//			//als hij de variable hit vind returnen we het gameObject 
//			return hit.collider.gameObject;
//		}
//		return null;
//	}
//	
//	private Vector3 FindHitPoint() {
//		//om uit te vinden welk object geraakt is, wordt er gebruik gemaakt van een paar Unity methodes
//		//De ray is een lijn die loop van waar punt waar de speler klikt en de main camera.
//		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
//		RaycastHit hit;
//		//Physics.Raycast() volgt de lijn en kijkt welk object er het eerst geraakt wordt.
//		//Als het een object find plaats hij hem in de variable hit.
//		if (Physics.Raycast (ray, out hit)) {
//			//als hij de variable hit vind returnen we de positie van hit.
//			return hit.point;
//		}
//		return ResourceManager.InvalidPosition;
//	}

	// //is de speler aan het slepen met de muis gebaseerd op verandering in de muispositie toen de speler de linker muis knop indrukte
	public bool UserDraggingByPosition(Vector2 DragStartPoint, Vector2 NewPoint)
	{
		if(
			(NewPoint.x > DragStartPoint.x + clickDragZone || NewPoint.x < DragStartPoint.x - clickDragZone) ||
			(NewPoint.y > DragStartPoint.y + clickDragZone || NewPoint.y < DragStartPoint.y - clickDragZone)
			)
			return true; else return false;
	}
	
	
	// Controleer of de user de linker muis knop heeft ingedrukt.
	public bool DidUserClickLeftMouse(Vector3 hitPoint)
	{
		if (
			(mouseDownPoint.x < hitPoint.x + clickDragZone && mouseDownPoint.x > hitPoint.x - clickDragZone) &&
			(mouseDownPoint.y < hitPoint.y + clickDragZone && mouseDownPoint.y > hitPoint.y - clickDragZone) &&
			(mouseDownPoint.z < hitPoint.z + clickDragZone && mouseDownPoint.z > hitPoint.z - clickDragZone)
			)
			return true;
		else
			return false;
	}
	
	//deselecteer het gameobject als die geselecteerd is
	public void DeselectGameobjectsIfSelected()
	{
		if(CurrentlySelectedWorldObjects.Count > 0)
		{
			for(int i = 0; i < CurrentlySelectedWorldObjects.Count; i++)
			{
				GameObject ArrayListUnit = CurrentlySelectedWorldObjects[i] as GameObject;
				//ArrayListUnit.transform.FindChild("Selector").gameObject.SetActive(false);
				if (ArrayListUnit) {
				ArrayListUnit.GetComponent<WorldObject>().SetSelection(false, player.hud.GetPlayingArea());
				}
			}
			
			CurrentlySelectedWorldObjects.Clear();
		}
	}
	
	//controleer of de unit al in de huidige geselecteerde units arraylist zit
	public static bool UnitAlreadyInCurrentlySelectedUnits(GameObject Unit)
	{
		if (CurrentlySelectedWorldObjects.Count > 0) {
			for (int i = 0; i < CurrentlySelectedWorldObjects.Count; i++) {
				GameObject ArrayListUnit = CurrentlySelectedWorldObjects [i] as GameObject;
				if (ArrayListUnit == Unit)
					return true;
				
			}
			
			return false;
		} else
			return false;
	}
	
	//verdwijder een unit uit de huidige geselecteerde units arraylist
	public void RemoveUnitFromCurrentlySelectedUnits(GameObject Unit)
	{
		if (CurrentlySelectedWorldObjects.Count > 0) {
			for (int i = 0; i < CurrentlySelectedWorldObjects.Count; i++) {
				GameObject ArrayListUnit = CurrentlySelectedWorldObjects [i] as GameObject;
				if (ArrayListUnit == Unit)
				{
					CurrentlySelectedWorldObjects.RemoveAt(i);
					//ArrayListUnit.transform.FindChild("Selector").gameObject.SetActive(false);
				}
			}
			
			return;
		} else
			return;
	}
	
	// check if a unit is within screen space to deal with mouse drag selecting
	
	//controlleer of een wereld object zich binnen het scherm bevind voor de muis drag selectering
	public static bool UnitWithinScreenSpace(Vector2 UnitScreenPos)
	{
		if (
			(UnitScreenPos.x < Screen.width && UnitScreenPos.y < Screen.height) &&
			(UnitScreenPos.x > 0F && UnitScreenPos.y > 0f)
			) {
			return true;
		} else { return false; }
	}
	
	//Verwijder een unit uit het worldobjectsOnScreen lijst
	public static void RemoveFromOnScreenUnits(GameObject Unit)
	{
		for (int i = 0; i < WorldObjectsOnScreen.Count; i++)
		{
			GameObject UnitObj = WorldObjectsOnScreen[i] as GameObject;
			if(Unit == UnitObj)
			{
				WorldObjectsOnScreen.RemoveAt(i);
				UnitObj.GetComponent<WorldObject>().OnScreen = false;
				return;
			}
		}
		return;
	}
	
	//Zit de unit in de gesleepte drag zone?
	public static bool UnitInsideDrag(Vector2 UnitScreenPos)
	{
		if (
			(UnitScreenPos.x > boxStart.x && UnitScreenPos.y < boxStart.y) &&
			(UnitScreenPos.x < boxFinish.x && UnitScreenPos.y > boxFinish.y))
			return true;
		else
			return false;
	}
	
	//Controleer of de unit al in de UnitDragArray lijst zit
	public static bool UnitAlreadyInDraggedUnits(GameObject Unit)
	{
		if (WorldObjectsInDrag.Count > 0) {
			for (int i = 0; i < WorldObjectsInDrag.Count; i++)
			{
				GameObject ArrayListUnit = WorldObjectsInDrag [i] as GameObject;
				if (ArrayListUnit == Unit)
					return true;
				
			}
			return false;
		} else
			return false;
	}
	
	//plaats alle units in UnitsInDrag in currentlySelectedUnits
	public void PutDraggedUnitsInCurrentlySelectedUnits()
	{
		if(!ShiftKeysDown())
			DeselectGameobjectsIfSelected();
		
		if (WorldObjectsInDrag.Count > 0)
		{
			for (int i = 0; i < WorldObjectsInDrag.Count; i++)
			{
				GameObject UnitObj = WorldObjectsInDrag [i] as GameObject;
				
				//als de unit niet al in de currentlySelectedUnits zit, voeg hem dan toe
				if(!UnitAlreadyInCurrentlySelectedUnits(UnitObj))
				{
					CurrentlySelectedWorldObjects.Add(UnitObj);
					UnitObj.GetComponent<WorldObject>().SetSelection(true, player.hud.GetPlayingArea());
				}
			}
			
			WorldObjectsInDrag.Clear();
		}
	}

	public static WorldObject GetFirstSelectedWorldObject() {
		if (CurrentlySelectedWorldObjects.Count > 0 && CurrentlySelectedWorldObjects[0] != null) {
			//Debug.Log("GetFirt 587");
			GameObject firstCurrentlySelectedWorldObject = CurrentlySelectedWorldObjects [0] as GameObject;
			WorldObject firstSelectedWorldObject = firstCurrentlySelectedWorldObject.GetComponent<WorldObject>();
			//Debug.Log("name:" + firstCurrentlySelectedWorldObject.name);
			return firstSelectedWorldObject;
		} else
			return null;
	}

	private void OpenPauseMenu() {
		Time.timeScale = 0.0f;
		GetComponentInChildren< PauseMenu >().enabled = true;
		GetComponent< UserInput >().enabled = false;
		Screen.showCursor = true;
		ResourceManager.MenuOpen = true;
	}
	#endregion
	
}