using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    // Start is called before the first frame update
    void Start()
    {
        if (!PlayerPrefs.HasKey(StorageKeys.MUSIC_VOLUME))
        {
            Save(1);
        }

        Load();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ChangeVolume()
    {
        AudioListener.volume = _slider.value;
        Save();
    }

    private void Load()
    {
        _slider.value = PlayerPrefs.GetFloat(StorageKeys.MUSIC_VOLUME);
    }

    private void Save(float? value = null)
    {
        PlayerPrefs.SetFloat(StorageKeys.MUSIC_VOLUME, value.HasValue ? value.Value : _slider.value);
    }
}
