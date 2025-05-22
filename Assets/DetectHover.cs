using UnityEngine;
using UnityEngine.EventSystems;

public class HoverNoClick : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    bool isHovering = false;

    public GameObject daddy;

    // Unity calls this when mouse moves over the UI element
    public void OnPointerEnter(PointerEventData e) => isHovering = true;

    // Unity calls this when mouse moves off
    public void OnPointerExit(PointerEventData e) => isHovering = false;

    void Update()
    {
        // true while hovering, and no mouse button held
        if (isHovering)
        {
            Debug.Log("Hovering but not clicking!");
            daddy.GetComponent<MouseShooting>().enabled = false;
        }
        else
        {
            daddy.GetComponent<MouseShooting>().enabled = true;
        }
    }
}
