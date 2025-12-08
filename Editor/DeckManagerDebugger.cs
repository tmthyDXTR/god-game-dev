using UnityEngine;
using UnityEditor;
using Prototype.Cards;
using System.Collections.Generic;

public class DeckManagerDebugger : EditorWindow
{
    DeckManager deck;
    int drawCount = 1;
    Vector2 scroll;

    [MenuItem("Tools/Deck Debugger %d")]
    public static void OpenWindow()
    {
        var w = GetWindow<DeckManagerDebugger>("Deck Debugger");
        w.minSize = new Vector2(320, 200);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Deck Manager Debugger", EditorStyles.boldLabel);

        deck = (DeckManager)EditorGUILayout.ObjectField("DeckManager", deck, typeof(DeckManager), true);

        if (deck == null)
        {
            if (GUILayout.Button("Find DeckManager in Scene"))
            {
                deck = FindObjectOfType<DeckManager>();
                if (deck == null) Debug.LogWarning("No DeckManager found in the active scene.");
            }
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Draw:{deck.DrawPileCount}  Discard:{deck.DiscardPileCount}  Hand:{deck.HandCount}");

        EditorGUILayout.BeginHorizontal();
        drawCount = EditorGUILayout.IntField("Draw Count", drawCount);
        if (GUILayout.Button("Draw"))
        {
            Undo.RecordObject(deck, "Deck Draw");
            deck.DrawToHand(Mathf.Max(1, drawCount));
            Debug.Log($"Deck Debugger: Drew {drawCount} card(s)");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset Deck"))
        {
            Undo.RecordObject(deck, "Deck Reset");
            deck.ResetDeck();
            Debug.Log("Deck Debugger: ResetDeck called");
        }
        if (GUILayout.Button("Shuffle Draw Pile"))
        {
            Undo.RecordObject(deck, "Shuffle Draw Pile");
            deck.ShuffleDrawPile();
        }
        if (GUILayout.Button("Reshuffle Discard"))
        {
            Undo.RecordObject(deck, "Reshuffle Discard");
            deck.ReshuffleDiscardIntoDraw();
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Log State"))
        {
            deck.LogState();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hand Contents:");
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(180));
        List<CardSO> hand = deck.GetHand();
        if (hand != null)
        {
            foreach (var c in hand)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(c != null ? c.name : "<null>");
                if (GUILayout.Button("Discard", GUILayout.Width(70)))
                {
                    deck.Discard(c);
                    Debug.Log($"Deck Debugger: Discarded '{(c!=null?c.name:"<null>")}'");
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
    }
}
