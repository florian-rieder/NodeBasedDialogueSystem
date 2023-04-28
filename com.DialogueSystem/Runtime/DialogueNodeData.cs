using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace NodeBasedDialogueSystem.com.DialogueSystem.Runtime
{
    [Serializable]
    public class DialogueNodeData
    {
        [FormerlySerializedAs("NodeGUID"), HideInInspector] public string nodeGuid;
        [FormerlySerializedAs("DialogueText")] public List<string> dialogueText;
        [FormerlySerializedAs("Position"), HideInInspector] public Vector2 position;
    }
}