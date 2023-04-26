using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace NodeBasedDialogueSystem.com.DialogueSystem.Runtime
{
    [Serializable]
    public class CommentBlockData
    {
        [FormerlySerializedAs("ChildNodes")] public List<string> childNodes = new List<string>();
        [FormerlySerializedAs("Position")] public Vector2 position;
        [FormerlySerializedAs("Title")] public string title = "Comment Block";
    }
}