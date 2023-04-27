using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace NodeBasedDialogueSystem.com.DialogueSystem.Runtime
{
    [Serializable]
    public class DialogueNodeData
    {
        [FormerlySerializedAs("NodeGUID"), HideInInspector] public string nodeGuid;
        [FormerlySerializedAs("DialogueText")] public string dialogueText;
        [FormerlySerializedAs("Position"), HideInInspector] public Vector2 position;
    }
}