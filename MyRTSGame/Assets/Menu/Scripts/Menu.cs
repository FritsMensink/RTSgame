using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RTS;

public class Menu : MonoBehaviour {
	
	public GUISkin GUIBackgroundSkin;
	public Texture2D header;
	protected string[] labels;
	protected string[] buttons;

	public AudioClip clickSound;
	public float clickVolume = 1.0f;
	
	private AudioElement audioElement;

	protected virtual void Start () {
		SetButtons ();

		if (clickVolume < 0.0f) {
			clickVolume = 0.0f;
		}
		if (clickVolume > 1.0f) {
			clickVolume = 1.0f;
		}
		List< AudioClip > sounds = new List< AudioClip >();
		List< float> volumes = new List< float >();
		sounds.Add(clickSound);
		volumes.Add (clickVolume);
		audioElement = new AudioElement(sounds, volumes, "Menu", null);
	}
	
	protected virtual void OnGUI() {
		DrawMenu();
	}
	
	protected virtual void DrawMenu() {
		//default implementation for a menu consisting of a vertical list of buttons
		GUI.skin = GUIBackgroundSkin;
		float menuHeight = GetMenuHeight();
		
		float groupLeft = Screen.width / 2 - ResourceManager.MenuWidth / 2;
		float groupTop = Screen.height / 2 - menuHeight / 2;
		GUI.BeginGroup(new Rect(groupLeft, groupTop, ResourceManager.MenuWidth, menuHeight));
		
		//background box
		GUI.Box(new Rect(0, 0, ResourceManager.MenuWidth, menuHeight), "");
		//header image
		GUI.DrawTexture(new Rect(ResourceManager.Padding, ResourceManager.Padding, ResourceManager.HeaderWidth, ResourceManager.HeaderHeight), header);

		//menu labels
		if(labels != null||buttons != null) {
			float leftPos = ResourceManager.MenuWidth / 2 - ResourceManager.ButtonWidth / 2;
			float topPos = 2 * ResourceManager.Padding + ResourceManager.HeaderHeight;
			if(labels != null) {
				for(int i = 0; i < labels.Length; i++) {               
					GUI.Label(new Rect(leftPos-50, topPos, ResourceManager.ButtonWidth+100, ResourceManager.ButtonHeight), labels[i]);
					topPos += ResourceManager.ButtonHeight + ResourceManager.Padding;

				}
			}
			//menu buttons
			if(buttons != null) {
				for(int i = 0; i < buttons.Length; i++) {                if(i > 0) topPos += ResourceManager.ButtonHeight + ResourceManager.Padding;
					if(GUI.Button(new Rect(leftPos, topPos, ResourceManager.ButtonWidth, ResourceManager.ButtonHeight), buttons[i])) {
						HandleButton(buttons[i]);
					}
				}
			}
		}

		
		GUI.EndGroup();
	}
	
	protected virtual void SetButtons() {
		//a child class needs to set this for buttons to appear
	}
	
	protected virtual void HandleButton(string text) {
		//a child class needs to set this to handle button clicks
		if(audioElement != null) audioElement.Play(clickSound);
	}
	
	protected virtual float GetMenuHeight() {
		float buttonHeight = 0;
		if(buttons != null) buttonHeight += buttons.Length * ResourceManager.ButtonHeight;
		if(labels!= null) buttonHeight += labels.Length * ResourceManager.ButtonHeight;

		float paddingHeight = 2 * ResourceManager.Padding;
		if(buttons != null) paddingHeight += buttons.Length * ResourceManager.Padding;
		if(labels != null) paddingHeight += labels.Length * ResourceManager.Padding;
		return ResourceManager.HeaderHeight + buttonHeight + paddingHeight;
	}
	
	protected void ExitGame() {
		Application.Quit();
	}
}