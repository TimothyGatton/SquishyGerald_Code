using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//An individual manager for a particle emitter spawned on enemy death
public class SG_Poof : MonoBehaviour
{
    //Total desired lifetime of particle system
    public float m_HangTime;

    private void Start()
    {
        StartCoroutine(Deathclock());
    }

    IEnumerator Deathclock()
    {
        yield return new WaitForSeconds(m_HangTime);

        Destroy(gameObject);
    }
}
