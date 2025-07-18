using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ZombieGame.NPC.Enemy.Zombie.Structs;

namespace ZombieGame.NPC.Enemy.Zombie
{
    public static class DynamicAnimatorController
    {
        public static void ApplyOverrides(
            Animator animator,
            RuntimeAnimatorController originalController,
            params AnimationStateData[] states)
        {
            if (animator == null || originalController == null || states == null) return;

            var overrideController = new AnimatorOverrideController(originalController);

            foreach (var state in states)
            {
                // Use GetAnimationClip() to ensure proper selection and set the index
                AnimationClip selectedClip = state.GetAnimationClip();
                if (selectedClip == null) continue;

                // Try to find original clip, but if not found, create a dummy clip for the state
                AnimationClip originalClip = FindOriginalClipForState(originalController, state.stateName);
                if (originalClip != null) {
                    overrideController[originalClip] = selectedClip;
                }
            }

            animator.runtimeAnimatorController = overrideController;
        }

        private static AnimationClip FindOriginalClipForState(RuntimeAnimatorController controller, string stateName)
        {
            if (controller == null) return null;
            foreach (var clip in controller.animationClips)
            {
                if (clip.name.Contains(stateName) || stateName.Contains(clip.name))
                    return clip;
            }
            foreach (var clip in controller.animationClips)
            {
                string stateNorm = stateName.ToLower().Replace("_", "").Replace(" ", "");
                string clipNorm = clip.name.ToLower().Replace("_", "").Replace(" ", "");
                if (stateNorm.Contains(clipNorm) || clipNorm.Contains(stateNorm))
                    return clip;
            }
            return null;
        }
    }
} 