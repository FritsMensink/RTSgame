//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;

public class Projectile : MonoBehaviour {
	
	public float velocity = 1;
	public int damage = 1;
	
	private float range = 1;
	private WorldObject target;
	
	void Update () {
		if(HitSomething()) {
			InflictDamage();
			Destroy(gameObject);
		}
		if(range>0) {
			float positionChange = Time.deltaTime * velocity;
			range -= positionChange;
			transform.position += (positionChange * transform.forward);
		} else {
			Destroy(gameObject);
		}
	}
	
	public void SetRange(float range) {
		this.range = range;
	}
	
	public void SetTarget(WorldObject target) {
		this.target = target;
	}
	
	private bool HitSomething() {
		if(target && target.GetSelectionBounds().Contains(transform.position)) return true;
		return false;
	}
	
	private void InflictDamage() {
		if(target) target.TakeDamage(damage);
	}
}