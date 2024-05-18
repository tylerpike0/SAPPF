using com.cyborgAssets.inspectorButtonPro;
using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;

[CreateAssetMenu(fileName = "Attack", menuName = "ScriptableObjects/Attacks/Attack", order = 1)]

public class Attack : ScriptableObject
{
    [SerializeField] protected float damage;
    [SerializeField] protected float stunDuration;
    [SerializeField] protected Vector2 launchDirection;
    [SerializeField] protected string ATTACK_ANIMATION_NAME;
    [SerializeField] protected PlayerController playerController;
    [SerializeField] public List<KeyFrame<SpriteKeyFrameData>> spriteKeyFrames = new List<KeyFrame<SpriteKeyFrameData>>();
    [SerializeField] public List<KeyFrame<HitboxKeyFrameData>> hitboxKeyFrames = new List<KeyFrame<HitboxKeyFrameData>>();
    [SerializeField] public List<KeyFrame<PosKeyFrameData>> posKeyFrames = new List<KeyFrame<PosKeyFrameData>>();
    public virtual void StartAttack(PlayerController player)
    {
        player.animator.SetTrigger("Attack");
        player.ChangeNetworkAnimation(ATTACK_ANIMATION_NAME);

    }

    [ProButton]
    public virtual void AddSprite()
    {
        KeyFrame<SpriteKeyFrameData> newSpriteKeyFrame = spriteKeyFrames[spriteKeyFrames.Count - 1];
        newSpriteKeyFrame.time += 0.1f;
        spriteKeyFrames.Add(newSpriteKeyFrame);
    }

    [ProButton]
    public virtual void AddHitboxKeyFrame()
    {
        KeyFrame<HitboxKeyFrameData> newHitboxKeyFrame = hitboxKeyFrames[hitboxKeyFrames.Count - 1];
        newHitboxKeyFrame.time += 0.1f;
        hitboxKeyFrames.Add(newHitboxKeyFrame);
    }

    [ProButton]
    public virtual void AddPosKeyFrame()
    {
        KeyFrame<PosKeyFrameData> newPosKeyFrame = posKeyFrames[posKeyFrames.Count - 1];
        newPosKeyFrame.time += 0.1f;
        posKeyFrames.Add(newPosKeyFrame);
    }


    public float GetTotalDuration()
    {
        float lastSpriteFrameTime = (spriteKeyFrames.Count > 0 )? spriteKeyFrames[spriteKeyFrames.Count-1].time: 0;
        float lastHitboxFrameTime = (hitboxKeyFrames.Count > 0) ? hitboxKeyFrames[hitboxKeyFrames.Count-1].time : 0;
        float lastposFrameTime = (posKeyFrames.Count > 0) ? posKeyFrames[posKeyFrames.Count-1].time : 0;

        return Mathf.Max(lastSpriteFrameTime, lastHitboxFrameTime, lastposFrameTime);
    }

    private float GetHighestKeyFrameTime<T>(List<KeyFrame<T>> keyFrames) where T: KeyFrameData
    {

        return keyFrames[keyFrames.Count - 1].time;
    }

    public virtual void OnHit(PlayerController player, PlayerController target)
    {
        target.GetComponent<PlayerController>().Hurt(damage, stunDuration, new Vector2(launchDirection.x * player.transform.localScale.x, launchDirection.y));

    }

    public void DisplayAtTime(PlayerController player, float time, Vector3 startPosition)
    {
        HandleSprites(player, time, startPosition);
        HandlePos(player, time, startPosition);
        HandleHitboxes(player, time, startPosition);
    }

    private void HandleSprites(PlayerController player, float time, Vector3 startPosition)
    {
        foreach (KeyFrame<SpriteKeyFrameData> spriteKeyFrame in spriteKeyFrames)
        {
            if (spriteKeyFrame.time > time)
            {
                Sprite sprite = spriteKeyFrames[spriteKeyFrames.IndexOf(spriteKeyFrame) - 1].data.sprite;
                player.spriteRenderer.sprite = sprite;
                break;
            }
        }
    }

