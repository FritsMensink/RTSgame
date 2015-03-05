using UnityEngine;
using System.Collections;

namespace RTS {
public static class ResourceManager {

		public static float ScrollSpeed { get { return 100; } }
		public static float RotateSpeed { get { return 100; } }
		public static float RotateAmount { get { return 10; } }

		public static int ScrollWidth { get { return 15; } }

		public static float MinCameraHeight { get { return 10; } }
		public static float MaxCameraHeight { get { return 50; } }

		//door het creeren van een private variable invalidPosition zorgen we er voor dat er geen nieuwe Vector3
		//aangemaakt moet worden elke keer InvalidPosition wordt aangeroepen.
		private static Vector3 invalidPosition = new Vector3(-9999, -9999, -9999);
		public static Vector3 InvalidPosition { get { return invalidPosition; } }

		private static Bounds invalidBounds = new Bounds(new Vector3(-99999, -99999, -99999), new Vector3(0, 0, 0));
		public static Bounds InvalidBounds { get { return invalidBounds; } }

		private static GUISkin selectBoxSkin;
		public static GUISkin SelectionBoxSkin { get { return selectBoxSkin; } }
				
		public static int BuildSpeed { get { return 2; } }

		private static GameObjectList gameObjectList;

		private static Texture2D healthyTexture, damagedTexture, criticalTexture;
		public static Texture2D HealthyTexture { get { return healthyTexture; } }
		public static Texture2D DamagedTexture { get { return damagedTexture; } }
		public static Texture2D CriticalTexture { get { return criticalTexture; } }

		public static void StoreSelectBoxItems(GUISkin skin, Texture2D healthy, Texture2D damaged, Texture2D critical) {
			selectBoxSkin = skin;
			healthyTexture = healthy;
			damagedTexture = damaged;
			criticalTexture = critical;
		}

		public static void SetGameObjectList(GameObjectList objectList) {
			gameObjectList = objectList;
		}

		public static GameObject GetBuilding(string name) {
			return gameObjectList.GetBuilding(name);
		}
		
		public static GameObject GetUnit(string name) {
			return gameObjectList.GetUnit(name);
		}
		
		public static GameObject GetWorldObject(string name) {
			return gameObjectList.GetWorldObject(name);
		}
		
		public static GameObject GetPlayerObject() {
			return gameObjectList.GetPlayerObject();
		}
		
		public static Texture2D GetBuildImage(string name) {
			return gameObjectList.GetBuildImage(name);
		}
}
}