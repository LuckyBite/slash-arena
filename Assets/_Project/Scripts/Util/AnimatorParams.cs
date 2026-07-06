using UnityEngine;

// Проверка наличия параметров аниматора, чтобы не падать на чужих контроллерах.
public static class AnimatorParams
{
    public static bool Has(Animator anim, string name, AnimatorControllerParameterType type)
    {
        if (!anim || string.IsNullOrEmpty(name)) return false;
        foreach (var p in anim.parameters)
            if (p.type == type && p.name == name)
                return true;
        return false;
    }
}
