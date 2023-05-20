using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Image ad_image;
    [SerializeField] TMP_Text ad_name;
    string ad_url;
    void Start()
    {
        // Create a new button
        Button button = gameObject.AddComponent<Button>();

        // Add a click event handler
        button.onClick.AddListener(() => {
            Debug.Log($"Button clicked! Open {ad_url}");
            Application.OpenURL(ad_url);
        });
    }

    public void SetAd(string name, string url, byte[] image){
        ad_name.text = name;
        ad_url = url;
        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(image);
        ad_image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
         // Get the current color of the image
        Color color = ad_image.color;

        // Set the alpha component of the color to 0.5 (50%)
        color.a = 0.5f;

        // Set the color of the image to the modified color
        ad_image.color = color;
    }

}
