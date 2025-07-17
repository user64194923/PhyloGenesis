using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Rule
{
    public char character;
    public string result;
}

public enum TreePreset
{
    Custom,
    MapleLike,
    PineLike,
    FernLike
}

public class LSystemGenerator : MonoBehaviour
{
    [Header("L-System Configuration")]
    public TreePreset preset = TreePreset.Custom;
    public string axiom = "F";
    public Rule[] rules;
    [Range(0, 10)]
    public int iterations = 5;

    [Header("Preview")]
    [TextArea(3, 10)]
    public string generatedSequence;

    public string Generate()
    {
        ApplyPresetIfNeeded();

        string current = axiom;

        for (int i = 0; i < iterations; i++)
        {
            string next = "";
            foreach (char c in current)
            {
                bool replaced = false;
                foreach (Rule rule in rules)
                {
                    if (c == rule.character)
                    {
                        next += rule.result;
                        replaced = true;
                        break;
                    }
                }
                if (!replaced)
                    next += c.ToString();
            }
            current = next;
        }

        generatedSequence = current;
        return current;
    }

    private void ApplyPresetIfNeeded()
    {
        if (preset == TreePreset.Custom)
            return;

        List<Rule> presetRules = new List<Rule>();

        switch (preset)
        {
            case TreePreset.MapleLike:
                axiom = "X";
                iterations = 5;
                presetRules.Add(new Rule { character = 'X', result = "F[+X][-X]FX" });
                presetRules.Add(new Rule { character = 'F', result = "FF" });
                break;

            case TreePreset.PineLike:
                axiom = "X";
                iterations = 6;
                presetRules.Add(new Rule { character = 'X', result = "F[+X]F[-X]+X" });
                presetRules.Add(new Rule { character = 'F', result = "FF" });
                break;

            case TreePreset.FernLike:
                axiom = "X";
                iterations = 4;
                presetRules.Add(new Rule { character = 'X', result = "F[+X]F[-X]FX" });
                presetRules.Add(new Rule { character = 'F', result = "FF" });
                break;
        }

        rules = presetRules.ToArray();
    }
}
