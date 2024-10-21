using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SG_AnimationController : MonoBehaviour
{

    private SG_CharacterController m_Character;

    private void Start()
    {
        m_Character = GetComponentInParent<SG_CharacterController>();
    }

    /*
     * All following events are called by Animation Events on the character's animation clips
     */

    public void TriggerJump()
    {
        m_Character.TriggerJump();
    }

    public void TriggerTeleport()
    {
        m_Character.TriggerTeleport();
    }

    public void AttackSquash()
    {
        m_Character.TriggerSquashAttack();
    }

    public void AttackStretch()
    {
        m_Character.TriggerStretchAttack();
    }

    public void ClearAttacks()
    {
        m_Character.ClearAttacks();
    }
}
