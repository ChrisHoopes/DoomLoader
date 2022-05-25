using UnityEngine;

public class DestroyAfterTime : MonoBehaviour 
{
    public float _lifeTime = 1;
    float time = 0f;

    void Update()
    {
        //so we clear visual effects at the end of campaign
        if (GameManager.Paused && !PlayerInfo.GameFinished)
            return;

        time += Time.deltaTime;

        if (time >= _lifeTime)
            Destroy(gameObject);
    }
}
