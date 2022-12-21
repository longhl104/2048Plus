using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ExplosiveBlock : BaseBlock
{
    private Sequence _sequence;

    public override void Init(BlockType type)
    {
        Value = type.Value;
        _text.text = type.Value.ToString();
        _sequence = DOTween.Sequence()
            .Append(_renderer.transform.DOScale(new Vector2(1.1f, 1.1f), 0.25f))
            .Append(_renderer.transform.DOScale(new Vector2(0.9f, 0.9f), 0.25f))
            .SetLoops(-1);
    }

    public void TriggerExplosion(TweenCallback onCompleteAction)
    {
        _sequence = DOTween.Sequence()
            .Append(_renderer.transform.DOScale(new Vector2(1.1f, 1.1f), 0.1f))
            .Append(_renderer.transform.DOScale(new Vector2(0.9f, 0.9f), 0.1f))
            .SetLoops(10)
            .OnComplete(() =>
            {
                _sequence = DOTween.Sequence()
                    .Append(_renderer.transform.DOScale(new Vector2(3.5f, 3.5f), 0.5f))
                    .Join(_renderer.transform.DOShakeRotation(0.5f, 45))
                    .OnComplete(onCompleteAction);
            });
    }
}
