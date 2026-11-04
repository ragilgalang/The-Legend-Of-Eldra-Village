using UnityEngine;

[CreateAssetMenu(menuName = "Eldara/Dialogue")]
public class DialogueData : ScriptableObject
{
    [System.Serializable] public class Line
    {
        public string speaker;
        [TextArea(2, 4)] public string text;
    }
    [System.Serializable] public class Choice
    {
        public string text;
        public DialogueData next;
    }

    public Line[] lines;
    public Choice[] choices;   // tampil setelah baris terakhir (boleh kosong)
}

