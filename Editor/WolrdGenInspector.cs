using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldGen))]
public class WolrdGenInspector : Editor
{
	[SerializeField]
	bool showDefault = true;
	[SerializeField]
	bool foldout_gen_settings = false;

    public override void OnInspectorGUI()
    {
        WorldGen gen = (WorldGen)target;
		
		// if(GUILayout.Button("Screen Cap"))
		// {
		// 	ScreenCapture.CaptureScreenshot(Application.dataPath + 
		// 		"/Oggal/ScreenShots/" + ((System.DateTime.Today.ToShortDateString()).Replace("/","-"))
		// 		+"_"+((System.DateTime.Now.ToShortTimeString()).Replace(" ", "").Replace(":",""))+".png", 1);
		// }

		showDefault = EditorGUILayout.Foldout(showDefault, "Show Default Inspector");
		if(showDefault)
			DrawDefaultInspector();

		//Generation Settings
		if(foldout_gen_settings = EditorGUILayout.Foldout(foldout_gen_settings, new GUIContent("Generation Settings"), true))
		{
			gen.UseSeed = GUILayout.Toggle(gen.UseSeed,
				new GUIContent("Use Seed", "Forces the generator to use supplied seed inplace of a random one."));

			gen.Seed = EditorGUILayout.IntField(new GUIContent("Seed", "Any world with an identical seed will be identical"),gen.Seed);

			gen.BuildOnStart = GUILayout.Toggle(gen.BuildOnStart,
				new GUIContent("Build On Start", "When selected a new world will be built on start. Leave unchecked for quicker starts. " +
					"\nRequired to be checked for endless distance."));
		
			//POI and City Placement Options
			gen.SpawnPOIs = GUILayout.Toggle(gen.SpawnPOIs,
				new GUIContent("Spawn POI", "Generate and place Points of Interest"));
		}



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

		SceneView.RepaintAll();
    }   

	public void OnSceneGUI(){
		 //Handles.DrawCamera(new Rect(0,0,500,500), Camera.current);
         //Handles.PositionHandle(Vector3.zero, Quaternion.identity);
		 WorldGen Gen = target as WorldGen;
		 if(Gen == null) return;

		 Handles.color = Color.red;

		 Handles.BeginGUI();
		 if(GUILayout.Button("Build World",GUILayout.MaxWidth(100))){
			Gen.BuildWorld();
		 }
		 Handles.EndGUI();
	}

	[DrawGizmo(GizmoType.InSelectionHierarchy)]
	static void DeselectedGizmo(Transform obj,GizmoType gizmoType){
		
		WorldGen target = obj.gameObject.GetComponent< WorldGen>();
		if(target == null) return;
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(obj.position,new Vector3((0.5f+target.Radius)*target.TileSize,2,(0.5f+target.Radius)*target.TileSize)*2);
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(obj.position,new Vector3(target.TileSize * target.Radius,100,target.TileSize * target.Radius));
		foreach( MeshCollider mCol in target.GetComponentsInChildren<MeshCollider>()){
			Gizmos.color = Color.cyan;
			Gizmos.DrawMesh(mCol.sharedMesh,mCol.transform.position);
		}
	}

}