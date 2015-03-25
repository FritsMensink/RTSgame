using UnityEngine;
using System.Collections;

public class WarFactory : Building {

	protected override void Start () {
		base.Start();
		canCreateUnits = true;
			actions = new string[] { "Tank","TankII" };
	}

	public override void PerformAction(string actionToPerform) {
		base.PerformAction(actionToPerform);
		CreateUnit(actionToPerform);
	}

	public override bool hasRallyPoint() {
		return true;
	}
}