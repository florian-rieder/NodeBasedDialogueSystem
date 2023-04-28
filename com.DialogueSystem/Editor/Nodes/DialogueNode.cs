using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

namespace NodeBasedDialogueSystem.com.DialogueSystem.Editor.Nodes
{
    public class DialogueNode : Node
    {
        public string GUID;
        public List<string> DialogueText;
        public bool EntryPoint = false;
    }
}