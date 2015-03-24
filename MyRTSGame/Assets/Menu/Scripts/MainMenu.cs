using UnityEngine;
using System.Collections;
using RTS;

public class MainMenu : Menu {
	Controls c;
	protected override void SetButtons () {
		buttons = new string[] {"New Game","Controls","Exit Game"};
		Screen.showCursor = true;
	}
	protected override void Start ()
	{
		base.Start ();
		c =(Controls) GetComponent(typeof(Controls));
	}
	protected override void HandleButton (string text) {
		base.HandleButton(text);
		switch(text) {
		case "New Game": NewGame(); break;
		case "Controls": Controls(); break;
		case "Exit Game": ExitGame(); break;
		default: break;
		}
	}
	
	private void NewGame() {
		ResourceManager.MenuOpen = false;
		Application.LoadLevel("Level01");
		//makes sure that the loaded level runs at normal speed
		Time.timeScale = 1.0f;
	}

	private void Controls ()
	{
		this.enabled = false;
		c.enabled = true;
	}
}