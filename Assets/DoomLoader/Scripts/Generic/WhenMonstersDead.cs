using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WhenMonstersDead : MonoBehaviour
{
    public List<MonsterController> monsters = new List<MonsterController>();
    public UnityEvent OnLastMonsterDie = new UnityEvent();
    public int MonsterID = 3003; //Baron of Hell

    private void Start()
    {
        enabled = false;
        StartCoroutine(WaitOneSecondAndEnable());
    }

    IEnumerator WaitOneSecondAndEnable()
    {
        yield return new WaitForSeconds(1);

        foreach (ThingController t in GameManager.Instance.transform.Find("MapThings").GetComponentsInChildren<ThingController>())
            if (t.thingID == MonsterID)
                monsters.Add(t.GetComponent<MonsterController>());

        enabled = true;
    }

    private void Update()
    {
        foreach (MonsterController monster in monsters)
            if (!monster.Dead)
                return;

        OnLastMonsterDie.Invoke();

        enabled = false;
    }
}
