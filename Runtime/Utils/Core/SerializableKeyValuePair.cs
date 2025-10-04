using System;
using UnityEngine;

namespace FluxFramework.Utils
{
    // --- Pairs with STRING key (the most common) ---

    [Serializable]
    public struct StringIntPair
    {
        public string Key;
        public int Value;
    }

    [Serializable]
    public struct StringFloatPair
    {
        public string Key;
        public float Value;
    }

    [Serializable]
    public struct StringBoolPair
    {
        public string Key;
        public bool Value;
    }

    [Serializable]
    public struct StringStringPair
    {
        public string Key;
        public string Value;
    }

    [Serializable]
    public struct StringVector2Pair
    {
        public string Key;
        public Vector2 Value;
    }

    [Serializable]
    public struct StringVector3Pair
    {
        public string Key;
        public Vector3 Value;
    }

    // --- Pairs for References to Unity Assets ---

    [Serializable]
    public struct StringGameObjectPair
    {
        public string Key;
        public GameObject Value; // For Prefabs or Scene Objects
    }

    [Serializable]
    public struct StringSpritePair
    {
        public string Key;
        public Sprite Value;
    }

    [Serializable]
    public struct StringMaterialPair
    {
        public string Key;
        public Material Value;
    }
}