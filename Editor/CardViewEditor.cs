using UnityEngine;
using UnityEditor;
using Prototype.Cards;

[CustomEditor(typeof(CardView))]
public class CardViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CardView cv = (CardView)target;
        GUILayout.Space(6);
        GUILayout.Label("Testing", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Draw"))
        {
            Undo.RecordObject(cv.gameObject, "Inspector Draw");
            cv.InspectorDraw();
            EditorUtility.SetDirty(cv);
        }
        if (GUILayout.Button("Discard"))
        {
            Undo.RecordObject(cv.gameObject, "Inspector Discard");
            cv.InspectorDiscard();
            EditorUtility.SetDirty(cv);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);
        EditorGUILayout.HelpBox("Set `testDrawParent` and `testDiscardParent` on the CardView to control where the card will be moved when using the buttons.", MessageType.Info);
    }
}
