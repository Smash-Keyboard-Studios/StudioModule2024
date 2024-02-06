using Palmmedia.ReportGenerator.Core.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public interface IInteractable
{
    public void Interact();
}
public class PlayerInteractionScript : MonoBehaviour
{

    public float InteractionDistance = 1.75f;
    public KeyCode interactKeycode = KeyCode.E;
    public Image Crosshair;
    public TMP_Text tutorialText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, InteractionDistance))
        {
            if (hit.transform.CompareTag("InteractableObject") || hit.transform.CompareTag("MoveableObject"))
            {
                Crosshair.color = Color.red;
                tutorialText.enabled = true;
                tutorialText.text = "Press '" + interactKeycode.ToString() + "' to interact with " + hit.transform.name + "";
                if (Input.GetKeyDown(interactKeycode))
                {
                    if (hit.collider.gameObject.TryGetComponent(out IInteractable objInteraction))
                    {
                        objInteraction.Interact();
                    }
                    else if (hit.collider.gameObject.TryGetComponent(out GrabObject grabbableObject))
                    {
                        Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
                        grabbableObject.Grab(grabbableObject.transform);
                    }
                }
            }
            else
            {
                Crosshair.color = Color.green;
                tutorialText.enabled = false;
            }
            if (hit.transform.CompareTag("MoveableObject"))
            {
                Crosshair.color = Color.red;

            }
        }
        else
        {
            Crosshair.color = Color.green;
            tutorialText.enabled = false;
        }

    }
}
