using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace NodeBasedDialogueSystem.com.DialogueSystem.Runtime
{
    [Serializable]
    public class DialogueContainer : ScriptableObject
    {
        // node link data does not need to be public to the editor, it's just confusing to read.
        [FormerlySerializedAs("NodeLinks")] public List<NodeLinkData> nodeLinks = new List<NodeLinkData>();
        [FormerlySerializedAs("DialogueNodeData")] public List<DialogueNodeData> dialogueNodeData = new List<DialogueNodeData>();
        [FormerlySerializedAs("ExposedProperties")] public List<ExposedProperty> exposedProperties = new List<ExposedProperty>();
        [FormerlySerializedAs("CommentBlockData")] public List<CommentBlockData> commentBlockData = new List<CommentBlockData>();
    }
}