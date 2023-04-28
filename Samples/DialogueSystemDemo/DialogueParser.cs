using System.Collections.Generic;
using System.Linq;
using NodeBasedDialogueSystem.com.DialogueSystem.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NodeBasedDialogueSystem.Samples.DialogueSystemDemo
{
    public class DialogueParser : MonoBehaviour
    {
        [SerializeField] private DialogueContainer dialogue;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Button choicePrefab;
        [SerializeField] private Transform buttonContainer;

        private void Start()
        {
            var narrativeData = dialogue.nodeLinks.First(); //Entrypoint node
            ProceedToNarrative(narrativeData.targetNodeGuid);
        }

        void ProceedToNarrative(string narrativeDataGuid)
        {
            var text = dialogue.dialogueNodeData.Find(x => x.nodeGuid == narrativeDataGuid).dialogueText;
            IEnumerable<NodeLinkData> choices = dialogue.nodeLinks.Where(x => x.baseNodeGuid == narrativeDataGuid);
            
            var processedText = ProcessPropertiesArray(text);
            dialogueText.text = string.Join("\n", processedText);
            Button[] buttons = buttonContainer.GetComponentsInChildren<Button>();
            foreach (var t in buttons)
                Destroy(t.gameObject);

            foreach (var choice in choices) {
                var button = Instantiate(choicePrefab, buttonContainer);
                button.GetComponentInChildren<Text>().text = ProcessProperties(choice.portName);
                button.onClick.AddListener(() => ProceedToNarrative(choice.targetNodeGuid));
            }
        }

        string ProcessProperties(string text) => dialogue.exposedProperties.Aggregate(text, (current, exposedProperty) => current.Replace($"[{exposedProperty.propertyName}]", exposedProperty.propertyValue));
        List<string> ProcessPropertiesArray(List<string> text)
        {
            dialogue.exposedProperties.ForEach(x => text = text.Select(y => y.Replace($"[{x.propertyName}]", x.propertyValue)).ToList());
            return text;
        }
    }
}