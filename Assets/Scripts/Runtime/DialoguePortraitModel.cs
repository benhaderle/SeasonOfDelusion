 using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CreateNeptune;
using UnityEngine;
using AYellowpaper.SerializedCollections;

public class DialoguePortraitModel : Singleton<DialoguePortraitModel>
{
    [SerializedDictionary("Character Name", "Portraits")] [SerializeField] private SerializedDictionary<string, SerializedDictionary<string, Sprite>> characterPortraits = new();

    public Sprite GetPortrait(string name, string tag = "")
    {
        if (characterPortraits.ContainsKey(name))
        {
            if (string.IsNullOrWhiteSpace(tag) || !characterPortraits[name].ContainsKey(tag))
            {
                return characterPortraits[name].ElementAt(0).Value;
            }
            else
            {
                return characterPortraits[name][tag];
            }
        }
        else
        {
            return TeamModel.Instance.GetPortrait(name, tag);
        }
    }
}

[Serializable]
public class CharacterPortraits
{
    // public SerializableDictionary<string, Sprite> portraits = new();
}
