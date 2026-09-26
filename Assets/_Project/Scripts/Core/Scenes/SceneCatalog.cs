using System;

namespace TheShadowWood.Core.Scenes
{
    /// <summary>
    /// Maps SceneId to the scene name in Build Settings. Explicit per-value mapping so reordering
    /// or inserting enum values cannot silently point at the wrong scene.
    /// </summary>
    public static class SceneCatalog
    {
        public static string GetSceneName(SceneId sceneId)
        {
            switch (sceneId)
            {
                case SceneId.Bootstrap: return "Bootstrap";
                case SceneId.Menu: return "Menu";
                case SceneId.Gameplay: return "Gameplay";
                default: throw new ArgumentOutOfRangeException(nameof(sceneId), sceneId, "Scene is not mapped in SceneCatalog.");
            }
        }
    }
}
