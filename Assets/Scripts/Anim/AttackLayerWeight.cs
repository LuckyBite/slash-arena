using UnityEngine;

public class AttackLayerWeight : StateMachineBehaviour
{
    [Range(0,1)] public float onEnter = 1f;
    [Range(0,1)] public float onExit  = 0f;
    public float blendIn  = 0.06f;
    public float blendOut = 0.12f;

    int layer;
    float t; bool fadingIn, fadingOut;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        layer = layerIndex; t = 0; fadingIn = true; fadingOut = false;
        animator.SetLayerWeight(layer, onEnter);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (fadingIn)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, blendIn);
            float w = Mathf.Lerp(animator.GetLayerWeight(layer), onEnter, t);
            animator.SetLayerWeight(layer, w);
            if (t >= 1f) { fadingIn = false; t = 0; }
        }
        else if (fadingOut)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, blendOut);
            float w = Mathf.Lerp(animator.GetLayerWeight(layer), onExit, t);
            animator.SetLayerWeight(layer, w);
            if (t >= 1f) { fadingOut = false; t = 0; }
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetLayerWeight(layer, onExit);
        fadingIn = false; fadingOut = false; t = 0;
    }
}