    private void HandlePos(PlayerController player, float time, Vector3 startPosition)
    {
        player.transform.position = GetPosAtTime(player, time, startPosition);
    }

    public Vector3 GetPosAtTime(PlayerController player, float time, Vector3 startPosition)
    {
        foreach (KeyFrame<PosKeyFrameData> posKeyFrame2 in posKeyFrames)
        {
            if (posKeyFrame2.time > time)
            {
                KeyFrame<PosKeyFrameData> posKeyFrame1 = posKeyFrames[posKeyFrames.IndexOf(posKeyFrame2) - 1]; // Get the next target keyframe
                float time1 = posKeyFrame1.time; // Keyframe last passed time
                float time2 = posKeyFrame2.time; // Next keyframe time

                float t = (time - time1) / (time2 - time1); // Calculate t (percentage along path between pos1 and 2)
                float tSquared = Mathf.Pow(t, 2);
                float tCubed = Mathf.Pow(t, 3);


                // Get Bezier control points
                Vector2 point1 = posKeyFrame1.data.pos;
                Vector2 point2 = posKeyFrame1.data.afterBezierControlPoint;
                Vector2 point3 = posKeyFrame2.data.beforeBezierControlPoint;
                Vector2 point4 = posKeyFrame2.data.pos;

                // Calculate position on bezier curve
                Vector2 pos = point1 * (-tCubed + 3 * tSquared - 3 * t + 1) +
                    point2 * (3 * tCubed - 6 * tSquared + 3 * t) +
                    point3 * (-3 * tCubed + 3 * tSquared) +
                    point4 * (tCubed);


                // Set Pos
                return startPosition + new Vector3(player.transform.localScale.x * pos.x, pos.y, 0);
            } else if (posKeyFrame2 == posKeyFrames[posKeyFrames.Count - 1])
            {
                return startPosition + new Vector3(player.transform.localScale.x * posKeyFrame2.data.pos.x, posKeyFrame2.data.pos.y, 0);
            }
        }
        throw new Exception("Time value of " + time + "has no corresponding Pos Keyframe");
    }

    private void HandleHitboxes(PlayerController player, float time, Vector3 startPosition)
    {
        // For each hitbox, if its between its start and end time, boxcast at its pos and size and handle hits
        foreach (KeyFrame<HitboxKeyFrameData> hitboxKeyFrame in hitboxKeyFrames)
        {
            if (hitboxKeyFrame.time < time && hitboxKeyFrame.time + hitboxKeyFrame.data.length > time)
            {
                Vector2 hitboxPos = new Vector2(player.transform.position.x, player.transform.position.y) + new Vector2(player.transform.localScale.x * hitboxKeyFrame.data.pos.x, hitboxKeyFrame.data.pos.y);
                Vector3 hitboxSize = new Vector2(hitboxKeyFrame.data.size.x, hitboxKeyFrame.data.size.y);
                RaycastHit2D[] hits = Physics2D.BoxCastAll(hitboxPos, hitboxSize, 0, Vector2.zero, 0, player.playerLayer);
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider != null && hit.collider.gameObject.TryGetComponent(out PlayerController playerHit))
                    {
                        if (playerHit != player) OnHit(player, playerHit);
                    }
                }
            }
        }
    }
}

[System.Serializable]
public class KeyFrame<T> where T : KeyFrameData
{
    public float time;
    public T data;
}

public interface KeyFrameData
{

}

[System.Serializable]
public struct SpriteKeyFrameData : KeyFrameData
{
    public Sprite sprite;
}

[System.Serializable]
public struct HitboxKeyFrameData : KeyFrameData
{
    public Vector2 pos;
    public Vector2 size;
    public float length;
}

[System.Serializable]
public struct PosKeyFrameData : KeyFrameData
{
    public Vector2 pos;
    public Vector2 beforeBezierControlPoint;
    public Vector2 afterBezierControlPoint;
}