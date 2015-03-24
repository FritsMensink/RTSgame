using UnityEngine;
using System.Collections;

public class Controls : Menu {
	MainMenu m;
	protected override void SetButtons () {
		labels = new string[] {"Camera bewegen= 'W','S','A','D' of 'muis naar zijkant'","Camera rotatie= 'ALT+rechts-klik+muis beweging'","Unit selecteren= 'links-klik'","Unit bewegen= 'rechts-klik'","Gebouw bouwen= 'links-klik'","Bouwer selecteren= 'H'"};
		buttons = new string[] {"Return"};
		Screen.showCursor = true;
	}

	protected override void Start ()
	{
		base.Start ();
		this.enabled = false;
		m =(MainMenu) GetComponent(typeof(MainMenu));
	}


	protected override void HandleButton (string text) {
		base.HandleButton(text);
		switch(text) {
		case "Return": ToMainMenu(); break;
		default: break;
		}
	}
	
	public void ToMainMenu() {
		m.enabled = true;
		this.enabled = false;
	}
	
}