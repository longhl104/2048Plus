using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Block : BaseBlock
{
    public ParticleSystem ExplosionParticle;

    public override void Init(BlockType type)
    {
        base.Init(type);
        ParticleSystem.MainModule psmain = ExplosionParticle.main;
        psmain.startColor = type.Color;
    }

    public IEnumerator Destroy()
    {
        ExplosionParticle.Play();
        _renderer.enabled = false;

        if (_text.gameObject != null)
            Destroy(_text.gameObject);

        yield return new WaitForSeconds(ExplosionParticle.main.startLifetime.constantMax);
        if (gameObject != null)
            Destroy(gameObject);
    }
}
