using UnityEditor.Experimental.GraphView;

namespace NodeBasedDialogueSystem.com.DialogueSystem.Editor.Nodes
{
    public class DialogueNode : Node
    {
        public string DialogueText;
        public string GUID;
        public bool EntryPoint = false;
    }
}