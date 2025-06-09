using UnityEngine;
using UnityEngine.EventSystems;

public class HoverNoClick : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    public bool isHovering = false;
    public bool isPressing = false;

    public GameObject daddy;

    public void OnPointerEnter(PointerEventData e)
    {
        daddy.GetComponent<MouseShooting>().canShoot = false;
        daddy.GetComponent<MouseShooting>().isShooting = true;
        daddy.GetComponent<MouseShooting>().canShoot = false;
    }
    public void OnPointerExit(PointerEventData e)
    {
        daddy.GetComponent<MouseShooting>().canShoot = false;
        daddy.GetComponent<MouseShooting>().isShooting = false;
        daddy.GetComponent<MouseShooting>().canShoot = true;
    }

    public void OnPointerDown(PointerEventData e)
    {
        isPressing = true;
        isHovering = true; // Treat as "hovering" if touched/clicked
    }

    public void OnPointerUp(PointerEventData e)
    {
        isPressing = false;

        // Optional: Set to false here if you want to stop shooting after press
        // isHovering = false;  // Only do this if you want to stop instantly
    }

    void Update()
    {
        if (isHovering || isPressing)
        {
            Debug.Log("Touch or Hover active!");
            
        }
        else
        {
            
        }
    }
}
