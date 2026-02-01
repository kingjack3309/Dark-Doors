using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Anchor : MonoBehaviour
{
    public Direction direction;

    public bool isOccupied = false;

}

public enum Direction
{

    Top,

    Bottom,

    Left,
    
    Right

}