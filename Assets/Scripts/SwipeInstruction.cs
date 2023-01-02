using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SwipeInstruction : MonoBehaviour
{
    [SerializeField] private GameObject _finger;

    private RectTransform _rectTransform;
    private Sequence _sequence;

    // Start is called before the first frame update
    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, _rectTransform.rect.width);
        _finger.SetActive(false);
    }

    public bool IsPlaying()
    {
        if (_sequence == null)
            return false;

        return _sequence.IsPlaying();
    }

    public void Stop()
    {
        if (_sequence == null || !IsPlaying())
            return;

        _sequence.OnComplete(() =>
        {
            _finger.SetActive(false);
        });
        _sequence.SetAutoKill(true);
    }

    public void Swipe(Vector2 dir)
    {
        _finger.SetActive(true);
        Vector2 initialPos = new();
        Vector2 toPos = new();
        if (dir == Vector2.up)
        {
            initialPos = new Vector2(0, -_rectTransform.rect.height / 2);
            toPos = new Vector2(0, _rectTransform.rect.height / 2);
        }
        else if (dir == Vector2.down)
        {
            initialPos = new Vector2(0, _rectTransform.rect.height / 2);
            toPos = new Vector2(0, -_rectTransform.rect.height / 2);
        }
        else if (dir == Vector2.left)
        {
            initialPos = new Vector2(_rectTransform.rect.width / 2, 0);
            toPos = new Vector2(-_rectTransform.rect.width / 2, 0);
        }
        else if (dir == Vector2.right)
        {
            initialPos = new Vector2(-_rectTransform.rect.width / 2, 0);
            toPos = new Vector2(_rectTransform.rect.width / 2, 0);
        }

        _finger.GetComponent<RectTransform>().anchoredPosition = initialPos;
        _sequence = DOTween.Sequence().SetAutoKill(false);
        _sequence.Append(_finger.transform.DOLocalMove(toPos, duration: 1));
        _sequence.PrependInterval(0.5f);

        _sequence.OnComplete(() =>
        {
            _sequence.Restart();
        });
    }
}
