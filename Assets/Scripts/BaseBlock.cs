using DG.Tweening;
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
    private Sequence _sequence;

    public virtual void Init(BlockType? type)
    {
        if (type.HasValue)
        {
            Value = type.Value.Value;
            _renderer.color = type.Value.Color;
            _text.text = type.Value.Value.ToString();
        }
    }

    public void TriggerExplosion()
    {
        _sequence = DOTween.Sequence()
            .Append(_renderer.transform.DOScale(new Vector2(1.1f, 1.1f), 0.1f))
            .Append(_renderer.transform.DOScale(new Vector2(0.9f, 0.9f), 0.1f))
            .SetLoops(10)
            .OnComplete(() =>
            {
                _sequence = DOTween.Sequence()
                    .Append(_renderer.transform.DOScale(new Vector2(1.0f, 1.0f), 0.1f))
                    .Join(_renderer.transform.DOShakeRotation(0.5f, 45))
                    ;
            });
    }

    public void ClearText()
    {
        _text.text = "";
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
