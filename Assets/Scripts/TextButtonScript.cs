using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextButtonScript : MonoBehaviour
{
    [SerializeField] private string text;
    [SerializeField] private TextMeshProUGUI textMesh;

    // Start is called before the first frame update
    void Start()
    {
        textMesh.SetText(text);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
