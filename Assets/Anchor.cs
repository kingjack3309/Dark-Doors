using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class Anchor : MonoBehaviour
{
    public Direction direction;

    public bool isOccupied = false;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Color color = isOccupied ? Color.red : Color.green;
        Gizmos.color = color;

        Vector3 dir = Vector3.zero;
        switch (direction)
        {
            case Direction.Top: dir = Vector3.up; break;
            case Direction.Bottom: dir = Vector3.down; break;
            case Direction.Left: dir = Vector3.left; break;
            case Direction.Right: dir = Vector3.right; break;
        }

        Gizmos.DrawRay(transform.position, dir * 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
#endif

}

public enum Direction
{

    Top,

    Bottom,

    Left,
    
    Right

}