using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NumberCounterUpdater : MonoBehaviour
{
    [SerializeField] private string _prefix;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private int _countFPS = 30;
    [SerializeField] private float _duration = 1f;
    [SerializeField] private string _numberFormat = "N0";

    private int _value;
    public int Value
    {
        get
        {
            return _value;
        }

        set
        {
            UpdateText(value);
            _value = value;
        }
    }

    private Coroutine _countingCoroutine;

    private void UpdateText(int newValue)
    {
        if (_countingCoroutine != null)
        {
            StopCoroutine(_countingCoroutine);
        }

        StartCoroutine(CountText(newValue));
    }

    private IEnumerator CountText(int newValue)
    {
        WaitForSeconds wait = new WaitForSeconds(1f / _countFPS);
        int previousValue = _value;
        int stepAmount;
        if (newValue - previousValue < 0)
        {
            stepAmount = Mathf.FloorToInt((newValue - previousValue) / (_countFPS * _duration));
        }
        else
        {
            stepAmount = Mathf.CeilToInt((newValue - previousValue) / (_countFPS * _duration));
        }

        if (previousValue < newValue)
        {
            while (previousValue < newValue)
            {
                previousValue += stepAmount;
                if (previousValue > newValue)
                {
                    previousValue = newValue;
                }

                _text.SetText((_prefix ?? "") + previousValue.ToString(_numberFormat));
                yield return wait;
            }
        }
        else
        {
            while (previousValue > newValue)
            {
                previousValue += stepAmount;
                if (previousValue < newValue)
                {
                    previousValue = newValue;
                }

                _text.SetText((_prefix ?? "") + previousValue.ToString(_numberFormat));
                yield return wait;
            }
        }
    }

    public void SetValueInstantly(int value)
    {
        _value = value;
        _text.SetText((_prefix ?? "") + _value.ToString(_numberFormat));
    }
}
