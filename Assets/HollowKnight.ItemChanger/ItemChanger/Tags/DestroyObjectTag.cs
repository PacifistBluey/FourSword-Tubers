﻿namespace ItemChanger.Tags
{
    /// <summary>
    /// Tag for destroying an object in a specific scene. Can search by name or by path.
    /// </summary>
    public class DestroyObjectTag : Tag
    {
        public string objectName;
        public string sceneName;

        public override void Load(object parent)
        {
            base.Load(parent);
            Events.AddSceneChangeEdit(sceneName, DestroyObject);
        }

        public override void Unload(object parent)
        {
            base.Unload(parent);
            Events.RemoveSceneChangeEdit(sceneName, DestroyObject);
        }

        public void DestroyObject(Scene to)
        {
            if (to.name == sceneName)
            {
                GameObject obj = Locations.ObjectLocation.FindGameObject(objectName);
                if (obj)
                {
                    UObject.Destroy(obj);
                }
            }
        }
    }
}
