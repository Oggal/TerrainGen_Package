using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldGen))]
public class WolrdGenInspector : Editor
{
	[SerializeField]
	bool showDefault = true;
	bool cityInfo = false;
	
    public override void OnInspectorGUI()
    {
        WorldGen gen = (WorldGen)target;
		
		if(GUILayout.Button("Screen Cap"))
		{
			ScreenCapture.CaptureScreenshot(Application.dataPath + 
				"/Oggal/ScreenShots/" + ((System.DateTime.Today.ToShortDateString()).Replace("/","-"))
				+"_"+((System.DateTime.Now.ToShortTimeString()).Replace(" ", "").Replace(":",""))+".png", 1);
		}

		showDefault = GUILayout.Toggle(showDefault, "Show Default Inspector");
		if(showDefault)
			DrawDefaultInspector();

		//Generation Settings
		EditorGUILayout.LabelField(new GUIContent("Generation Settings"));
		gen.drawRadius = GUILayout.Toggle(gen.drawRadius, 
			new GUIContent("Draw Radius", "Draws a Yellow out line of the world and it's tiles"));

		gen.UseSeed = GUILayout.Toggle(gen.UseSeed,
			new GUIContent("Use Seed", "Forces the generator to use supplied seed inplace of a random one."));

		gen.Seed = EditorGUILayout.IntField(new GUIContent("Seed", "Any world with an identical seed will be identical"),gen.Seed);

		gen.BuildOnStart = GUILayout.Toggle(gen.BuildOnStart,
			new GUIContent("Build On Start", "When selected a new world will be built on start. Leave unchecked for quicker starts. " +
				"\nRequired to be checked for endless distance."));


		//POI and City Placement Options
		cityInfo =EditorGUILayout.BeginToggleGroup("Place Cities", cityInfo);
		gen.UseCityGrid = GUILayout.Toggle(gen.UseCityGrid,
			new GUIContent("Use Grid Method", "Places Cities on a fixed Infinte Grid"));
		EditorGUILayout.EndToggleGroup();




		//Update Materials
        if(GUILayout.Button(new GUIContent("Update Materials","Updates the materials on the meshs with out regenerating the world.\nNOT FULLY IMPLEMENTED")))
        {
          foreach( MeshRenderer m in gen.gameObject.GetComponentsInChildren<MeshRenderer>()){
				m.materials = gen.mats;
			}
        }
		//Rebuild World
		if (GUILayout.Button(new GUIContent("Build World","Builds a new World")))
        {
            gen.BuildWorld();
        }
		//Clear All Children
        if (GUILayout.Button(new GUIContent("Clear World","Removes all children from the world data object, Clearing the world.")))
            gen.ClearChildren();

		if (GUILayout.Button(new GUIContent("Move West")))
			gen.MoveX(false);
		if (GUILayout.Button(new GUIContent("Move East")))
			gen.MoveX(true);


		gen.TreeMenu();

		SceneView.RepaintAll();
    }   

}
