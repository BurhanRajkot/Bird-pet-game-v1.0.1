using UnityEngine;

public static class ScreenToWorld
{
    public static Vector2 Convert(float x, float y)
    {
        if (Camera.main == null) return Vector2.zero;

        Camera cam = Camera.main;

        Vector3 screenPoint = new Vector3(
            x,
            Screen.height - y,
            Mathf.Abs(cam.transform.position.z)
        );

        Vector3 world = cam.ScreenToWorldPoint(screenPoint);
        return new Vector2(world.x, world.y);
    }
}