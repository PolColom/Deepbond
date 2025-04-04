using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NoiseData", menuName = "World Generation/Noise Data")]
public class NoiseDataSO : ScriptableObject
{
    public Vector2 offset;
    public float startFrequency = 0.02f;
    public float persistance = 0.5f;
    public float frequencyModifier = 2f;
    public int octaves = 4;
    public float noiseRangeMin = 10;
    public float noiseRangeMax = 30;
}