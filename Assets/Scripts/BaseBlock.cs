using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public abstract class BaseBlock : MonoBehaviour
{
    public int Value;
    [SerializeField] protected SpriteRenderer _renderer;
    [SerializeField] protected TextMeshPro _text;
    public Vector2 Pos => transform.position;
    public Node Node;
    public BaseBlock MergingBlock;
    public bool Merging;

    public virtual void Init(BlockType type)
    {
        Value = type.Value;
        _renderer.color = type.Color;
        _text.text = type.Value.ToString();
    }

    public void SetBlock(Node node)
    {
        if (Node != null)
            Node.OccupiedBlock = null;

        Node = node;
        Node.OccupiedBlock = this;
    }

    public virtual void MergeBlock(BaseBlock blockToMergeWith)
    {
        // Set the block we are merging with
        MergingBlock = blockToMergeWith;

        // Set current node as unoccupied to allow blocks to use it
        Node.OccupiedBlock = null;

        blockToMergeWith.Merging = true;
    }

    public bool CanMerge(int value) => value == Value && !Merging && MergingBlock == null;
}
