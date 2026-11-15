using UnityEngine;

/// <summary>A clickable test object that counts how many times WorldClicker clicked it.</summary>
public class ClickCounter : MonoBehaviour, IClickable
{
    public int Clicks { get; private set; }

    public void OnClicked()
    {
        Clicks++;
    }
}
