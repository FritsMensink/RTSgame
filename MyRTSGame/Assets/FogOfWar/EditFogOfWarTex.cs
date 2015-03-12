using UnityEngine;
using System.Collections;
using System.IO;

public class EditFogOfWarTex : MonoBehaviour {

	public  Texture2D FogOfWarTex;

	public void drawCircle(int cx, int cy, int r)
	{
		int x, y, px, nx, py, ny, d;
		for (x = 0; x <= r; x++)
		{
			d = (int)Mathf.Ceil(Mathf.Sqrt(r * r - x * x));
			for (y = 0; y <= d; y++)
			{
				px = cx + x;
				nx = cx - x;
				py = cy + y;
				ny = cy - y;
				FogOfWarTex.SetPixel(px, py, Color.green);
				FogOfWarTex.SetPixel(nx, py, Color.green);
				FogOfWarTex.SetPixel(px, ny, Color.green);
				FogOfWarTex.SetPixel(nx, ny, Color.green);

			}
		}
		FogOfWarTex.Apply();
		//var bytes = FogOfWarTex.EncodeToPNG ();
		//File.WriteAllBytes(Application.dataPath + "/Texture/FogOfWarTex.png", bytes);
	} 
	// Use this for initialization
	void Start () {
		//Reset ();
		drawCircle (0, 0, 120);

		//var bytes = FogOfWarTex.EncodeToPNG ();
		//File.WriteAllBytes(Application.dataPath + "/Texture/FogOfWarTex.png", bytes);
	}
	void Reset(){
		int xx = 0;
		int yy = 0;
		while (xx<FogOfWarTex.height) {
			while (yy<FogOfWarTex.width) {
				//if(FogOfWarTex.GetPixel(xx,yy).GetHashCode().Equals(Color.green.GetHashCode())){
					FogOfWarTex.SetPixel(xx,yy,Color.blue);
				//}
				yy++;
			}
			xx++;
		}
		FogOfWarTex.Apply ();
	}
	// Update is called once per frame
	void Update () {
	
	}
}
