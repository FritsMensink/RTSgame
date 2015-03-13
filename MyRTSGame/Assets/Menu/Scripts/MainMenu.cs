using UnityEngine;
using System.Collections;
using RTS;

public class MainMenu : Menu {
	
	protected override void SetButtons () {
		buttons = new string[] {"New Game", "Exit Game"};
		Screen.showCursor = true;
	}
	
	protected override void HandleButton (string text) {
		base.HandleButton(text);
		switch(text) {
		case "New Game": NewGame(); break;
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
}