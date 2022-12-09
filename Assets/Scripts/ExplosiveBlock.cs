using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ExplosiveBlock : BaseBlock
{
    [SerializeField] private Animator _animator;
    public Action ExploseAction;

    public override void Init(BlockType type)
    {
        Value = type.Value;
        _text.text = type.Value.ToString();
    }

    public void TriggerExplosion()
    {
        _animator.SetTrigger("TriggerExplosion");
    }

    public void Explose()
    {
        ExploseAction();
    }
}
