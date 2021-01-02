using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProjectileBase))]
public class InteractStandard : MonoBehaviour
{
    [Tooltip("LifeTime of the spell")]
    public float maxLifeTime = 1f;
    ProjectileBase m_ProjectileBase;
    private void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, InteractStandard>(m_ProjectileBase, this, gameObject);

        m_ProjectileBase.onShoot += OnShoot;

        Destroy(gameObject, maxLifeTime);
    }
    
    void OnShoot()
    {
        PlayerSpellsManager playerSpellsManager = m_ProjectileBase.owner.GetComponent<PlayerSpellsManager>();
        PlayerCharacterController playerCharacterController = m_ProjectileBase.owner.GetComponent<PlayerCharacterController>();
        Ray ray = playerSpellsManager.spellCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        print("onShoot");
        if (Physics.Raycast(ray, out hit, 100))
        {
            InteractableBase interactable = hit.collider.GetComponent<InteractableBase>();
            if (interactable != null)
            {
                print(string.Format("clicking {0}", hit.collider.name));
                playerCharacterController.SetFocus(interactable);
            } else {
                print("not interactable");
            }
        } else {
            print("didn't hit anything");
        }
    }

}
