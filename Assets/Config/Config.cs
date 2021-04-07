using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Config", menuName = "General Config", order = 1)]
public class Config : ScriptableObject {

    public enum Shape
    {
        Circle,
        Square
    }

    [Serializable]
    public struct ShapeConfig
    {
        public Vector2 initialPos;
        public float size;
        public Shape shape;
    }

    [SerializeField]
    public List<ShapeConfig> allShapesToSpawn;

    [SerializeField]
    public bool enableInputPrediction;

    [SerializeField]
    public bool enablDeadReckoning;

}
