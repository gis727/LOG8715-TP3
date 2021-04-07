// Source: http://wiki.unity3d.com/index.php/SerializableDictionary

using System;
 
using UnityEngine;
 
// ---------------
//  String => Int
// ---------------
[Serializable]
public class StringIntDictionary : SerializableDictionary<string, int> {}
 
// ---------------
//  GameObject => Float
// ---------------
[Serializable]
public class GameObjectFloatDictionary : SerializableDictionary<GameObject, float> {}

[Serializable]

public class StringBoolDictionary : SerializableDictionary<string, bool> { }

